using TF.Attributes;

namespace TF.Commands;

/// <summary>Read module outputs.</summary>
public class Output : Action
{
	protected override string Command => "output";

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
	public static bool Json => true;
}
