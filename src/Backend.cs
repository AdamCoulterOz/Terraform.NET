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
	protected T Credential { get; init; }

	public Backend(T credential)
		=> Credential = credential;

	IDictionary<string, string> IBackend.Parameters => CliNamedAttribute.BuildVariables(this);

	public void WriteBackendFile(DirectoryInfo workingFolder)
	{
		var backendFile = new FileInfo(Path.Combine(workingFolder.FullName, "_backend.tf"));
		backendFile.WriteAllText($$"""
			terraform {
				backend "{{Name}}" {}
			}
			""");
	}
}
