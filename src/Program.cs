using TF.Results;

namespace TF;

public class Program
{
	public static async Task Main()
	{
		var terraform = new Terraform();
		var result = await terraform.Version();
		string output = result switch
		{
			Successful<Model.Version> success => success.Result.Architecture,
			Failed failure => failure.Error,
			_ => throw new InvalidOperationException()
		};
		Console.WriteLine(output);
	}
}