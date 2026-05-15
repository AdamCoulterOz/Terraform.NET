namespace TF;
public abstract class Provider(Credential credential)
{
	    public Credential Credential { get; } = credential;
	    public Dictionary<string, string> GetConfig()
			=> GetEnvironmentConfig();
		protected internal virtual Dictionary<string, string> GetEnvironmentConfig()
			=> Credential.EnvKeys().AppendDictionary(this.EnvKeys());
		protected internal virtual Dictionary<string, TFValue> GetTerraformConfig()
			=> Credential.TFValues().AppendDictionary(this.TFValues());
		public abstract string Name { get; }
		public virtual string Source => $"hashicorp/{Name}";
		public virtual string? VersionConstraint => null;
	}
