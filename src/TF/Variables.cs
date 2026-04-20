using System.Collections.ObjectModel;
using System.Text.Json;

namespace TF;
public class Variables
{
	public Variables() => VariableDictionary = [];

	public Dictionary<string, TFValue> VariableDictionary { get; set; }

	public TFValue? this[string name]
	{
		get => VariableDictionary.TryGetValue(name, out var value) ? value : null;
		set => VariableDictionary[name] = value?.DeepClone() ?? TFValue.Null;
	}

	public void Set<T>(string name, T value)
		=> VariableDictionary[name] = TFValue.From(value);

	public ReadOnlyCollection<string> Arguments
	{
		get
		{
			var variables = new List<string>();
			foreach (var (key, value) in VariableDictionary)
			{
				var newValue = JsonSerializer.Serialize(value).Trim('"');
				var quote = newValue.Contains('"') ? '\'' : '"';
				variables.Add($"-var={quote}{key}={newValue}{quote}");
			}
			return new ReadOnlyCollection<string>(variables);
		}
	}

	public string VariableJson => JsonSerializer.Serialize(VariableDictionary);
}
