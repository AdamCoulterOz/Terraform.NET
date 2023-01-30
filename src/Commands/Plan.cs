using TF.Attributes;

namespace TF.Commands;

/// <summary>
///   Generates a speculative execution plan, showing what actions Terraform would take to apply the current configuration. This command will not actually perform the planned actions.
/// </summary>
/// <remarks>
///   You can optionally save the plan to a file, which you can then pass to the "apply" command to perform exactly the actions described in the plan.
/// </remarks>
public class Plan : PlanApply
{
	protected override string Command => "plan";

	/// <summary>Return a detailed exit code when the command exits. This will change the meaning of exit codes to:  0 - Succeeded, diff is empty (no changes), 1 - Errored, 2 - Succeeded, there is a diff.</summary>
	[CliOption("detailed-exitcode")]
	public bool? DetailedExitCode { get; set; }

	/// <summary>Write a plan file to the given path. This can be used as input to the "apply" command.</summary>
	[CliOption("out")]
	public string? Out { get; set; }
}
