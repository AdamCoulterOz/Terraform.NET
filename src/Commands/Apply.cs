using TF.Attributes;

namespace TF.Commands;

/// <summary>
///   Creates or updates infrastructure according to Terraform configuration files in the current directory.
/// </summary>
/// <remarks>
///  By default, Terraform will generate a new plan and present it for your approval before taking any action. You can optionally provide a plan file created by a previous call to "terraform plan", in which case Terraform will take the actions described in that plan without any confirmation prompt.
/// </remarks>
public class Apply : PlanApply
{
	protected override string Command => "apply";

	[CliArgument]
	public FileInfo? PlanFile { get; set; }

	/// <summary>Skip interactive approval before applying.</summary>
	[CliOption("auto-approve")]
	public bool? AutoApprove { get; set; }

	/// <summary>Path to backup the existing state file before modifying. Defaults to the "-state-out" path with ".backup" extension. Set to "-" to disable backup.</summary>
	[CliOption("backup")]
	public string? Backup { get; set; }

	/// <summary>Path to write state to that is different than "-state". This can be used to preserve the old state.</summary>
	[CliOption("state-out")]
	public DirectoryInfo? StateOut { get; set; }
}
