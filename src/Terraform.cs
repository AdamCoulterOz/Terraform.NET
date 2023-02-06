using CliWrap;
using CliWrap.Buffered;
using TF.BuiltIn;
using TF.Commands;
using TF.Extensions;
using TF.Model;
using TF.Model.Validate;
using TF.Results;
using Plan = TF.Model.Plan;

namespace TF;

public class Terraform
{
	public string CLI { get; init; } = "terraform";
	public DirectoryInfo Path { get; init; } = IOExtensions.GetNewTempDirectory();
	public ProviderSet Providers { get; init; } = new();
	public IBackend Backend { get; init; } = new LocalBackend();
	public Dictionary<string, string> Variables { get; init; } = new();
	public Configuration Configuration { get; init; } = new Configuration();
	public Stream? Stream { get; set; }

	public async Task<Result<Model.Version>> Version()
		=> await RunCommandAsync<Commands.Version, Model.Version>(new Commands.Version { });

	public async Task<Result<Initialisation>> Init()
	{
		var init = new Init { BackendConfigValues = Backend.Parameters };
		Backend.WriteBackendFile(Path);
		return await RunCommandAsync<Init, Initialisation>(init);
	}

	public async Task<Result<Validation>> Validate()
		=> await RunCommandAsync<Validate, Validation>(new Validate { });

	public async Task<Result<Plan>> Refresh()
		=> await RunCommandAsync<Refresh, Plan>(new Refresh
		{ Variables = Variables });

	public async Task<Result<Plan>> Plan()
		=> await RunCommandAsync<Commands.Plan, Plan>(new Commands.Plan
		{ Variables = Variables });

	public async Task<Result<Plan>> Apply()
		=> await RunCommandAsync<Apply, Plan>(new Apply
		{ Variables = Variables });

	public async Task<Result<Plan>> Destroy()
		=> await RunCommandAsync<Apply, Plan>(new Apply
		{
			Destroy = true,
			Variables = Variables
		});

	public async Task<Result<T>> Output<T>()
		where T : IOutput
		=> await RunCommandAsync<Output<T>, T>(new Output<T> { });


	public async Task<Result<TResult>> RunCommandAsync<TAction, TResult>(TAction action)
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

		return result.ExitCode == 0 ?
			new Successful<TResult>
			{
				Data = action.Parse(result.StandardOutput),
				Output = result.StandardOutput,
				StartTime = result.StartTime,
				ExitTime = result.ExitTime,
				Duration = result.RunTime
			} :
			new Failed<TResult>
			{
				Data = action.Parse(result.StandardOutput),
				Error = result.StandardError,
				Output = result.StandardOutput,
				StartTime = result.StartTime,
				ExitTime = result.ExitTime,
				Duration = result.RunTime
			};
	}
}