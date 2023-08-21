using System.Text.Json.Serialization;

namespace TF;
public class Module
{
	[JsonPropertyName("id")]
	public required string Id { get; set; }

	[JsonPropertyName("name")]
	public required string Name { get; set; }

	[JsonPropertyName("namespace")]
	public required string Namespace { get; set; }

	[JsonPropertyName("provider")]
	public required string Provider { get; set; }

	[JsonPropertyName("latest_version")]
	public required string LatestVersion { get; set; }
}
