using TF.Model;

namespace TF.Results;

public abstract class Result<T>
	where T : IOutput
{
	public required T Data { get; init; }
	public required string Output { get; init; }
	public required DateTimeOffset StartTime { get; init; }
	public required DateTimeOffset ExitTime { get; init; }
	public required TimeSpan Duration { get; init; }
}
