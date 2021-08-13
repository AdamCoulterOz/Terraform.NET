using System.Text.Json.Serialization;

namespace TF.Model;
public class Variable
{
	/// <summary>
	///     Default value which then makes the variable optional
	/// </summary>
	[JsonPropertyName("default")]
	public dynamic? Default { get; set; }

	/// <summary>
	///     What value types are accepted for the variable
	/// </summary>
	[JsonPropertyName("type")]
	public string Type { get; set; } = null!;

	/// <summary>
	///     Documentation for the variable
	/// </summary>
	[JsonPropertyName("description")]
	public string Description { get; set; } = "";

	/// <summary>
	///     A block to define validation rules, usually in addition to type constraints
	/// </summary>
	[JsonPropertyName("validation")]
	public string? Validation { get; set; }

	/// <summary>
	///     Limits Terraform UI output when the variable is used in configuration
	/// </summary>
	[JsonPropertyName("sensitive")]
	public bool? Sensitive { get; set; }
}
