using System.Text.Json.Serialization;

namespace TF.Model;
/// <summary>
///     Action to take for one instance object
/// </summary>
public class ResourceChange
{
	/// <summary>
	///     Resource instance to change
	/// </summary>
	[JsonPropertyName("address")]
	public required string ResourceAddress { get; init; }


	[JsonPropertyName("mode")]
	public Mode Mode { get; set; }

	/// <summary>
	///     If set, indicates the resource being superceeded
	/// </summary>
	[JsonPropertyName("deposed")]
	public string? Deposed { get; set; }

	/// <summary>
	///     Change to be applied
	/// </summary>
	[JsonPropertyName("change")]
	public required Change Change { get; init; }

	/// <summary>
	///     Special reason for why the resource needed to be replaced (if applicable)
	/// </summary>
	[JsonPropertyName("action_reason")]
	public ActionReason? ActionReason { get; set; }
}
