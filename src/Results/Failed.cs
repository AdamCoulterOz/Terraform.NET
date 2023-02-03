namespace TF.Results;

public class Failed : Result
{
	public required string Error { get; init; }
}