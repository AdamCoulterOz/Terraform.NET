using System.Text.Json.Nodes;

namespace TF;
public abstract class Provider(Credential credential)
{
	    public Credential Credential { get; } = credential;
	    public Dictionary<string, string> GetConfig()
			=> GetEnvironmentConfig();
		internal Dictionary<string, string> GetEnvironmentConfig()
			=> Credential.EnvKeys().AppendDictionary(this.EnvKeys());
		internal Dictionary<string, JsonValue> GetTerraformConfig()
			=> Credential.TFNodes().AppendDictionary(this.TFNodes());
		public abstract string Name { get; }
	}
