using TF.Attributes;

namespace TF.Commands;

/// <summary>
///   Generates a speculative execution plan, showing what actions Terraform would take to apply the current configuration. This command will not actually perform the planned actions.
/// </summary>
/// <remarks>
///   You can optionally save the plan to a file, which you can then pass to the "apply" command to perform exactly the actions described in the plan.
/// </remarks>
public sealed class Plan : PlanApply
{
	protected override string Command => "plan";

	/// <summary>Return a detailed exit code when the command exits.</summary>
	/// <remarks>
	/// This will change the meaning of exit codes to:
	/// <list type="bullet">
	///   <item>`0` - Succeeded (no changes)</item>
	///   <item>`1` - Errored</item>
	///   <item>`2` - Succeeded (has changes)</item>
	/// </list>
	/// </remarks>
	[CliOption("detailed-exitcode")]
	protected static bool DetailedExitCode => false;

	/// <summary>Write a plan file to the given path.</summary>
	/// <remarks>This can be used as input to the "apply" command.</remarks>
	[CliOption("out")]
	public FileInfo? Out { get; set; }
}
