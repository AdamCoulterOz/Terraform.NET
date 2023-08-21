using TF.BuiltIn;

namespace TF;

public class TerraformConfig
{
	public string CLI { get; init; } = "terraform";
	public ProviderSet Providers { get; init; } = new();
	public IBackend Backend { get; init; } = new LocalBackend();
	public Dictionary<string, string> Variables { get; init; } = new();
	public Configuration Configuration { get; init; } = new Configuration();

	/// <summary>The stream to write the output to.</summary>
	public Stream? Stream { get; set; }
}
