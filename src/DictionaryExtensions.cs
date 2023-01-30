namespace TF;
public static class DictionaryExtensions
{
	public static Dictionary<TKey, TValue> AppendDictionary<TKey, TValue>(
		this Dictionary<TKey, TValue> dictionary, Dictionary<TKey, TValue>? append)
		where TKey : notnull
		where TValue : notnull
	{
		if (append is null)
			return dictionary;
		foreach (KeyValuePair<TKey, TValue> kvp in append)
		{
			if (!dictionary.ContainsKey(kvp.Key))
			{
				dictionary.Add(kvp.Key, kvp.Value);
				continue;
			}

			var leftValue = dictionary[kvp.Key];
			var rightValue = append[kvp.Key];

			if (leftValue.Equals(rightValue))
				continue;

			throw new Exception($"Cannot resolve duplicate key {kvp.Key} when merging dictionaries. Left value: {leftValue} Right value: {rightValue}");
		}
		return dictionary;
	}
}