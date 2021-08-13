using System.Text.Json.Serialization;

namespace TF;
public class Module
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = null!;

	[JsonPropertyName("name")]
	public string Name { get; set; } = null!;

	[JsonPropertyName("namespace")]
	public string Namespace { get; set; } = null!;

	[JsonPropertyName("provider")]
	public string Provider { get; set; } = null!;

	[JsonPropertyName("latest_version")]
	public string LatestVersion { get; set; } = null!;
}
