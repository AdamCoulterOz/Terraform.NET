namespace TF;
[AttributeUsage(AttributeTargets.Property)]
public class TerraformAttribute(string name, string? env = null) : Attribute
{
	public enum FieldType { Name, Env }

    public string Get(FieldType type) => type switch
	{
		FieldType.Name => Name,
		FieldType.Env => Env ?? throw new Exception("Env value not set"),
		_ => throw new Exception($"Invalid switch value {type}")
	};

    public string Name { get; } = name;
    public string? Env { get; } = env;
    public bool Lower { get; set; } = false;
}
