using TF.Model;

namespace TF.Results;

public class Successful<T> : Result<T>
	where T : IOutput
{
}
