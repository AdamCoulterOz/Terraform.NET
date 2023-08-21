using TF.Attributes;
using TF.Model;

namespace TF.Commands;

public sealed class Init : Main<Initialisation>
{
	protected override string Command => "init";

	/// <summary>Set to `false` to prevent backend setup for this execution</summary>
	[CliOption("backend")]
	public bool? UseBackend { get; set; }

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
	public ICollection<DirectoryInfo> PluginDirectories { get; set; } = new List<DirectoryInfo>();

	[CliOption("plugin-dir")]
	public ICollection<string> PluginDirectoriesCli => PluginDirectories.Select(i => i.FullName).ToList();

	

	/// <summary>Reconfigure a backend, ignoring any saved configuration.</summary>
	[CliOption("reconfigure")]
	public bool? Reconfigure { get; set; }

	/// <summary>Install the latest module and provider versions allowed within configured constraints, overriding the default behavior of selecting precisely the version recorded in the dependency lock file.</summary>
	[CliOption("upgrade")]
	public bool? Upgrade { get; set; }

	public override Initialisation Parse(string output)
	{
		var providers = new Dictionary<string, string>();
		var lines = output.Split('\n');
		bool isParsingProviders = false;

		foreach (var line in lines)
		{
			if (line.Trim() == "Initializing provider plugins...")
			{
				isParsingProviders = true;
				continue; // Move to the next line
			}

			if (line.Trim() == "Terraform has been successfully initialized!")
			{
				break; // Stop parsing
			}

			if (isParsingProviders)
			{
				var lastSlashIndex = line.LastIndexOf('/');
				if (lastSlashIndex != -1 && lastSlashIndex + 1 < line.Length)
				{
					var partsBeforeSlash = line[..lastSlashIndex].Trim().Split(' ');
					var partsAfterSlash = line[(lastSlashIndex + 1)..].Trim().Split(' ');

					var providerNameBeforeSlash = partsBeforeSlash[^1]; // Last word before the slash
					var providerNameAfterSlash = partsAfterSlash[0]; // First word after the slash
					var version = partsAfterSlash[1].Trim('v');

					var providerName = providerNameBeforeSlash + "/" + providerNameAfterSlash;
					providers.Add(providerName, version);
				}
			}
		}

		return new Initialisation { Providers = providers };
	}
}
