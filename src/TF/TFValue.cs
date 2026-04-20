using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TF;

public enum TFValueKind
{
	Null,
	String,
	Boolean,
	Number,
	Array,
	Object,
	Expression
}

[JsonConverter(typeof(TFValueJsonConverter))]
public abstract class TFValue
{
	public static TFNull Null { get; } = new();

	public abstract TFValueKind Kind { get; }

	public abstract object? UntypedValue { get; }

	public abstract TFValue DeepClone();

	public abstract JsonNode? ToJsonNode();

	public T GetValue<T>()
	{
		if (TryGetValue<T>(out var value))
			return value;

		throw new InvalidOperationException(
			$"Terraform value of kind '{Kind}' does not contain a '{typeof(T).FullName}'.");
	}

	public bool TryGetValue<T>(out T value)
	{
		if (this is TFNumber number && TryConvertNumber(number.Value, out value))
			return true;

		if (this is TFValue<T> typed)
		{
			value = typed.Value;
			return true;
		}

		if (UntypedValue is T raw)
		{
			value = raw;
			return true;
		}

		value = default!;
		return false;
	}

	private static bool TryConvertNumber<T>(decimal number, out T value)
	{
		object? converted = typeof(T) switch
		{
			var type when type == typeof(decimal) => number,
			var type when type == typeof(double) => (double)number,
			var type when type == typeof(float) => (float)number,
			var type when type == typeof(long) && decimal.Truncate(number) == number && number >= long.MinValue && number <= long.MaxValue => (long)number,
			var type when type == typeof(int) && decimal.Truncate(number) == number && number >= int.MinValue && number <= int.MaxValue => (int)number,
			var type when type == typeof(short) && decimal.Truncate(number) == number && number >= short.MinValue && number <= short.MaxValue => (short)number,
			var type when type == typeof(byte) && decimal.Truncate(number) == number && number >= byte.MinValue && number <= byte.MaxValue => (byte)number,
			var type when type == typeof(sbyte) && decimal.Truncate(number) == number && number >= sbyte.MinValue && number <= sbyte.MaxValue => (sbyte)number,
			var type when type == typeof(ushort) && decimal.Truncate(number) == number && number >= ushort.MinValue && number <= ushort.MaxValue => (ushort)number,
			var type when type == typeof(uint) && decimal.Truncate(number) == number && number >= uint.MinValue && number <= uint.MaxValue => (uint)number,
			var type when type == typeof(ulong) && decimal.Truncate(number) == number && number >= ulong.MinValue && number <= ulong.MaxValue => (ulong)number,
			_ => null,
		};

		if (converted is T typed)
		{
			value = typed;
			return true;
		}

		value = default!;
		return false;
	}

	public string ToJsonString(JsonSerializerOptions? options = null)
		=> ToJsonNode()?.ToJsonString(options) ?? "null";

	public override string ToString() => ToJsonString();

	public static TFValue Expression(string expression)
		=> new TFExpression(expression);

	public static TFValue From<T>(T value)
		=> FromObject(value);

	internal static TFValue FromTerraformAttribute(object rawValue, TerraformAttribute attribute)
		=> FromObject(rawValue, new TFValueConversionOptions(attribute.Lower));

	internal static TFValue FromJsonNode(JsonNode? node)
	{
		if (node is null)
			return Null;

		return FromJsonElement(JsonSerializer.SerializeToElement(node));
	}

	internal static TFValue FromJsonElement(JsonElement element)
		=> element.ValueKind switch
		{
			JsonValueKind.Null or JsonValueKind.Undefined => Null,
			JsonValueKind.String => new TFString(element.GetString() ?? string.Empty),
			JsonValueKind.True => new TFBool(true),
			JsonValueKind.False => new TFBool(false),
			JsonValueKind.Number => CreateNumberValue(element),
			JsonValueKind.Array => new TFArray(element.EnumerateArray().Select(FromJsonElement)),
			JsonValueKind.Object => new TFObject(
				element.EnumerateObject()
					.ToDictionary(property => property.Name, property => FromJsonElement(property.Value), StringComparer.Ordinal)),
			_ => throw new InvalidOperationException($"Unsupported JSON value kind '{element.ValueKind}'."),
		};

	public static TFValue FromObject(object? value)
		=> FromObject(value, TFValueConversionOptions.Default);

	internal static TFValue FromObject(object? value, TFValueConversionOptions options)
	{
		if (value is null)
			return Null;

		if (value is TFValue tfValue)
			return tfValue.DeepClone();

		if (value is JsonElement jsonElement)
			return FromJsonElement(jsonElement);

		if (value is JsonNode jsonNode)
			return FromJsonNode(jsonNode);

		if (TryCreateScalar(value, options, out var scalar))
			return scalar;

		if (TryCreateObject(value, options, out var obj))
			return obj;

		if (TryCreateArray(value, options, out var array))
			return array;

		throw new InvalidOperationException(
			$"Type '{value.GetType().FullName}' cannot be represented as a Terraform value.");
	}

	private static bool TryCreateScalar(object value, TFValueConversionOptions options, out TFValue scalar)
	{
		scalar = value switch
		{
			string text => new TFString(Format(text, options)),
			bool boolean => new TFBool(boolean),
			sbyte number => new TFNumber(number),
			byte number => new TFNumber(number),
			short number => new TFNumber(number),
			ushort number => new TFNumber(number),
			int number => new TFNumber(number),
			uint number => new TFNumber(number),
			long number => new TFNumber(number),
			ulong number => new TFNumber(number),
			float number => new TFNumber(ToDecimal(number)),
			double number => new TFNumber(ToDecimal(number)),
			decimal number => new TFNumber(number),
			Guid guid => new TFString(Format(guid.ToString(), options)),
			Uri uri => new TFString(Format(uri.ToString(), options)),
			FileInfo file => new TFString(Format(file.FullName, options)),
			DirectoryInfo directory => new TFString(Format(directory.FullName, options)),
			SecureString secureString => new TFString(
				Format(new System.Net.NetworkCredential(string.Empty, secureString).Password, options)),
			Enum enumValue => new TFString(Format(enumValue.ToString(), options)),
			_ => Null,
		};

		return scalar is not TFNull;
	}

	private static bool TryCreateObject(object value, TFValueConversionOptions options, out TFValue obj)
	{
		if (value is TFObject tfObject)
		{
			obj = tfObject.DeepClone();
			return true;
		}

		if (value is IDictionary<string, TFValue> tfValues)
		{
			obj = new TFObject(tfValues.ToDictionary(entry => entry.Key, entry => entry.Value.DeepClone(), StringComparer.Ordinal));
			return true;
		}

		if (value is IReadOnlyDictionary<string, TFValue> readonlyTfValues)
		{
			obj = new TFObject(readonlyTfValues.ToDictionary(entry => entry.Key, entry => entry.Value.DeepClone(), StringComparer.Ordinal));
			return true;
		}

		if (value is IDictionary<string, object?> values)
		{
			obj = new TFObject(values.ToDictionary(entry => entry.Key, entry => FromObject(entry.Value, options), StringComparer.Ordinal));
			return true;
		}

		if (value is IReadOnlyDictionary<string, object?> readonlyValues)
		{
			obj = new TFObject(readonlyValues.ToDictionary(entry => entry.Key, entry => FromObject(entry.Value, options), StringComparer.Ordinal));
			return true;
		}

		if (value is IDictionary dictionary)
		{
			var properties = new Dictionary<string, TFValue>(StringComparer.Ordinal);
			foreach (DictionaryEntry entry in dictionary)
			{
				if (entry.Key is not string key)
					throw new InvalidOperationException("Terraform object keys must be strings.");

				properties[key] = FromObject(entry.Value, options);
			}

			obj = new TFObject(properties);
			return true;
		}

		obj = Null;
		return false;
	}

	private static bool TryCreateArray(object value, TFValueConversionOptions options, out TFValue array)
	{
		if (value is TFArray tfArray)
		{
			array = tfArray.DeepClone();
			return true;
		}

		if (value is IEnumerable<TFValue> tfValues)
		{
			array = new TFArray(tfValues.Select(item => item.DeepClone()));
			return true;
		}

		if (value is string or IDictionary)
		{
			array = Null;
			return false;
		}

		if (value is IEnumerable enumerable)
		{
			var items = new List<TFValue>();
			foreach (var item in enumerable)
				items.Add(FromObject(item, options));

			array = new TFArray(items);
			return true;
		}

		array = Null;
		return false;
	}

	private static TFValue CreateNumberValue(JsonElement element)
	{
		if (element.TryGetDecimal(out var decimalValue))
			return new TFNumber(decimalValue);

		return new TFNumber(decimal.Parse(element.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture));
	}

	private static string Format(string value, TFValueConversionOptions options)
		=> options.LowercaseStrings ? value.ToLowerInvariant() : value;

	private static decimal ToDecimal(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
			throw new InvalidOperationException("Terraform numbers cannot represent NaN or infinity.");

		return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
	}

	private static decimal ToDecimal(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
			throw new InvalidOperationException("Terraform numbers cannot represent NaN or infinity.");

		return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
	}

	public static implicit operator TFValue(string value) => From(value);
	public static implicit operator TFValue(bool value) => From(value);
	public static implicit operator TFValue(sbyte value) => From(value);
	public static implicit operator TFValue(byte value) => From(value);
	public static implicit operator TFValue(short value) => From(value);
	public static implicit operator TFValue(ushort value) => From(value);
	public static implicit operator TFValue(int value) => From(value);
	public static implicit operator TFValue(uint value) => From(value);
	public static implicit operator TFValue(long value) => From(value);
	public static implicit operator TFValue(ulong value) => From(value);
	public static implicit operator TFValue(float value) => From(value);
	public static implicit operator TFValue(double value) => From(value);
	public static implicit operator TFValue(decimal value) => From(value);
	public static implicit operator TFValue(Guid value) => From(value);
	public static implicit operator TFValue(Uri value) => From(value);
}

public abstract class TFValue<T>(T value) : TFValue
{
	public T Value { get; } = value;

	public sealed override object? UntypedValue => Value;
}

public sealed class TFNull() : TFValue<object?>(null)
{
	public override TFValueKind Kind => TFValueKind.Null;

	public override TFValue DeepClone() => this;

	public override JsonNode? ToJsonNode() => null;
}

public sealed class TFString(string value) : TFValue<string>(value)
{
	public override TFValueKind Kind => TFValueKind.String;

	public override TFValue DeepClone() => new TFString(Value);

	public override JsonNode ToJsonNode()
		=> JsonSerializer.SerializeToNode(Value)
			?? throw new InvalidOperationException("Unable to serialize Terraform string value.");

	public static implicit operator string(TFString value) => value.Value;
	public static implicit operator TFString(string value) => new(value);
}

public sealed class TFBool(bool value) : TFValue<bool>(value)
{
	public override TFValueKind Kind => TFValueKind.Boolean;

	public override TFValue DeepClone() => new TFBool(Value);

	public override JsonNode ToJsonNode()
		=> JsonSerializer.SerializeToNode(Value)
			?? throw new InvalidOperationException("Unable to serialize Terraform boolean value.");

	public static implicit operator bool(TFBool value) => value.Value;
	public static implicit operator TFBool(bool value) => new(value);
}

public sealed class TFNumber(decimal value) : TFValue<decimal>(value)
{
	public override TFValueKind Kind => TFValueKind.Number;

	public override TFValue DeepClone() => new TFNumber(Value);

	public override JsonNode ToJsonNode()
		=> JsonSerializer.SerializeToNode(Value)
			?? throw new InvalidOperationException("Unable to serialize Terraform number value.");

	public static implicit operator decimal(TFNumber value) => value.Value;
	public static implicit operator TFNumber(decimal value) => new(value);
}

public sealed class TFExpression(string value) : TFValue<string>(value)
{
	public override TFValueKind Kind => TFValueKind.Expression;

	public override TFValue DeepClone() => new TFExpression(Value);

	public override JsonNode ToJsonNode()
		=> JsonValue.Create(Value)
			?? throw new InvalidOperationException("Unable to serialize Terraform expression value.");
}

public sealed class TFArray : TFValue<IReadOnlyList<TFValue>>, IReadOnlyList<TFValue>
{
	private readonly List<TFValue> _items;

	public TFArray(IEnumerable<TFValue> items) : this(items.Select(item => item.DeepClone()).ToList())
	{
	}

	private TFArray(List<TFValue> items) : base(new ReadOnlyCollection<TFValue>(items))
		=> _items = items;

	public override TFValueKind Kind => TFValueKind.Array;

	public int Count => _items.Count;

	public TFValue this[int index] => _items[index];

	public override TFValue DeepClone() => new TFArray(_items);

	public override JsonNode ToJsonNode()
		=> new JsonArray(_items.Select(item => item.ToJsonNode()).ToArray());

	public IEnumerator<TFValue> GetEnumerator() => _items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class TFObject : TFValue<IReadOnlyDictionary<string, TFValue>>, IReadOnlyDictionary<string, TFValue>
{
	private readonly Dictionary<string, TFValue> _properties;

	public TFObject() : this(new Dictionary<string, TFValue>(StringComparer.Ordinal))
	{
	}

	public TFObject(IEnumerable<KeyValuePair<string, TFValue>> properties)
		: this(properties.ToDictionary(entry => entry.Key, entry => entry.Value.DeepClone(), StringComparer.Ordinal))
	{
	}

	private TFObject(Dictionary<string, TFValue> properties)
		: base(new ReadOnlyDictionary<string, TFValue>(properties))
	{
		_properties = properties;
	}

	public override TFValueKind Kind => TFValueKind.Object;

	public IEnumerable<string> Keys => _properties.Keys;

	public IEnumerable<TFValue> Values => _properties.Values;

	public int Count => _properties.Count;

	public TFValue this[string key]
	{
		get => _properties[key];
		set => _properties[key] = value?.DeepClone() ?? Null;
	}

	public void Add(string key, TFValue value) => _properties.Add(key, value.DeepClone());

	public bool Remove(string key) => _properties.Remove(key);

	public override TFValue DeepClone() => new TFObject(_properties);

	public override JsonNode ToJsonNode()
	{
		var json = new JsonObject();
		foreach (var (key, value) in _properties)
			json[key] = value.ToJsonNode();
		return json;
	}

	public bool ContainsKey(string key) => _properties.ContainsKey(key);

	public bool TryGetValue(string key, out TFValue value) => _properties.TryGetValue(key, out value!);

	public IEnumerator<KeyValuePair<string, TFValue>> GetEnumerator() => _properties.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal readonly record struct TFValueConversionOptions(bool LowercaseStrings)
{
	internal static TFValueConversionOptions Default => new(false);
}

internal sealed class TFValueJsonConverter : JsonConverter<TFValue>
{
	public override bool HandleNull => true;

	public override TFValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		return TFValue.FromJsonElement(document.RootElement.Clone());
	}

	public override void Write(Utf8JsonWriter writer, TFValue value, JsonSerializerOptions options)
	{
		var node = value.ToJsonNode();
		if (node is null)
		{
			writer.WriteNullValue();
			return;
		}

		node.WriteTo(writer, options);
	}
}
