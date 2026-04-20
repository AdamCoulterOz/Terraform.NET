namespace TF;
public abstract class Provider(Credential credential)
{
	    public Credential Credential { get; } = credential;
	    public Dictionary<string, string> GetConfig()
			=> GetEnvironmentConfig();
		internal Dictionary<string, string> GetEnvironmentConfig()
			=> Credential.EnvKeys().AppendDictionary(this.EnvKeys());
		internal Dictionary<string, TFValue> GetTerraformConfig()
			=> Credential.TFValues().AppendDictionary(this.TFValues());
		public abstract string Name { get; }
	}
