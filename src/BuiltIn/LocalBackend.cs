namespace TF.BuiltIn;

/// <summary>
///     The local backend stores state on the local filesystem, locks that state using system APIs, and performs operations
///     locally.
/// </summary>
public class LocalBackend : Backend
{
	public LocalBackend(FileInfo? stateFile = null, DirectoryInfo? workspace = null)
	{
		StateFile = stateFile;
		Workspace = workspace;
	}

	protected override string Name => "local";

	/// <summary>
	///     The path to the tfstate file. This defaults to "terraform.tfstate" relative to the root module by default.
	/// </summary>
	[Terraform("path")]
	public FileInfo? StateFile { get; }

	/// <summary>
	///     The path to non-default workspaces.
	/// </summary>
	[Terraform("workspace_dir")]
	public DirectoryInfo? Workspace { get; }
}
