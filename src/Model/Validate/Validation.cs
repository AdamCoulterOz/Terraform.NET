using System.Text.Json.Serialization;

namespace TF.Model.Validate;

public class Validation : IOutput
{
	[JsonPropertyName("format_version")]
	public required string FormatVersion { get; init; }

	/// <summary>
	/// Summarizes the overall validation result, by indicating true if Terraform considers the current configuration to be valid or false if it detected any errors.
	/// </summary>
	[JsonPropertyName("valid")]
	public required bool Valid { get; init; }

	/// <summary>
	/// A zero or positive whole number giving the count of errors Terraform detected. If valid is true then error_count will always be zero, because it is the presence of errors that indicates that a configuration is invalid.
	/// </summary>
	[JsonPropertyName("error_count")]
	public required int ErrorCount { get; init; }

	/// <summary>
	/// A zero or positive whole number giving the count of warnings Terraform detected. Warnings do not cause Terraform to consider a configuration to be invalid, but they do indicate potential caveats that a user should consider and possibly resolve.
	/// </summary>
	[JsonPropertyName("warning_count")]
	public required int WarningCount { get; init; }

	/// <summary>
	/// A JSON array of nested objects that each describe an error or warning from Terraform.
	/// </summary>
	[JsonPropertyName("diagnostics")]
	public required Diagnostic[] Diagnostics { get; init; }
}
