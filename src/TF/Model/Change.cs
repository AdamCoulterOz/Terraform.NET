namespace TF.Model;
public class Change
{
	public required List<ChangeAction> Actions { get; set; }
	public required TFValue Before { get; set; }
	public required TFValue After { get; set; }
	public required List<List<TFValue>> ReplacePaths { get; set; }
}
