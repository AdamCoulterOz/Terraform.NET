using System.Text.Json.Serialization;

namespace TF.Model;
public class Change
{
	[JsonPropertyName("actions")] public List<ChangeAction> Actions { get; set; } = null!;

	[JsonPropertyName("before")] public dynamic Before { get; set; } = null!;

	[JsonPropertyName("after")] public dynamic After { get; set; } = null!;

	[JsonPropertyName("replace_paths")] public List<List<dynamic>> ReplacePaths { get; set; } = null!;
}
