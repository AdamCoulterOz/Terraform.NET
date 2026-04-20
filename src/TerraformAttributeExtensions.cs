using System.Security;
using System.Text.Json.Nodes;

namespace TF;
public static class TerraformAttributeExtensions
{
	public static Dictionary<string, string> TFKeys(this object? item)
		=> item.Keys(TerraformAttribute.FieldType.Name);

	public static Dictionary<string, string> EnvKeys(this object? item)
		=> item.Keys(TerraformAttribute.FieldType.Env);

	public static Dictionary<string, JsonNode> TFNodes(this object? item)
		=> item.Nodes();

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

	private static Dictionary<string, JsonNode> Nodes(this object? item)
	{
		var keyValues = new Dictionary<string, JsonNode>();
		if (item is null) return keyValues;
		var itemType = item.GetType();
		var itemProperties = itemType.GetProperties();
		foreach (var property in itemProperties)
		{
			var tfProp = (TerraformAttribute?)property.GetCustomAttributes(typeof(TerraformAttribute), true)
				.FirstOrDefault();
			if (tfProp is null) continue;

			var rawValue = property.GetValue(item);
			if (rawValue is null) continue;

			keyValues.Add(tfProp.Name, ToJsonNode(rawValue, tfProp));
		}

		return keyValues;
	}

	private static JsonNode ToJsonNode(object rawValue, TerraformAttribute attribute)
	{
		switch (rawValue)
		{
			case string value:
				return JsonValue.Create(value)!;
			case bool value:
				return JsonValue.Create(value)!;
			case Guid value:
				return JsonValue.Create(attribute.Lower ? value.ToString().ToLowerInvariant() : value.ToString())!;
			case Uri value:
				return JsonValue.Create(value.ToString())!;
			case FileInfo value:
				return JsonValue.Create(value.FullName)!;
			case DirectoryInfo value:
				return JsonValue.Create(value.FullName)!;
			case SecureString value:
				return JsonValue.Create(new System.Net.NetworkCredential(string.Empty, value).Password)!;
			case Enum value:
				return JsonValue.Create(attribute.Lower ? value.ToString().ToLowerInvariant() : value.ToString())!;
			case sbyte value:
				return JsonValue.Create(value)!;
			case byte value:
				return JsonValue.Create(value)!;
			case short value:
				return JsonValue.Create(value)!;
			case ushort value:
				return JsonValue.Create(value)!;
			case int value:
				return JsonValue.Create(value)!;
			case uint value:
				return JsonValue.Create(value)!;
			case long value:
				return JsonValue.Create(value)!;
			case ulong value:
				return JsonValue.Create(value)!;
			case float value:
				return JsonValue.Create(value)!;
			case double value:
				return JsonValue.Create(value)!;
			case decimal value:
				return JsonValue.Create(value)!;
			default:
				return JsonValue.Create(rawValue.ToString())!;
		}
	}
}
