using System.Text.Json.Serialization;

namespace TF.Model;
public class Plan : IOutput
{
	[JsonPropertyName("format_version")]
	public required string FormatVersion { get; init; }

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
	public required Dictionary<string, Variable> Variables { get; init; }

	/// <summary>
	///     Collection of resource changes to move from existing to target state
	/// </summary>
	[JsonPropertyName("resource_changes")]
	public required List<ResourceChange> ResourceChanges { get; init; }

	/// <summary>
	///     Planned changes to the output values
	///     Key is the output name
	/// </summary>
	[JsonPropertyName("output_changes")]
	public required Dictionary<string, Change> OutputChanges { get; init; }
}
