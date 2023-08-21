using TF.Attributes;
using TF.Extensions;

namespace TF;
public abstract class Provider<T> : IProvider, ICliAttributed
	where T : Credential
{
	public Provider(T credential) { Credential = credential; }
	public T Credential { get; init; }
	public IDictionary<string, string> GetConfig()
		=> CliNamedAttribute.BuildVariables(Credential)
							.Merge(CliNamedAttribute.BuildVariables(this));

    IDictionary<string, string> IProvider.GetConfig()
    {
        throw new NotImplementedException();
    }

    public abstract string Name { get; }
}

public interface IProvider
{
    public IDictionary<string, string> GetConfig();
    public string Name { get; }
}