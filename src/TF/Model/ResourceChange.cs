namespace TF.Model;

public class ResourceChange
{
	public required string Address { get; set; }
	public required Change Change { get; set; }
	public required Mode Mode { get; set; }
	public string? Deposed { get; set; }
	public ActionReason? ActionReason { get; set; }
}
