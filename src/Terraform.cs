using CliWrap;
using CliWrap.Buffered;
using TF.BuiltIn;

namespace TF;

public class Terraform
{
	public required string CLI { get; init; } = "terraform";
	public required DirectoryInfo Path { get; init; } = new(".");
	public required ProviderSet Providers { get; init; } = new();
	public required IBackend Backend { get; init; } = new LocalBackend();
	public required Dictionary<string, string> Variables { get; init; } = new();
	public Configuration Configuration { get; init; } = new Configuration();
	public Stream? Stream { get; set; }

	public async Task<Result> Version()
		=> await RunCommandAsync(new Commands.Version { });

	public async Task<Result> Init()
	{
		var init = new Commands.Init { BackendConfigValues = Backend.Parameters };
		Backend.WriteBackendFile(Path);
		return await RunCommandAsync(init);
	}

	public async Task<Result> Validate()
		=> await RunCommandAsync(new Commands.Validate { });

	public async Task<Result> Refresh()
		=> await RunCommandAsync(new Commands.Refresh
		{ Variables = Variables });

	public async Task<Result> Plan()
		=> await RunCommandAsync(new Commands.Plan
		{ Variables = Variables });

	public async Task<Result> Apply()
		=> await RunCommandAsync(new Commands.Apply
		{ Variables = Variables });

	public async Task<Result> Destroy()
		=> await RunCommandAsync(new Commands.Apply
		{
			Destroy = true,
			Variables = Variables
		});

	public async Task<Result> RunCommandAsync<T>(T action)
		where T : Commands.Action
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
		return new Result
		{
			ExitCode = result.ExitCode,
			Output = result.StandardOutput,
			Error = result.StandardError,
			StartTime = result.StartTime,
			ExitTime = result.ExitTime,
			Duration = result.RunTime
		};
	}
}