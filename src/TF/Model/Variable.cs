namespace TF.Model;
public class Variable
{
	public required TFType Type { get; set; }
	public TFValue? Default { get; set; }
	public string? Description { get; set; }
	public string? Validation { get; set; }
	public bool? Sensitive { get; set; }
}
