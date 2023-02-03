namespace TF.Results;

public abstract class Result
{
	public required string Output { get; init; }
	public required DateTimeOffset StartTime { get; init; }
	public required DateTimeOffset ExitTime { get; init; }
	public required TimeSpan Duration { get; init; }
}
