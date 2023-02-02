using TF.Attributes;

namespace TF;
public abstract class Provider : ICliAttributed
{
	protected Provider(Credential credential) => Credential = credential;
	public Credential Credential { get; }
	public IDictionary<string, string> GetConfig()
		=> CliNamedAttribute.BuildVariables(Credential)
							.Merge(CliNamedAttribute.BuildVariables(this));
	protected internal abstract string Name { get; }
}
