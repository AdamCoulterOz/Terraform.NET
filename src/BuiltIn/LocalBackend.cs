using TF.Attributes;

namespace TF.BuiltIn;

/// <summary>
///     The local backend stores state on the local filesystem, locks that state using system APIs, and performs operations
///     locally.
/// </summary>
public class LocalBackend : Backend<VoidCredential>
{
	public LocalBackend(FileInfo? stateFile = null, DirectoryInfo? workspaceDir = null)
		: base(new VoidCredential())
	{
		StateFile = stateFile;
		WorkspaceDir = workspaceDir;
	}

	protected override string Name => "local";

	/// <summary>
	///     The path to the tfstate file. This defaults to "terraform.tfstate" relative to the root module by default.
	/// </summary>
	[CliNamed("path")]
	public FileInfo? StateFile { get; }

	/// <summary>
	///     The path to non-default workspaces.
	/// </summary>
	[CliNamed("workspace_dir")]
	public DirectoryInfo? WorkspaceDir { get; }
}
