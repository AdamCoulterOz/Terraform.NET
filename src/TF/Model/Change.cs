namespace TF.Model;
public class Change
{
	public required List<ChangeAction> Actions { get; set; }
	public required dynamic Before { get; set; }
	public required dynamic After { get; set; }
	public required List<List<dynamic>> ReplacePaths { get; set; }
}
