namespace TF.Model;
public class Plan
{
	public required string FormatVersion { get; set; }
	public required Dictionary<string, Variable> Variables { get; set; }
	public required List<ResourceChange> ResourceChanges { get; set; }
	public required Dictionary<string, Change> OutputChanges { get; set; }

	//public State PriorState;
	//public Configuration Configuration;
	//public Values PlannedValues;
	//public Values ProposedUnknown;
}
