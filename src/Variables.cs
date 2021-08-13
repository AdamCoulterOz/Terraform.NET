using System.Collections.ObjectModel;
using System.Text.Json;

namespace TF;
public class Variables
{
	public Variables()
        => VariableDictionary = new Dictionary<string, object>();

	public Dictionary<string, object> VariableDictionary { get; set; }

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
