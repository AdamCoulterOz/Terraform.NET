using System.Reflection;

namespace TF.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public abstract class CliAttribute : Attribute { }

public abstract class CliNamedAttribute : CliAttribute
{
	public string Name { get; }

	public CliNamedAttribute(string name)
	{
		Name = name;
	}

	public static string BuildArguments<T>(Commands.Action action)
				where T : CliNamedAttribute
	{
		var arguments = new List<string>();
		var properties = action.GetType().GetProperties();

		foreach (var property in properties)
		{
			var attr = property.GetCustomAttribute<T>();
			var value = property.GetValue(action);
			if (attr == null || value == null) continue;

			arguments.Add(value switch
			{
				ICollection<string> c => string.Join(" ", c.Select(i => $"-{attr.Name}={i}")),
				bool b => $"-{attr.Name}={b.ToString().ToLowerInvariant()}",
				_ => $"-{attr.Name}={value}"
			});
		}

		return string.Join(" ", arguments);
	}
}

public class CliOptionAttribute : CliNamedAttribute
{
	public CliOptionAttribute(string name) : base(name) { }

	public static string BuildArguments(Commands.Action actions)
		=> BuildArguments<CliOptionAttribute>(actions);
}

public class CliGlobalOptionAttribute : CliNamedAttribute
{
	public CliGlobalOptionAttribute(string name) : base(name) { }

	public static string BuildArguments(Commands.Action actions)
		=> BuildArguments<CliGlobalOptionAttribute>(actions);
}

public class CliArgumentAttribute : CliAttribute
{
	public static string BuildArguments(Commands.Action action)
	{
		var property = action.GetType().GetProperties()
			.FirstOrDefault(p => p.GetCustomAttribute<CliArgumentAttribute>() != null);

		if (property is null) return "";

		var value = property.GetValue(action);
		return value is string s ? s : "";
	}
}
