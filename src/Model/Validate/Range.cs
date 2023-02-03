using System.Text.Json.Serialization;

namespace TF.Model.Validate;

/// <summary>
/// A range object describes a range of source code within a file.
/// The range is given as a start position and an end position,
/// the start is inclusive and the end exclusive.
/// </summary>
public class Range
{
	/// <summary>
	/// The relative path from the current working directory
	/// </summary>
	[JsonPropertyName("filename")]
	public required string FileName { get; init; }

	/// <summary>
	/// Start source position (inclusive)
	/// </summary>
	[JsonPropertyName("start")]
	public required SourcePosition Start { get; init; }

	/// <summary>
	/// End source position (exclusive)
	/// </summary>
	[JsonPropertyName("end")]
	public required SourcePosition End { get; init; }
}
