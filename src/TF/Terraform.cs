using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
namespace TF;

public class Terraform(Backend backend, DirectoryInfo rootPath, string tfPath, bool ownsRootPath = true) : IDisposable
{
    private const string ManagedPlanFileName = "execute.tfplan";
    private readonly string _tfPath = tfPath;
    private readonly bool _ownsRootPath = ownsRootPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    public DirectoryInfo RootPath { get; } = rootPath;
    public Variables Variables { get; set; } = new Variables();
    public ProviderCollection Providers { get; set; } = new ProviderCollection();
    public Configuration Configuration { get; set; } = new Configuration();
    public Backend Backend { get; set; } = backend;
    public Stream? OutputStream { get; set; }
    private readonly Action _unregisterForProcessExit = ownsRootPath ? RegisterForProcessExit(rootPath) : () => { };

    public static Terraform ForExternalWorkingDirectory(Backend backend, DirectoryInfo rootPath, string tfPath = "terraform")
        => new(backend, rootPath, tfPath, ownsRootPath: false);

    private static Action RegisterForProcessExit(DirectoryInfo rootPath)
    {
        void handler(object? _, EventArgs _e)
        {
            if (rootPath.Exists)
                rootPath.Delete(true);
        }
        AppDomain.CurrentDomain.ProcessExit += handler;
        return () => AppDomain.CurrentDomain.ProcessExit -= handler;
    }

    public void Dispose()
    {
        _unregisterForProcessExit();
        if (_ownsRootPath && RootPath.Exists)
            RootPath.Delete(true);
    }

    public async Task<InitResult> Init(CancellationToken cancellationToken = default)
        => await Command<InitResult>("init", withConfiguration: await Configuration.WriteConfigurationAsync(RootPath), withBackendConfig: true, asJson: true, cancellationToken: cancellationToken);
    public async Task<RefreshResult> Refresh(CancellationToken cancellationToken = default)
        => await Command<RefreshResult>("refresh", withVars: true, asJson: true, cancellationToken: cancellationToken);
    public async Task<ValidateResult> Validate(CancellationToken cancellationToken = default)
        => await Command<ValidateResult>("validate", asJson: true, cancellationToken: cancellationToken);
    public async Task<ApplyResult> Apply(CancellationToken cancellationToken = default)
        => await Command<ApplyResult>("apply", withVars: true, autoApprove: true, asJson: true, cancellationToken: cancellationToken);
    public async Task<ApplyResult> ApplyPlan(CancellationToken cancellationToken = default)
    {
        EnsurePlanExists();
        return await Execute(
            () => Command<ApplyResult>("apply", asJson: true, additionalArguments: [ManagedPlanFileName], cancellationToken: cancellationToken),
            DeletePlanFile);
    }
    public async Task<DestroyResult> Destroy(CancellationToken cancellationToken = default)
        => await Command<DestroyResult>("destroy", withVars: true, autoApprove: true, asJson: true, cancellationToken: cancellationToken);
    public async Task<IReadOnlyDictionary<string, OutputValue>> Output(CancellationToken cancellationToken = default)
        => await Command<Dictionary<string, OutputValue>>("output", asJson: true, cancellationToken: cancellationToken);
    public async Task<PlanResult> Plan(CancellationToken cancellationToken = default)
    {
        DeletePlanFile();
        var result = await Command<PlanResult>("plan", withVars: true, withDetailedExitCode: true, outFile: ManagedPlanFileName, asJson: true, cancellationToken: cancellationToken);
        if (!result.Success)
            DeletePlanFile();
        return result;
    }

    public async Task<ShowResult> Show(bool deleteManagedPlan = true, CancellationToken cancellationToken = default)
    {
        EnsurePlanExists();
        return deleteManagedPlan
            ? await Execute(() => ShowManagedPlan(cancellationToken), DeletePlanFile)
            : await ShowManagedPlan(cancellationToken);
    }

    public async Task<TResult> Show<TResult>(CancellationToken cancellationToken = default)
    {
        EnsurePlanExists();
        return await Execute(() => ShowJson<TResult>(cancellationToken), DeletePlanFile);
    }

    private async Task<TFResult> Command(string action, bool autoApprove = false, bool withVars = false,
        string? outFile = null, bool withConfiguration = false, bool asJson = false, bool withBackendConfig = false,
        bool withDetailedExitCode = false, IEnumerable<string>? additionalArguments = null,
        CancellationToken cancellationToken = default)
    {
        ProviderConfigurationRewriter.Rewrite(RootPath, Providers);

        var command = Cli.Wrap(_tfPath)
            .WithWorkingDirectory(RootPath.FullName);
        var arguments = new List<string> { action };
        if (autoApprove) arguments.Add("-auto-approve");
        if (withDetailedExitCode) arguments.Add("-detailed-exitcode");
        if (asJson) arguments.Add("-json");
        if (outFile != null) arguments.Add($"-out={outFile}");
        if (additionalArguments is not null) arguments.AddRange(additionalArguments);
        if (withVars)
        {
            var json = Variables.VariableJson;
            File.WriteAllText(Path.Join(RootPath.FullName, "execute.tfvars.json"), json);
            arguments.Add("-var-file=\"execute.tfvars.json\"");
        }
        if (withBackendConfig)
        {
            Backend.WriteBackendFile(RootPath);
            arguments.AddRange(Backend.Arguments);
        }
        command = command.WithArguments(arguments, false);
        var environmentVariables = Providers.CombinedProviderConfigs
            .AppendDictionary(Backend.EnvironmentConfig);
        if (withConfiguration)
            environmentVariables.Add(Configuration.ConfigFileEnvVariable, Configuration.FilePath(RootPath));
        var envVariables = environmentVariables.ToDictionary(kv => kv.Key, kv => (string?)kv.Value); //this line is redundent, used to fix compiler warning
        command = command.WithEnvironmentVariables(envVariables);

        if (OutputStream is not null)
            command = command.WithStandardOutputPipe(PipeTarget.ToStream(OutputStream, true))
                             .WithStandardErrorPipe(PipeTarget.ToStream(OutputStream, true));

        // Pass the caller's token as CliWrap's GRACEFUL cancellation token (sends Ctrl-C/SIGINT and waits
        // for the process to exit on its own), never the forceful one (which would SIGKILL). This lets
        // terraform finish its current resource, flush state to the remote backend, release the lock, and
        // exit cleanly so the run is resumable. Encodings are passed explicitly because the graceful overload
        // has no encoding-defaulting variant; Console.OutputEncoding matches CliWrap's default behaviour, so
        // an uncancelled run is byte-for-byte identical. When the token trips, CliWrap throws
        // OperationCanceledException AFTER the process exits — it is allowed to propagate.
        var cmdResult = await command.WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(
                Console.OutputEncoding,
                Console.OutputEncoding,
                forcefulCancellationToken: CancellationToken.None,
                gracefulCancellationToken: cancellationToken);

        bool? planHasChanges = withDetailedExitCode && cmdResult.ExitCode == 2 ? true : null;
        var success = cmdResult.ExitCode == 0 || (planHasChanges.HasValue && planHasChanges.Value);

        return new TFResult(success, cmdResult.StandardOutput, cmdResult.StandardError, cmdResult.ExitCode)
        {
            PlanHasChanges = planHasChanges
        };
    }

    private async Task<TResult> Command<TResult>(string action, bool autoApprove = false, bool withVars = false,
        string? outFile = null, bool withConfiguration = false, bool asJson = true, bool withBackendConfig = false,
        bool withDetailedExitCode = false, IEnumerable<string>? additionalArguments = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Command(action, autoApprove, withVars, outFile, withConfiguration, asJson, withBackendConfig,
            withDetailedExitCode, additionalArguments, cancellationToken);
        try
        {
            return DeserializeCommandResult<TResult>(result);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"Terraform {action} did not return JSON that could be deserialized into {typeof(TResult).FullName}.{Environment.NewLine}{CombineOutput(result.Error, result.Output)}",
                ex);
        }
    }

    private TResult DeserializeCommandResult<TResult>(TFResult result)
    {
        if (typeof(ITerraformCommandResult).IsAssignableFrom(typeof(TResult)))
        {
            var instance = Activator.CreateInstance(typeof(TResult))
                ?? throw new InvalidOperationException($"Unable to create Terraform command result type {typeof(TResult).FullName}.");
            ((ITerraformCommandResult)instance).LoadFromCommandResult(result, JsonOptions);
            return (TResult)instance;
        }

        if (string.IsNullOrWhiteSpace(result.Output))
            throw new InvalidOperationException("Terraform did not produce any JSON output.");

        return JsonSerializer.Deserialize<TResult>(result.Output, JsonOptions)
            ?? throw new InvalidOperationException(
                $"Terraform returned JSON, but it could not be deserialized into {typeof(TResult).FullName}.");
    }

    private async Task<ShowResult> ShowManagedPlan(CancellationToken cancellationToken = default)
        => new()
        {
            Json = await ShowJson<ShowJsonResult>(cancellationToken),
            File = ShowFileResult.From(await ShowFile(cancellationToken))
        };

    private Task<TResult> ShowJson<TResult>(CancellationToken cancellationToken = default)
        => Command<TResult>("show", asJson: true, additionalArguments: [ManagedPlanFileName], cancellationToken: cancellationToken);

    private Task<TFResult> ShowFile(CancellationToken cancellationToken = default)
        => Command("show", additionalArguments: [ManagedPlanFileName], cancellationToken: cancellationToken);

    private static async Task<TResult> Execute<TResult>(Func<Task<TResult>> action, Action cleanup)
    {
        try
        {
            return await action();
        }
        finally
        {
            cleanup();
        }
    }

    private void EnsurePlanExists()
    {
        if (!File.Exists(ManagedPlanFilePath))
            throw new InvalidOperationException("No managed Terraform plan exists. Run `Plan` before calling `Show`.");
    }

    private void DeletePlanFile()
    {
        if (File.Exists(ManagedPlanFilePath))
            File.Delete(ManagedPlanFilePath);
    }

    private string ManagedPlanFilePath => Path.Join(RootPath.FullName, ManagedPlanFileName);

    private static string CombineOutput(string? left, string? right)
        => string.Join(Environment.NewLine, new[] { left, right }.Where(part => !string.IsNullOrWhiteSpace(part)));
}
