using System.Text.Json.Serialization;

namespace TF.Model.Validate;

public class Diagnostic
{
	/// <summary>
	/// Indicates if this diagnostic is an "error" or "warning".
	/// </summary>
	/// <remarks>
	/// The presence of errors causes Terraform to consider a configuration to be invalid, while warnings are just advice or caveats to the user which do not block working with the configuration. Later versions of Terraform may introduce new severity keywords, so consumers should be prepared to accept and ignore severity values they don't understand.
	/// </remarks>
	[JsonPropertyName("severity"), JsonConverter(typeof(JsonStringEnumConverter))]
	public required DiagnosticSeverity Severity { get; init; }
	public enum DiagnosticSeverity { Error, Warning }

	/// <summary>
	/// A short description of the nature of the problem that the diagnostic is reporting.
	/// </summary>
	/// <remarks>
	/// In Terraform's usual human-oriented diagnostic messages, the summary serves as a sort of "heading" for the diagnostic, printed after the "Error:" or "Warning:" indicator.
	/// <br/>
	/// Summaries are typically short, single sentences, but can sometimes be longer as a result of returning errors from subsystems that are not designed to return full diagnostics, where the entire error message therefore becomes the summary. In those cases, the summary might include newline characters which a renderer should honor when presenting the message visually to a user.
	/// </remarks>
	[JsonPropertyName("summary")]
	public required string Summary { get; init; }

	/// <summary>
	/// Additional message giving more detail about the problem.
	/// </summary>
	/// <remarks>
	/// In Terraform's usual human-oriented diagnostic messages, the detail provides the paragraphs of text that appear after the heading and the source location reference.
	/// <br/>
	/// Detail messages are often multiple paragraphs and possibly interspersed with non-paragraph lines, so tools which aim to present detail messages to the user should distinguish between lines without leading spaces, treating them as paragraphs, and lines with leading spaces, treating them as preformatted text. Renderers should then soft-wrap the paragraphs to fit the width of the rendering container, but leave the preformatted lines unwrapped.
	/// <br/>
	/// Some Terraform detail messages contain an approximation of bullet lists using ASCII characters to mark the bullets. This is not a contractural formatting convention, so renderers should avoid depending on it and should instead treat those lines as either paragraphs or preformatted text. Future versions of this format may define additional rules for other text conventions, but will maintain backward compatibility.
	/// </remarks>
	[JsonPropertyName("detail")]
	public string? Detail { get; init; }

	/// <summary>
	/// References a portion of the configuration source code that the diagnostic message relates to.
	/// </summary>
	/// <remarks>
	/// For errors, this will typically indicate the bounds of the specific block header, attribute, or expression which was detected as invalid.
	/// <br/>
	/// Not all diagnostic messages are connected with specific portions of the configuration.
	/// </remarks>
	[JsonPropertyName("range")]
	public Range? Range { get; init; }

	/// <summary>
	/// Excerpt of the configuration source code that the diagnostic message relates to.
	/// </summary>
	[JsonPropertyName("snippet")]
	public Snippet? Snippet { get; init; }
}
