using TF.Attributes;

namespace TF.Commands;

public abstract class PlanApply : Main<Model.Plan>
{
	/// <summary>If Terraform produces any warnings that are not accompanied by errors, shows them in a more compact form that includes only the summary messages.</summary>
	[CliOption("compact-warnings")]
	public bool? CompactWarnings { get; set; }

	/// <summary>Destroy Terraform-managed infrastructure.</summary>
	[CliOption("destroy")]
	public bool? Destroy { get; set; }

	/// <summary>Limit the number of parallel resource operations. Defaults to 10.</summary>
	[CliOption("parallelism")]
	public int? Parallelism { get; set; }

	/// <summary>Override `terraform.tfstate` as the local backend's state file.</summary>
	[CliOption("state")]
	public string? State { get; set; }

	// Set to false to skip checking for external changes to remote objects while creating the plan. This can potentially make planning faster, but at the expense of possibly planning against a stale record of the remote system state.</summary>
	[CliOption("refresh")]
	public bool? Refresh { get; set; }

	/// <summary>Select the "refresh only" planning mode, which checks whether remote objects still match the outcome of the most recent Terraform apply but does not propose any actions to undo any changes made outside of Terraform.</summary>
	[CliOption("refresh-only")]
	public bool? RefreshOnly { get; set; }

	/// <summary>Force replacement of a particular resource instances using their resource addresses.</summary>
	/// <remarks>If the plan would've normally produced an update or no-op action for an instance, Terraform will plan to replace it instead.</remarks>
	[CliOption("replace")]
	public ICollection<string> Replace { get; set; } = new List<string>();

	/// <summary>Limit the planning operation to only the given modules, resources, and/or resource instances and all of their dependencies.</summary>
	/// <remarks>This is for exceptional use only. It is not intended to be used for normal operations.</remarks>
	[CliOption("target")]
	public ICollection<string> Targets { get; set; } = new List<string>();

	/// <summary>Set a values for the input variables in the root module of the configuration.</summary>
	public IDictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

	[CliOption("var")]
	protected ICollection<string> Vars => Variables.Select(i => $"'{i.Key}={i.Value}'").ToList();

	/// <summary>Load variable values from the given files (in addition to the default files terraform.tfvars and *.auto.tfvars).</summary>
	[CliOption("var-file")]
	public ICollection<string> VarFiles { get; set; } = new List<string>();
}
