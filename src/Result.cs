namespace TF;

public class Result
{
	public required string Output { get; init; }
	public required string Error { get; init; }
	public required int ExitCode { get; init; }
	public required DateTimeOffset StartTime { get; init; }
	public required DateTimeOffset ExitTime { get; init; }
	public required TimeSpan Duration { get; init; }
}
