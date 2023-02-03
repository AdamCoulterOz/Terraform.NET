using TF.Model;

namespace TF.Results;

public class Successful<T> : Result
	where T : IOutput
{
	public required T Result { get; init; }
}
