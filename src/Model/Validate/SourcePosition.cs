using System.Text.Json.Serialization;

namespace TF.Model.Validate;

/// <summary>
/// A start position is inclusive while an end position is exclusive. The exact positions used for particular error messages are intended for human interpretation only.
/// </summary>
public class SourcePosition
{
	/// <summary>
	/// A one-based line count for the line containing the relevant position in the indicated file.
	/// </summary>
	[JsonPropertyName("line")]
	public int Line { get; set; }

	/// <summary>
	/// A one-based count of Unicode characters from the start of the line indicated in <see cref="Line"/>.
	/// </summary>
	[JsonPropertyName("column")]
	public int Column { get; set; }

	/// <summary>
	/// A zero-based byte offset into the indicated file.
	/// </summary>
	[JsonPropertyName("byte")]
	public int Byte { get; set; }
}
