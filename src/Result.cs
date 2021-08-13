namespace TF;

public class TFResult
{
	public string? Output { get; }
	public string? Error { get; }
	public bool Success { get; }
	public bool? PlanHasChanges { get; set; }
	public TFResult(bool success, string output, string error)
	{
		Success = success;
		if (!string.IsNullOrEmpty(output))
			Output = output;
		if (!string.IsNullOrEmpty(error))
			Error = error;
	}
}
