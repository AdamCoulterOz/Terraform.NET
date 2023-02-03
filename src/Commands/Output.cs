using TF.Attributes;
using TF.Model;

namespace TF.Commands;

/// <summary>
/// 	Read module outputs.
/// </summary>
/// <remarks>
/// 	Please implement <see cref="IOutput"/> with the structure of your outputs
/// 	and pass as the <see cref="T"/> argument for automatic deserialisation.
/// 	The deserilisation is done using <see cref="System.Text.Json"/>.
/// </remarks>
public sealed class Output<T> : Action<T>
	where T : IOutput
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

	/// <summary>Always produce output in a machine-readable JSON format.</summary>
	[CliOption("json")]
	protected static bool Json => true;
}
