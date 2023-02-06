namespace TF.Model;

public class Initialisation : IOutput
{
	/// <summary>
	/// The providers and their versions that were installed
	/// </summary>
	public Dictionary<string, string> Providers { get; set; } = new();
}