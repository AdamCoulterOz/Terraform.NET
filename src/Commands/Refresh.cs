namespace TF.Commands;

/// <summary>
/// Update the state file of your infrastructure with metadata that matches the physical resources they are tracking.
/// <br />
/// This will not modify your infrastructure, but it can modify your state file to update metadata.
/// This metadata might cause new changes to occur when you generate a plan or call apply next.
/// </summary>
public sealed class Refresh : PlanApply
{
	protected override string Command => "refresh";
}
