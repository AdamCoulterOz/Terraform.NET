namespace TF;
public abstract class Provider
{
	protected Provider(Credential credential) => Credential = credential;
	public Credential Credential { get; }
	public Dictionary<string, string> GetConfig()
		=> Credential.EnvKeys().AppendDictionary(this.EnvKeys());
	public abstract string Name { get; }
}
