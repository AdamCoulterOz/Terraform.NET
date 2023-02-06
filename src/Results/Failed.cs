using TF.Model;

namespace TF.Results;

public class Failed<T> : Result<T>
	where T : IOutput
{
	public required string Error { get; init; }
}