namespace TF;
public abstract class Backend
{
	protected abstract string Name { get; }
	protected Credential? Credential { get; set; }

	public Backend(Credential? credential = null)
		=> Credential = credential;

	public IEnumerable<string> Arguments
		=> this.TFKeys().AppendDictionary(Credential.TFKeys()).Select(keyValue => $"""-backend-config="{keyValue.Key}={keyValue.Value}" """);

	public void WriteBackendFile(DirectoryInfo workingFolder)
	{
		var backendFile = new FileInfo(Path.Join(workingFolder.FullName, "_backend.tf"));
		var content = $$"""
			terraform {
				backend "{{Name}}" {}
			}
			""";
		File.WriteAllText(backendFile.FullName, content);
	}
}
