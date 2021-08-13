using System.Text.Json.Serialization;

namespace TF.Model;
public class Plan
{
	[JsonPropertyName("format_version")] public string FormatVersion { get; set; } = null!;

	//[JsonPropertyName("prior_state")]
	//public State PriorState;
	//[JsonPropertyName("configuration")]
	//public Configuration Configuration;
	//[JsonPropertyName("planned_values")]
	//public Values PlannedValues;
	//[JsonPropertyName("proposed_unknown")]
	//public Values ProposedUnknown;

	/// <summary>
	///     Map of variables given as input
	///     Key is the variable name
	/// </summary>
	[JsonPropertyName("variables")]
	public Dictionary<string, Variable> Variables { get; set; } = null!;

	/// <summary>
	///     Collection of resource changes to move from existing to target state
	/// </summary>
	[JsonPropertyName("resource_changes")]
	public List<ResourceChange> ResourceChanges { get; set; } = null!;

	/// <summary>
	///     Planned changes to the output values
	///     Key is the output name
	/// </summary>
	[JsonPropertyName("output_changes")]
	public Dictionary<string, Change> OutputChanges { get; set; } = null!;
}
