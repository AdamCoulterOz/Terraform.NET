using CliWrap;
using CliWrap.Buffered;

namespace TF;

public class Terraform : IDisposable
{
	private readonly string _tfPath;
	public DirectoryInfo RootPath { get; }
	public Variables Variables { get; set; }
	public ProviderCollection Providers { get; set; }
	public Configuration Configuration { get; set; }
	public Backend Backend { get; set; }
	public Stream? OutputStream { get; set; }

	public Terraform(Backend backend, DirectoryInfo rootPath, string tfPath)
	{
		_tfPath = tfPath;
		Backend = backend;
		RootPath = rootPath;
		Variables = new Variables();
		Providers = new ProviderCollection();
		Configuration = new Configuration();
	}

	public void Dispose()
	{
		RootPath.Delete(true);
		GC.SuppressFinalize(this);
	}

	public async Task<TFResult> Init() => await Command("init", withConfiguration: await Configuration.WriteConfigurationAsync(RootPath), withBackendConfig: true);
	public async Task<TFResult> Refresh() => await Command("refresh", withVars: true);
	public async Task<TFResult> Validate() => await Command("validate");
	public async Task<TFResult> Apply() => await Command("apply", withVars: true, autoApprove: true);
	public async Task<TFResult> Destroy() => await Command("destroy", withVars: true, autoApprove: true);
	public async Task<TFResult> Plan() => await Command("plan", withVars: true, withDetailedExitCode: true);

	private async Task<TFResult> Command(string action, bool autoApprove = false, bool withVars = false,
		string? outFile = null, bool withConfiguration = false, bool asJson = false, bool withBackendConfig = false,
		bool withDetailedExitCode = false)
	{
		var command = Cli.Wrap(_tfPath)
			.WithWorkingDirectory(RootPath.FullName);
		var arguments = new List<string> { action };
		if (autoApprove) arguments.Add("-auto-approve");
		if (withDetailedExitCode) arguments.Add("-detailed-exitcode");
		if (asJson) arguments.Add("-json");
		if (outFile != null) arguments.Add($"-out={outFile}");
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
		var environmentVariables = Providers.CombinedProviderConfigs;
		if (withConfiguration)
			environmentVariables.Add(Configuration.ConfigFileEnvVariable, Configuration.FilePath(RootPath));
		var envVariables = environmentVariables.ToDictionary(kv => kv.Key, kv => (string?)kv.Value); //this line is redundent, used to fix compiler warning
		command = command.WithEnvironmentVariables(envVariables);

		if (OutputStream is not null)
			command = command.WithStandardOutputPipe(PipeTarget.ToStream(OutputStream, true))
							 .WithStandardErrorPipe(PipeTarget.ToStream(OutputStream, true));

		var cmdResult = await command.WithValidation(CommandResultValidation.None).ExecuteBufferedAsync();

		bool? planHasChanges = withDetailedExitCode && cmdResult.ExitCode == 2 ? true : null;
		var success = cmdResult.ExitCode == 0 || (planHasChanges.HasValue && planHasChanges.Value);

		return new TFResult(success, cmdResult.StandardOutput, cmdResult.StandardError)
		{
			PlanHasChanges = planHasChanges
		};
	}
}
