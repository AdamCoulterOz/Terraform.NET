namespace TF;
public class ModuleReference
{
	public required string Namespace { get; init; }
	public required string Name { get; init; }
	public required string Provider { get; init; }
	public string? Version { get; set; }

	public string Path => string.Join('/', Keys);
	public string FullName => string.Join('-', Keys);

	private IEnumerable<string> Keys
		=> new List<string?>() { Namespace, Name, Provider, Version }
		.Where(k => !string.IsNullOrEmpty(k)).Select(k => k!).ToList();

	public override string ToString() => Path;
}
