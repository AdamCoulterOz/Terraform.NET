using TF.Attributes;
using TF.Extensions;

namespace TF;

public interface IBackend : ICliAttributed
{
	internal IDictionary<string, string> Parameters { get; }
	void WriteBackendFile(DirectoryInfo path);
}

public abstract class Backend<T> : IBackend
	where T : Credential
{
	protected abstract string Name { get; }
	public T Credential { get; init; }

	public Backend(T credential) => Credential = credential;

	IDictionary<string, string> IBackend.Parameters => CliNamedAttribute.BuildVariables(this);

	public void WriteBackendFile(DirectoryInfo path)
	{
		var backendFile = new FileInfo(Path.Combine(path.FullName, "_backend.tf"));
		backendFile.WriteAllText($$"""
			terraform {
				backend "{{Name}}" {}
			}
			""");
	}
}
