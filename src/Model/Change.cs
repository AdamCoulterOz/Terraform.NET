using System.Text.Json.Serialization;

namespace TF.Model;
public class Change
{
	[JsonPropertyName("actions")]
	public required List<ChangeAction> Actions { get; init; }

	[JsonPropertyName("before")]
	public required dynamic Before { get; init; }

	[JsonPropertyName("after")]
	public required dynamic After { get; init; }

	[JsonPropertyName("replace_paths")]
	public required List<List<dynamic>> ReplacePaths { get; init; }
}
