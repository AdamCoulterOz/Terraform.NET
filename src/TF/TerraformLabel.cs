using System.Text.RegularExpressions;

namespace TF;

public readonly partial record struct TerraformLabel
{
	private readonly string _value;

	private TerraformLabel(string value)
		=> _value = value;

	public static TerraformLabel From(string value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(value);

		var label = LabelUnsafeCharacters().Replace(value, "_");
		if (string.IsNullOrWhiteSpace(label))
			throw new ArgumentException("Terraform label must contain at least one usable character.", nameof(value));

		if (char.IsDigit(label[0]))
			label = $"pkg_{label}";

		return new TerraformLabel(label);
	}

	public override string ToString() => _value;

	public static implicit operator string(TerraformLabel label) => label._value;

	[GeneratedRegex("[^0-9A-Za-z_]")]
	private static partial Regex LabelUnsafeCharacters();
}
