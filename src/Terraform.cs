using System.Text.Json;
using System.Text.Json.Serialization;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;
using TF.Model;

namespace TF;
public class Terraform : IDisposable
{
	private readonly ILogger _log;
	private readonly string _tfPath;
	public Terraform(Backend backend, DirectoryInfo rootPath, string tfPath, ILogger log)
	{
		_tfPath = tfPath;
		Backend = backend;
		RootPath = rootPath;
		Variables = new Variables();
		Providers = new ProviderCollection();
		Configuration = new Configuration();
		_log = log;
	}
	public void Dispose() => RootPath.Delete(true);

	public DirectoryInfo RootPath { get; }
	public Variables Variables { get; set; }
	public ProviderCollection Providers { get; set; }
	public Configuration Configuration { get; set; }
	public Backend Backend { get; set; }
	public Stream? OutputStream { get; set; }

	public async Task<string> Init()
	{
		var hasConfiguration = await Configuration.WriteConfigurationAsync(RootPath);
		return await Command("init", withConfiguration: hasConfiguration, withBackendConfig: true);
	}
	public async Task<string> Refresh() => await Command("refresh", withVars: true);
	public async Task<string> Validate() => await Command("validate");
	public async Task<string> Apply() => await Command("apply", withVars: true, autoApprove: true);
	public async Task<string> Destroy() => await Command("destroy", withVars: true, autoApprove: true);
	public async Task<(Plan? Plan, string StandardOutput)> Plan()
	{
		const string reviewTfPlanPath = "reviewTFPlan";
		// add "-detailed-exitcode" to command to get back different RCs
		// 0 = Empty diff
		// 1 = Error
		// 2 = Non-empty diff
		var stdOut = await Command("plan", withVars: true, outFile: reviewTfPlanPath);
		var showPlanResult = await Cli.Wrap("terraform")
			.WithWorkingDirectory(RootPath.FullName)
			.WithArguments($"show -json {reviewTfPlanPath}")
			.ExecuteBufferedAsync();
		var jsonPlan = showPlanResult.StandardOutput;
		JsonSerializerOptions options = new() { Converters = { new JsonStringEnumConverter() } };
		var plan = JsonSerializer.Deserialize<Plan>(jsonPlan, options);
		return (plan, stdOut);
	}

	private async Task<string> Command(string action, bool autoApprove = false, bool withVars = false,
		string? outFile = null, bool withConfiguration = false, bool asJson = false, bool withBackendConfig = false)
	{
		var command = Cli.Wrap(_tfPath)
			.WithWorkingDirectory(RootPath.FullName);
		var arguments = new List<string> { action };
		if (autoApprove) arguments.Add("-auto-approve");
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
		try
		{
			var task = await command.WithValidation(CommandResultValidation.None).ExecuteBufferedAsync();
			return task.StandardOutput;
		}
		catch (Exception e)
		{
			_log.LogError(e, "Occured while trying to run terraform-cli");
			throw;
		}
	}
}
