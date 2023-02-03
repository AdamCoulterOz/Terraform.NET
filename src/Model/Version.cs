namespace TF.Model;

public class Version : IOutput
{
	public required string Number { get; init; }
	public required string Architecture { get; init; }
}