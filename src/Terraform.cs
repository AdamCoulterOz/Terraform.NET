using CliWrap;
using CliWrap.Buffered;
using TF.Commands;
using TF.Extensions;
using TF.Model;
using TF.Model.Validate;
using TF.Results;
using Plan = TF.Model.Plan;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TF.Tests.Unit")]
namespace TF;

public class Terraform : IDisposable, IAsyncDisposable
{
	private readonly TerraformConfig config;
	internal DirectoryInfo Path { get; init; }

	private Terraform(TerraformConfig? config = null)
	{
		this.config = config ?? new();
		Path = IOExtensions.GetNewTempDirectory();
	}

	public static async Task<Terraform> CreateAsync(DirectoryInfo? source = null, TerraformConfig? config = null, bool backend = true)
	{
		var terraform = new Terraform(config);
		if (source != null)
			await source.CopyToAsync(terraform.Path);
		var initResult = await terraform.Init(backend);
		if (initResult is Failed<Initialisation> failed)
			throw new ArgumentException($"Failed to initialise Terraform: {failed.Error}");
		return terraform;
	}

	/// <summary>Get the version of Terraform that is being used.</summary>
	public async Task<Result<Model.Version>> Version()
	=> await RunCommandAsync<Commands.Version, Model.Version>(new Commands.Version { });


	/// <summary>Initialise the Terraform configuration.</summary>
	/// <param name="backend">Whether to initialise with a backend.</param>
	private async Task<Result<Initialisation>> Init(bool backend = true)
	{
		var init = new Init { UseBackend = backend };
		if (backend)
		{
			init.BackendConfigValues = config.Backend.Parameters;
			config.Backend.WriteBackendFile(Path);
		}
		return await RunCommandAsync<Init, Initialisation>(init);
	}

	/// <summary>Validate the configuration, not accessing state or services.</summary>
	public async Task<Result<Validation>> Validate()
		=> await RunCommandAsync<Validate, Validation>(new Validate { });

	public async Task<Result<Plan>> Refresh()
		=> await RunCommandAsync<Refresh, Plan>(new Refresh
		{ Variables = config.Variables });

	public async Task<Result<Plan>> Plan()
		=> await RunCommandAsync<Commands.Plan, Plan>(new Commands.Plan
		{ Variables = config.Variables });

	public async Task<Result<Plan>> Apply()
		=> await RunCommandAsync<Apply, Plan>(new Apply
		{ Variables = config.Variables });

	public async Task<Result<Plan>> Destroy()
		=> await RunCommandAsync<Apply, Plan>(new Apply
		{
			Destroy = true,
			Variables = config.Variables
		});

	public async Task<Result<T>> Output<T>()
		where T : IOutput
		=> await RunCommandAsync<Output<T>, T>(new Output<T> { });

	public async Task<Result<TResult>> RunCommandAsync<TAction, TResult>(TAction action)
		where TAction : Commands.Action<TResult>
		where TResult : IOutput
	{
		var command = Cli.Wrap(config.CLI)
			.WithWorkingDirectory(Path.FullName)
			.WithArguments(action.GetCommand())
			.WithEnvironmentVariables(config.Providers.CombinedProviderConfigs)
			.WithValidation(CommandResultValidation.None);

		if (config.Stream is not null)
			command = command.WithStandardOutputPipe(PipeTarget.ToStream(config.Stream, true))
							 .WithStandardErrorPipe(PipeTarget.ToStream(config.Stream, true));

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

	~Terraform() => Dispose();
	private bool disposedValue;
	public void Dispose()
	{
		if (!disposedValue)
		{
			Path?.Delete(true);
			disposedValue = true;
			GC.SuppressFinalize(this);
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (disposedValue) return;
		await Task.Run(() => Path?.Delete(true));
		disposedValue = true;
		GC.SuppressFinalize(this);
	}
}
