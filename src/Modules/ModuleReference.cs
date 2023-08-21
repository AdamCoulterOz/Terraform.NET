using TF.Extensions;

namespace TF;
public class ModuleReference
{
	public required string Namespace { get; init; }
	public required string Name { get; init; }
	public required string Provider { get; init; }
	public string? Version { get; set; }

	public string Path => Keys.Join('/');
	public string FullName => Keys.Join('-');

	private IEnumerable<string> Keys
		=> new[] { Namespace, Name, Provider, Version }
		.Where(k => !string.IsNullOrEmpty(k))
		.Select(k => k!);

	public override string ToString() => Path;
}
