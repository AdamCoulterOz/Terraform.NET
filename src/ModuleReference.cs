namespace TF;
public class ModuleReference
{
	public ModuleReference(string @namespace, string name, string provider, string? version = null)
	{
		Namespace = @namespace;
		Name = name;
		Provider = provider;
		Version = version;
	}

	public string Namespace { get; set; }
	public string Name { get; set; }
	public string Provider { get; set; }
	public string? Version { get; set; }

	public string Path => string.Join('/', Keys);
	public string FullName => string.Join('-', Keys);

	private IEnumerable<string> Keys => new List<string?>() { Namespace, Name, Provider, Version }
		.Where(k => !string.IsNullOrEmpty(k)).Select(k => k!).ToList();
	public override string ToString() => Path;
}
