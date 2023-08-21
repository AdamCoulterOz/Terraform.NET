namespace TF.Extensions;
internal static class IEnumerableExtensions
{
	public static IDictionary<TKey, TValue> Merge<TKey, TValue>(
		this IDictionary<TKey, TValue> first,
		IDictionary<TKey, TValue> second,
		bool overwrite = true)
	{
		if (second is null) return first;
		foreach (var item in second)
		{
			if (first.TryGetValue(item.Key, out var firstVal)
				&& !overwrite && !Equals(firstVal, item.Value))
				throw new ArgumentException($"""
					Key already exists in dictionary: {item.Key}.
					Existing Value: {firstVal}, New value: {item.Value}
					""");
			first[item.Key] = item.Value;
		}
		return first;
	}

	public static string Join<T>(this IEnumerable<T> source, char separator)
		=> string.Join(separator, source);
}