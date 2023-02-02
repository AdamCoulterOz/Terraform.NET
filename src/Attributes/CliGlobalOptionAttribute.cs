namespace TF.Attributes;

public class CliGlobalOptionAttribute : CliNamedAttribute
{
	public CliGlobalOptionAttribute(string name) : base(name) { }

	public static string BuildArguments(ICliAttributed actions)
		=> BuildArguments<CliGlobalOptionAttribute>(actions);
}
