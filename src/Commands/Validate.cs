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
	public static bool Json => true;
}
