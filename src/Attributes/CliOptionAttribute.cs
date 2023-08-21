namespace TF.Attributes;

public class CliOptionAttribute(string name) : CliNamedAttribute(name)
{
    public static string BuildArguments(ICliAttributed actions)
		=> BuildArguments<CliOptionAttribute>(actions);
}
