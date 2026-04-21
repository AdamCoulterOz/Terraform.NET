using System.Text.Json;

namespace TF;

public sealed class ShowResult
{
	public required ShowJsonResult Json { get; init; }
	public required ShowFileResult File { get; init; }
	public bool Success => Json.Success && File.Success;
}

public sealed class ShowJsonResult : CommandJsonResult
{
	public TFObject Document { get; private set; } = new();

	protected override void LoadJson(string? output, JsonSerializerOptions options)
	{
		var value = DeserializeJson<TFValue>(output, options);
		Document = value as TFObject
			?? throw new InvalidOperationException($"Terraform show -json returned '{value.Kind}', expected an object document.");
	}
}

public sealed class ShowFileResult : TFResult
{
	internal ShowFileResult(bool success, string output, string error, int exitCode)
		: base(success, output, error, exitCode)
	{
	}

	internal static ShowFileResult From(TFResult result)
		=> new(result.Success, result.Output ?? string.Empty, result.Error ?? string.Empty, result.ExitCode)
		{
			PlanHasChanges = result.PlanHasChanges
		};
}
