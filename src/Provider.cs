using TF.Attributes;

namespace TF;
public abstract class Provider : ICliAttributed
{
	protected Provider(Credential credential) => Credential = credential;
	public Credential Credential { get; }
	public Dictionary<string, string> GetConfig()
		=> CliNamedAttribute.BuildVariables(Credential)
							.AppendDictionary(CliNamedAttribute.BuildVariables(this));
	public abstract string Name { get; }
}
