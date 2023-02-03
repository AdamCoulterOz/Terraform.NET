using CliWrap;
using CliWrap.Buffered;
using TF.BuiltIn;
using TF.Commands;
using TF.Model;
using TF.Model.Validate;
using TF.Results;
using Plan = TF.Model.Plan;

namespace TF;

public class Terraform
{
	public string CLI { get; init; } = "terraform";
	public DirectoryInfo Path { get; init; } = new(".");
	public ProviderSet Providers { get; init; } = new();
	public IBackend Backend { get; init; } = new LocalBackend();
	public Dictionary<string, string> Variables { get; init; } = new();
	public Configuration Configuration { get; init; } = new Configuration();
	public Stream? Stream { get; set; }

	public async Task<Result> Version()
		=> await RunCommandAsync<Commands.Version, Model.Version>(new Commands.Version { });

	public async Task<Result> Init()
	{
		var init = new Init { BackendConfigValues = Backend.Parameters };
		Backend.WriteBackendFile(Path);
		return await RunCommandAsync<Init, Initialisation>(init);
	}

	public async Task<Result> Validate()
		=> await RunCommandAsync<Validate, Validation>(new Validate { });

	public async Task<Result> Refresh()
		=> await RunCommandAsync<Refresh, Plan>(new Refresh
		{ Variables = Variables });

	public async Task<Result> Plan()
		=> await RunCommandAsync<Commands.Plan, Plan>(new Commands.Plan
		{ Variables = Variables });

	public async Task<Result> Apply()
		=> await RunCommandAsync<Apply, Plan>(new Apply
		{ Variables = Variables });

	public async Task<Result> Destroy()
		=> await RunCommandAsync<Apply, Plan>(new Apply
		{
			Destroy = true,
			Variables = Variables
		});

	public async Task<Result> RunCommandAsync<TAction, TResult>(TAction action)
		where TAction : Commands.Action<TResult>
		where TResult : IOutput
	{
		var command = Cli.Wrap(CLI)
			.WithWorkingDirectory(Path.FullName)
			.WithArguments(action.GetCommand())
			.WithEnvironmentVariables(Providers.CombinedProviderConfigs)
			.WithValidation(CommandResultValidation.None);

		if (Stream is not null)
			command = command.WithStandardOutputPipe(PipeTarget.ToStream(Stream, true))
							 .WithStandardErrorPipe(PipeTarget.ToStream(Stream, true));

		var result = await command.ExecuteBufferedAsync();

		if (result.ExitCode != 0)
			return new Failed
			{
				Output = result.StandardOutput,
				Error = result.StandardError,
				StartTime = result.StartTime,
				ExitTime = result.ExitTime,
				Duration = result.RunTime
			};

		return new Successful<TResult>
		{
			Result = action.Parse(result.StandardOutput),
			Output = result.StandardOutput,
			StartTime = result.StartTime,
			ExitTime = result.ExitTime,
			Duration = result.RunTime
		};
	}
}