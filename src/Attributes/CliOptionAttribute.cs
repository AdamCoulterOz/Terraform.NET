namespace TF.Attributes;

public class CliOptionAttribute : CliNamedAttribute
{
	public CliOptionAttribute(string name) : base(name) { }

	public static string BuildArguments(ICliAttributed actions)
		=> BuildArguments<CliOptionAttribute>(actions);
}
