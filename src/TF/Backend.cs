namespace TF;
public abstract class Backend(Credential? credential = null)
{
	protected abstract string Name { get; }
    protected Credential? Credential { get; set; } = credential;

    public IEnumerable<string> Arguments
		=> this.TFKeys().AppendDictionary(Credential.TFKeys()).Select(keyValue => $"-backend-config=\"{keyValue.Key}={keyValue.Value}\"");

	public void WriteBackendFile(DirectoryInfo workingFolder)
	{
		var backendFile = new FileInfo(Path.Join(workingFolder.FullName, "backend.tf"));
		var content = $"terraform {{\n\tbackend \"{Name}\" {{ }}\n}}";
		File.WriteAllText(backendFile.FullName, content);
	}
}
