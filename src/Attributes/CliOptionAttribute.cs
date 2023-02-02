using System.Reflection;

namespace TF.Attributes;

public interface ICliAttributed { }

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public abstract class CliAttribute : Attribute { }

public class CliNamedAttribute : CliAttribute
{
	public string Name { get; }

	public CliNamedAttribute(string name)
	{
		Name = name;
	}

	public static string BuildArguments<T>(ICliAttributed item)
				where T : CliNamedAttribute
	{
		var variables = BuildVariables<T>(item);
		var arguments = new List<string>();
		foreach (var (var, value) in variables)
		{
			arguments.Add(value switch
			{
				ICollection<string> c => string.Join(" ", c.Select(i => $"-{var}={i}")),
				bool b => $"-{var}={b.ToString().ToLowerInvariant()}",
				_ => $"-{var}={value}"
			});
		}
		return string.Join(" ", arguments);
	}

	public static Dictionary<string, object> BuildVariables<T>(ICliAttributed item)
				where T : CliNamedAttribute
	{
		var variables = new Dictionary<string, object>();
		var properties = item.GetType().GetProperties();

		foreach (var property in properties)
		{
			var attr = property.GetCustomAttribute<T>();
			var value = property.GetValue(item);
			if (attr == null || value == null) continue;
			variables.Add(attr.Name, value);
		}
		return variables;
	}

	public static Dictionary<string, string> BuildVariables(ICliAttributed item)
		=> BuildVariables<CliNamedAttribute>(item).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()!);
}

public class CliOptionAttribute : CliNamedAttribute
{
	public CliOptionAttribute(string name) : base(name) { }

	public static string BuildArguments(ICliAttributed actions)
		=> BuildArguments<CliOptionAttribute>(actions);
}

public class CliGlobalOptionAttribute : CliNamedAttribute
{
	public CliGlobalOptionAttribute(string name) : base(name) { }

	public static string BuildArguments(ICliAttributed actions)
		=> BuildArguments<CliGlobalOptionAttribute>(actions);
}

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
