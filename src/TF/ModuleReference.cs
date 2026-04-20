namespace TF;
public class ModuleReference(string @namespace, string name, string provider, string? version = null)
{
    public string Namespace { get; set; } = @namespace;
    public string Name { get; set; } = name;
    public string Provider { get; set; } = provider;
    public string? Version { get; set; } = version;

    public string Path => string.Join('/', Keys);
	public string FullName => string.Join('-', Keys);

	private IEnumerable<string> Keys => [.. new[] { Namespace, Name, Provider, Version }
		.OfType<string>()
		.Where(k => k.Length > 0)];
	public override string ToString() => Path;
}
