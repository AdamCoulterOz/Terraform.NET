using TF.Attributes;

namespace TF;

public interface IBackend : ICliAttributed
{
	public IDictionary<string, string> Parameters { get; }
	void WriteBackendFile(DirectoryInfo path);
}