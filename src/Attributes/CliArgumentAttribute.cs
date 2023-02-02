using System.Reflection;

namespace TF.Attributes;

public class CliArgumentAttribute : CliAttribute
{
	public static string BuildArguments(ICliAttributed action)
	{
		var property = action.GetType().GetProperties()
			.FirstOrDefault(p => p.GetCustomAttribute<CliArgumentAttribute>() != null);

		if (property is null) return "";

		var value = property.GetValue(action);
		return value is string s ? s : "";
	}
}
