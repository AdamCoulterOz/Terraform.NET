using System.Text.Json.Nodes;

namespace TF;
public abstract class Provider
{
	protected Provider(Credential credential) => Credential = credential;
	public Credential Credential { get; }
	public Dictionary<string, string> GetConfig()
		=> GetEnvironmentConfig();
	internal Dictionary<string, string> GetEnvironmentConfig()
		=> Credential.EnvKeys().AppendDictionary(this.EnvKeys());
	internal Dictionary<string, JsonNode> GetTerraformConfig()
		=> Credential.TFNodes().AppendDictionary(this.TFNodes());
	public abstract string Name { get; }
}
