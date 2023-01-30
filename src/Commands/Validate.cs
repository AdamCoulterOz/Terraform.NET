using TF.Attributes;

namespace TF.Commands;

/// <summary>
/// Validates the configuration files in a directory, verifying the configuration's syntax and internal consistency without accessing remote services.
/// <br />
/// This command is primarily useful for general verification of reusable modules, including correctness of attribute names and value types.
/// </summary>
/// <remarks>
///   <list type="bullet">
///     <item>Validation requires <see cref="Init"/> has already been run.</item>
///     <item>To verify configuration in the context of a particular run, use the <see cref="Plan"/> command instead.</item>
///   </list>
/// </remarks>
public class Validate : Main
{
	protected override string Command => "validate";

	/// <summary>Produce output in a machine-readable JSON format.</summary>
	[CliOption("json")]
	public bool? Json { get; set; }
}

/// <summary>Read module outputs.</summary>
public class Output
{
	/// <summary>The name of the output to read. If omitted, all outputs are shown.</summary>
	[CliArgument]
	public string? Name { get; set; }

	/// <summary>Output the values as raw strings, without formatting.</summary>
	[CliOption("raw")]
	public bool? Raw { get; set; }

	/// <summary>Override `terraform.tfstate` as the local backend's state file.</summary>
	[CliOption("state")]
	public string? State { get; set; }

	/// <summary>Produce output in a machine-readable JSON format.</summary>
	[CliOption("json")]
	public bool? Json { get; set; }
}
