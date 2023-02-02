using TF.Attributes;

namespace TF.Commands;

public class Init : Main
{
	protected override string Command => "init";

	/// <summary>Set to `false` to revent backend (re)initialization for this execution</summary>
	[CliOption("backend")]
	public bool? ReinitBackend { get; set; }

	[CliOption("backend-config")]
	protected ICollection<string> CalcBackendConfig => BackendConfigValues
		.Select(i => $"\"{i.Key}={i.Value}\"")
		.Concat(BackendConfigFiles.Select(i => i.FullName))
		.ToList();

	/// <summary>Merge the configuration with what is in the 'backend' block of the configuration file by defining additional key/value pairs.</summary>
	public IDictionary<string, string> BackendConfigValues { get; set; } = new Dictionary<string, string>();

	/// <summary>Merge the configuration with what is in the 'backend' block of the configuration file by providing path to an HCL file with key/value assignments in the same format as terraform.tfvars.</summary>
	public ICollection<DirectoryInfo> BackendConfigFiles { get; set; } = new List<DirectoryInfo>();

	/// <summary>Suppress the prompts that ask to copy state data when initializing a new state backend by automatically answering "yes" to all confirmation prompts.</summary>
	[CliOption("force-copy")]
	public bool? ForceCopy { get; set; }

	/// <summary>Copy the contents of the given module into the target directory as the root module before initialization.</summary>
	[CliOption("from-module")]
	public DirectoryInfo? FromModule { get; set; }

	/// <summary>Set to false to skip child module installation. Note that some other init steps can complete only when the module tree is complete, so it's recommended to use this flag only when the working directory was already previously initialized with its child modules.</summary>
	[CliOption("get")]
	public bool? InstallModules { get; set; }

	/// <summary>
	/// A rare option used for Terraform Cloud and the remote backend only. Set this to ignore checking that the local and remote Terraform versions use compatible state representations, making an operation proceed even when there is a potential mismatch. See the documentation on configuring Terraform with Terraform Cloud for more information.
	/// </summary>
	[CliOption("ignore-remote-version")]
	public bool? IgnoreRemoteVersion { get; set; }

	[CliOption("lockfile")]
	protected string? CalcLockFile => LockFileMode?.ToString().ToLowerInvariant();

	/// <summary>Set a dependency lock file mode. Currently, only "readonly" is valid.</summary>
	public LockFileModes? LockFileMode { get; set; }
	public enum LockFileModes { ReadOnly }

	/// <summary>Reconfigure a backend and attempt to migrate any existing state.</summary>
	[CliOption("migrate-state")]
	public bool? MigrateState { get; set; }

	/// <summary>Directories containing plugin binaries with the flag, which overrides all default search paths for plugins and prevents automatic plugin installation.</summary>
	[CliOption("plugin-dir")]
	public ICollection<DirectoryInfo> PluginDirectories { get; set; } = new List<DirectoryInfo>();

	/// <summary>Reconfigure a backend, ignoring any saved configuration.</summary>
	[CliOption("reconfigure")]
	public bool? Reconfigure { get; set; }

	/// <summary>Install the latest module and provider versions allowed within configured constraints, overriding the default behavior of selecting precisely the version recorded in the dependency lock file.</summary>
	[CliOption("upgrade")]
	public bool? Upgrade { get; set; }
}
