namespace TF;
public static class TerraformAttributeExtensions
{
	public static Dictionary<string, string> TFKeys(this object? item)
		=> item.Keys(TerraformAttribute.FieldType.Name);

	public static Dictionary<string, string> EnvKeys(this object? item)
		=> item.Keys(TerraformAttribute.FieldType.Env);

	private static Dictionary<string, string> Keys(this object? item, TerraformAttribute.FieldType type)
	{
		var keyValues = new Dictionary<string, string>();
		if (item is null) return keyValues;
		var itemType = item.GetType();
		var itemProperties = itemType.GetProperties();
		foreach (var property in itemProperties)
		{
			var tfProp = (TerraformAttribute?)property.GetCustomAttributes(typeof(TerraformAttribute), true)
				.FirstOrDefault();
			if (tfProp is null) continue;

			var rawValue = property.GetValue(item);
			if (rawValue == null) continue;

			var value = rawValue.ToString();
			if (!string.IsNullOrEmpty(value))
			{
				if (tfProp.Lower) value = value.ToLower();
				keyValues.Add(tfProp.Get(type), value);
			}
		}

		return keyValues;
	}
}