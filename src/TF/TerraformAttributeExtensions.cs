using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TF;

public static class TerraformAttributeExtensions
{
	public static Dictionary<string, string> TFKeys(this object? item)
		=> item.Keys(TerraformAttribute.FieldType.Name);

	public static Dictionary<string, string> EnvKeys(this object? item)
		=> item.Keys(TerraformAttribute.FieldType.Env);

	public static Dictionary<string, JsonValue> TFNodes(this object? item)
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

	private static Dictionary<string, JsonValue> Nodes(this object? item)
	{
		var keyValues = new Dictionary<string, JsonValue>();
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

			keyValues.Add(tfProp.Name, ToJsonValue(rawValue, tfProp));
		}
		return keyValues;
	}

	private static JsonValue ToJsonValue(object rawValue, TerraformAttribute attribute)
	{
		if (TryGetStringValue(rawValue, attribute, out var stringValue))
			return JsonValue.Create(stringValue)
				?? throw new InvalidOperationException($"Unable to serialize Terraform attribute '{attribute.Name}' as a string value.");

		var serialized = JsonSerializer.SerializeToNode(rawValue)
			?? throw new InvalidOperationException(
				$"Unable to serialize Terraform attribute '{attribute.Name}' from type '{rawValue.GetType().FullName}'.");

		return serialized as JsonValue
			?? throw new InvalidOperationException(
				$"Terraform attribute '{attribute.Name}' from type '{rawValue.GetType().FullName}' serialized as '{serialized.GetType().Name}'. Only scalar values are supported.");
	}

	private static bool TryGetStringValue(object rawValue, TerraformAttribute attribute, out string value)
	{
		value = rawValue switch
		{
			FileSystemInfo fileSystemInfo => fileSystemInfo.FullName,
			Uri uri => uri.ToString(),
			SecureString secureString => new System.Net.NetworkCredential(string.Empty, secureString).Password,
			Guid guid => Format(guid.ToString(), attribute),
			Enum enumValue => Format(enumValue.ToString(), attribute),
			_ => string.Empty,
		};
		return value.Length > 0;
	}

	private static string Format(string value, TerraformAttribute attribute)
		=> attribute.Lower ? value.ToLowerInvariant() : value;
}
