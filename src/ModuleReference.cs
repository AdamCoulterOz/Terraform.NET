namespace TF;
public class ModuleReference
{
	public ModuleReference(string @namespace, string name, string provider)
	{
		Namespace = @namespace;
		Name = name;
		Provider = provider;
	}

	public ModuleReference(string @namespace, string name, string provider, string version)
		: this(@namespace, name, provider)
	{
		Version = version;
	}

	public string Namespace { get; set; }
	public string Name { get; set; }
	public string Provider { get; set; }
	public string? Version { get; set; }

	public string Path => string.Join('/', Keys);
	public string FullName => string.Join('-', Keys);

	private IEnumerable<string> Keys
	{
		get
		{
			var keys = new List<string>() { Namespace, Name, Provider };
			if (Version is not null)
				keys.Add(Version);
			return keys;
		}
	}

	public override string ToString()
	{
		return Path;
	}
}
