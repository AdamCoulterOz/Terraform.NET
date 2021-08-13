namespace TF;
[AttributeUsage(AttributeTargets.Property)]
internal class TerraformAttribute : Attribute
{
	public enum FieldType { Name, Env }
	public TerraformAttribute(string name, string? env = null)
	{
		Name = name;
		Env = env;
		Lower = false;
	}

	public string Get(FieldType type) => type switch
	{
		FieldType.Name => Name,
		FieldType.Env => Env ?? throw new Exception("Env value not set"),
		_ => throw new Exception($"Invalid switch value {type}")
	};

	public string Name { get; }
	public string? Env { get; }
	public bool Lower { get; set; }
}
