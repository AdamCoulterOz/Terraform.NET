using TF.Attributes;
using TF.Extensions;

namespace TF;

public interface IBackend : ICliAttributed
{
	internal IDictionary<string, string> Parameters { get; }
	void WriteBackendFile(DirectoryInfo path);
}

public abstract class Backend<T>(T credential) : IBackend
	where T : Credential
{
	protected abstract string Name { get; }
	public T Credential { get; init; } = credential;

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
