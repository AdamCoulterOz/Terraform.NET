using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TF;

public enum TFTypeKind
{
	Any,
	String,
	Number,
	Boolean,
	List,
	Map,
	Set,
	Tuple,
	Object
}

[JsonConverter(typeof(TFTypeJsonConverter))]
public abstract record TFType
{
	public abstract TFTypeKind Kind { get; }

	public abstract TFValue ToValue();

	public abstract override string ToString();

	public static TFType Parse(string text)
		=> new Parser(text).Parse();

	public static TFType FromValue(TFValue value)
		=> value switch
		{
			TFString keyword => Parse(keyword.Value),
			TFArray sequence => FromSequence(sequence),
			_ => throw new InvalidOperationException($"Terraform type serialization must be a string or array, not '{value.Kind}'."),
		};

	private static TFType FromSequence(TFArray sequence)
	{
		if (sequence.Count != 2)
			throw new InvalidOperationException("Terraform complex type serialization must be a two-element array.");

		var constructor = sequence[0].GetValue<string>();
		return constructor switch
		{
			"list" => new TFListType(FromValue(sequence[1])),
			"map" => new TFMapType(FromValue(sequence[1])),
			"set" => new TFSetType(FromValue(sequence[1])),
			"tuple" => new TFTupleType(ParseTupleElements(sequence[1])),
			"object" => new TFObjectType(ParseObjectAttributes(sequence[1])),
			_ => throw new InvalidOperationException($"Unknown Terraform type constructor '{constructor}'."),
		};
	}

	private static IReadOnlyList<TFType> ParseTupleElements(TFValue value)
	{
		if (value is not TFArray elements)
			throw new InvalidOperationException("Terraform tuple type serialization must use an array of element types.");

		return new ReadOnlyCollection<TFType>(elements.Select(FromValue).ToList());
	}

	private static IReadOnlyDictionary<string, TFType> ParseObjectAttributes(TFValue value)
	{
		if (value is not TFObject attributes)
			throw new InvalidOperationException("Terraform object type serialization must use an object of attribute types.");

		return new ReadOnlyDictionary<string, TFType>(
			attributes.ToDictionary(attribute => attribute.Key, attribute => FromValue(attribute.Value), StringComparer.Ordinal));
	}

	private sealed class Parser(string text)
	{
		private readonly string _text = text;
		private int _index;

		internal TFType Parse()
		{
			var type = ParseType();
			SkipWhitespace();
			if (!IsAtEnd)
				throw new InvalidOperationException($"Unexpected trailing characters in Terraform type constraint '{_text}'.");
			return type;
		}

		private TFType ParseType()
		{
			SkipWhitespace();
			var identifier = ReadIdentifier();
			return identifier switch
			{
				"string" => TFStringType.Instance,
				"number" => TFNumberType.Instance,
				"bool" => TFBoolType.Instance,
				"any" => TFAnyType.Instance,
				"list" => ParseCollectionType("list"),
				"map" => ParseCollectionType("map"),
				"set" => ParseCollectionType("set"),
				"tuple" => ParseTupleType(),
				"object" => ParseObjectType(),
				_ => throw new InvalidOperationException($"Unknown Terraform type keyword '{identifier}'."),
			};
		}

		private TFType ParseCollectionType(string constructor)
		{
			SkipWhitespace();
			if (!TryConsume('('))
			{
				return constructor switch
				{
					"list" => new TFListType(TFAnyType.Instance),
					"map" => new TFMapType(TFAnyType.Instance),
					"set" => new TFSetType(TFAnyType.Instance),
					_ => throw new InvalidOperationException($"Terraform type constructor '{constructor}' requires parentheses."),
				};
			}

			var elementType = ParseType();
			SkipWhitespace();
			Expect(')');
			return constructor switch
			{
				"list" => new TFListType(elementType),
				"map" => new TFMapType(elementType),
				"set" => new TFSetType(elementType),
				_ => throw new InvalidOperationException($"Unknown Terraform collection type '{constructor}'."),
			};
		}

		private TFType ParseTupleType()
		{
			Expect('(');
			SkipWhitespace();
			Expect('[');
			var items = new List<TFType>();
			while (true)
			{
				SkipWhitespace();
				if (TryConsume(']'))
					break;

				items.Add(ParseType());
				SkipWhitespace();
				if (TryConsume(','))
					continue;

				Expect(']');
				break;
			}

			SkipWhitespace();
			Expect(')');
			return new TFTupleType(items);
		}

		private TFType ParseObjectType()
		{
			Expect('(');
			SkipWhitespace();
			Expect('{');
			var attributes = new Dictionary<string, TFType>(StringComparer.Ordinal);
			while (true)
			{
				SkipWhitespace();
				if (TryConsume('}'))
					break;

				var key = Peek() == '"' ? ReadQuotedString() : ReadIdentifier();
				SkipWhitespace();
				Expect('=');
				var attributeType = ParseType();
				attributes[key] = attributeType;

				SkipWhitespace();
				if (TryConsume(','))
					continue;

				Expect('}');
				break;
			}

			SkipWhitespace();
			Expect(')');
			return new TFObjectType(attributes);
		}

		private string ReadIdentifier()
		{
			SkipWhitespace();
			if (IsAtEnd || !(char.IsLetter(Peek()) || Peek() == '_'))
				throw new InvalidOperationException($"Expected Terraform type identifier at position {_index} in '{_text}'.");

			var start = _index;
			while (!IsAtEnd && (char.IsLetterOrDigit(Peek()) || Peek() == '_' || Peek() == '-'))
				_index++;

			return _text[start.._index];
		}

		private string ReadQuotedString()
		{
			Expect('"');
			var builder = new StringBuilder();
			while (!IsAtEnd)
			{
				var current = _text[_index++];
				if (current == '"')
					return builder.ToString();

				if (current == '\\')
				{
					if (IsAtEnd)
						throw new InvalidOperationException("Unterminated escape sequence in Terraform type string.");

					builder.Append(_text[_index++]);
					continue;
				}

				builder.Append(current);
			}

			throw new InvalidOperationException("Unterminated quoted string in Terraform type constraint.");
		}

		private void SkipWhitespace()
		{
			while (!IsAtEnd && char.IsWhiteSpace(Peek()))
				_index++;
		}

		private bool TryConsume(char value)
		{
			SkipWhitespace();
			if (IsAtEnd || Peek() != value)
				return false;

			_index++;
			return true;
		}

		private void Expect(char value)
		{
			SkipWhitespace();
			if (IsAtEnd || Peek() != value)
				throw new InvalidOperationException($"Expected '{value}' in Terraform type constraint '{_text}'.");

			_index++;
		}

		private bool IsAtEnd => _index >= _text.Length;

		private char Peek() => _text[_index];
	}
}

public sealed record TFAnyType : TFType
{
	public static TFAnyType Instance { get; } = new();

	private TFAnyType()
	{
	}

	public override TFTypeKind Kind => TFTypeKind.Any;

	public override TFValue ToValue() => new TFString("any");

	public override string ToString() => "any";
}

public sealed record TFStringType : TFType
{
	public static TFStringType Instance { get; } = new();

	private TFStringType()
	{
	}

	public override TFTypeKind Kind => TFTypeKind.String;

	public override TFValue ToValue() => new TFString("string");

	public override string ToString() => "string";
}

public sealed record TFNumberType : TFType
{
	public static TFNumberType Instance { get; } = new();

	private TFNumberType()
	{
	}

	public override TFTypeKind Kind => TFTypeKind.Number;

	public override TFValue ToValue() => new TFString("number");

	public override string ToString() => "number";
}

public sealed record TFBoolType : TFType
{
	public static TFBoolType Instance { get; } = new();

	private TFBoolType()
	{
	}

	public override TFTypeKind Kind => TFTypeKind.Boolean;

	public override TFValue ToValue() => new TFString("bool");

	public override string ToString() => "bool";
}

public sealed record TFListType(TFType ElementType) : TFType
{
	public override TFTypeKind Kind => TFTypeKind.List;

	public override TFValue ToValue() => new TFArray([new TFString("list"), ElementType.ToValue()]);

	public override string ToString() => $"list({ElementType})";
}

public sealed record TFMapType(TFType ElementType) : TFType
{
	public override TFTypeKind Kind => TFTypeKind.Map;

	public override TFValue ToValue() => new TFArray([new TFString("map"), ElementType.ToValue()]);

	public override string ToString() => $"map({ElementType})";
}

public sealed record TFSetType(TFType ElementType) : TFType
{
	public override TFTypeKind Kind => TFTypeKind.Set;

	public override TFValue ToValue() => new TFArray([new TFString("set"), ElementType.ToValue()]);

	public override string ToString() => $"set({ElementType})";
}

public sealed record TFTupleType : TFType
{
	public IReadOnlyList<TFType> ElementTypes { get; }

	public TFTupleType(IEnumerable<TFType> elementTypes)
		=> ElementTypes = new ReadOnlyCollection<TFType>(elementTypes.ToList());

	public override TFTypeKind Kind => TFTypeKind.Tuple;

	public override TFValue ToValue() => new TFArray(
		[
			new TFString("tuple"),
			new TFArray(ElementTypes.Select(elementType => elementType.ToValue()))
		]);

	public override string ToString() => $"tuple([{string.Join(", ", ElementTypes)}])";
}

public sealed record TFObjectType : TFType
{
	public IReadOnlyDictionary<string, TFType> Attributes { get; }

	public TFObjectType(IEnumerable<KeyValuePair<string, TFType>> attributes)
		=> Attributes = new ReadOnlyDictionary<string, TFType>(
			attributes.ToDictionary(attribute => attribute.Key, attribute => attribute.Value, StringComparer.Ordinal));

	public override TFTypeKind Kind => TFTypeKind.Object;

	public override TFValue ToValue()
	{
		var attributes = new TFObject();
		foreach (var (key, value) in Attributes)
			attributes[key] = value.ToValue();

		return new TFArray([new TFString("object"), attributes]);
	}

	public override string ToString()
		=> $"object({{{string.Join(", ", Attributes.Select(attribute => $"{attribute.Key} = {attribute.Value}"))}}})";
}

internal sealed class TFTypeJsonConverter : JsonConverter<TFType>
{
	public override TFType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = JsonSerializer.Deserialize<TFValue>(ref reader, options)
			?? throw new InvalidOperationException("Unable to deserialize Terraform type serialization.");
		return TFType.FromValue(value);
	}

	public override void Write(Utf8JsonWriter writer, TFType value, JsonSerializerOptions options)
	{
		var serialized = value.ToValue();
		JsonSerializer.Serialize(writer, serialized, options);
	}
}
