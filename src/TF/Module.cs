using System.Text.Json.Serialization;

namespace TF;
public class Module
{
	public required string Id { get; set; }
	public required string Name { get; set; }
	public required string Namespace { get; set; }
	public required string Provider { get; set; }
	public required string LatestVersion { get; set; }
}
