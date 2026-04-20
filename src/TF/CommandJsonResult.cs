using System.Text.Json;

namespace TF;

internal interface ITerraformCommandResult
{
	void LoadFromCommandResult(TFResult result, JsonSerializerOptions options);
}

public abstract class CommandJsonResult : ITerraformCommandResult
{
	public bool Success { get; private set; }
	public int ExitCode { get; private set; }
	public bool? PlanHasChanges { get; private set; }
	public string? RawOutput { get; private set; }
	public string? RawError { get; private set; }

	void ITerraformCommandResult.LoadFromCommandResult(TFResult result, JsonSerializerOptions options)
	{
		Success = result.Success;
		ExitCode = result.ExitCode;
		PlanHasChanges = result.PlanHasChanges;
		RawOutput = result.Output;
		RawError = result.Error;

		LoadJson(result.Output, options);
	}

	protected abstract void LoadJson(string? output, JsonSerializerOptions options);

	protected static T DeserializeJson<T>(string? output, JsonSerializerOptions options)
		=> JsonSerializer.Deserialize<T>(output ?? string.Empty, options)
			?? throw new InvalidOperationException($"Unable to deserialize Terraform JSON output into {typeof(T).FullName}.");
}
