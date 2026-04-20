namespace TF.Model;
public class Variable
{
	public required string Type { get; set; }
	public dynamic? Default { get; set; }
	public string? Description { get; set; }
	public string? Validation { get; set; }
	public bool? Sensitive { get; set; }
}
