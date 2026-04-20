namespace TF;

public class TFResult
{
	public string? Output { get; }
	public string? Error { get; }
	public bool Success { get; }
	public int ExitCode { get; }
	public bool? PlanHasChanges { get; set; }
	public TFResult(bool success, string output, string error, int exitCode)
	{
		Success = success;
		ExitCode = exitCode;
		if (!string.IsNullOrEmpty(output))
			Output = output;
		if (!string.IsNullOrEmpty(error))
			Error = error;
	}
}
