using System.Text.Json;
using System.Text.Json.Nodes;

namespace TF;

internal static class ProviderConfigurationRewriter
{
	internal const string GeneratedFileName = "providers.auto.tf.json";
	private static readonly JsonSerializerOptions JsonWriteOptions = new() { WriteIndented = true };

	internal static void Rewrite(DirectoryInfo rootPath, ProviderCollection providers)
	{
		var extractedBlocks = new List<ProviderConfigBlock>();
		foreach (var file in EnumerateTerraformRootFiles(rootPath))
		{
			if (file.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
				ExtractProviderBlocksFromJson(file, extractedBlocks);
			else
				ExtractProviderBlocksFromHcl(file, extractedBlocks);
		}

		var mergedBlocks = MergeBlocks(extractedBlocks, providers.Bindings);
		WriteGeneratedProviderFile(rootPath, mergedBlocks);
	}

	private static IEnumerable<FileInfo> EnumerateTerraformRootFiles(DirectoryInfo rootPath)
		=> rootPath.EnumerateFiles()
			.Where(file =>
				file.Name.EndsWith(".tf", StringComparison.OrdinalIgnoreCase)
				|| file.Name.EndsWith(".tf.json", StringComparison.OrdinalIgnoreCase))
			.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase);

	private static Dictionary<(string provider, string alias), ProviderConfigBlock> MergeBlocks(
		IEnumerable<ProviderConfigBlock> extractedBlocks,
		IEnumerable<ProviderBinding> bindings)
	{
		var merged = new Dictionary<(string provider, string alias), ProviderConfigBlock>();
		foreach (var block in extractedBlocks)
		{
			var key = (block.ProviderName, block.Alias);
			if (!merged.TryAdd(key, block))
				throw new Exception($"Duplicate provider block found for provider '{block.ProviderName}' with alias '{DisplayAlias(block.Alias)}'.");
		}

		foreach (var binding in bindings)
		{
			var key = (binding.ProviderName, NormalizeAlias(binding.Alias));
			if (!merged.TryGetValue(key, out var block))
			{
				block = new ProviderConfigBlock(binding.ProviderName, key.Item2, new JsonObject());
				merged.Add(key, block);
			}

			foreach (var (name, value) in binding.Settings)
				block.Settings[name] = CloneNode(value);
		}

		return merged;
	}

	private static void WriteGeneratedProviderFile(DirectoryInfo rootPath, Dictionary<(string provider, string alias), ProviderConfigBlock> mergedBlocks)
	{
		var outputPath = Path.Join(rootPath.FullName, GeneratedFileName);
		if (mergedBlocks.Count == 0)
		{
			if (File.Exists(outputPath))
				File.Delete(outputPath);
			return;
		}

		var providerObject = new JsonObject();
		foreach (var providerGroup in mergedBlocks.Values
					 .OrderBy(block => block.ProviderName, StringComparer.Ordinal)
					 .ThenBy(block => block.Alias == ProviderCollection.DefaultAlias ? string.Empty : block.Alias, StringComparer.Ordinal)
					 .GroupBy(block => block.ProviderName, StringComparer.Ordinal))
		{
			var blocks = providerGroup.Select(ToJsonBlock).ToList();
			providerObject[providerGroup.Key] = blocks.Count == 1 ? blocks[0] : new JsonArray(blocks.ToArray());
		}

		var root = new JsonObject
		{
			["provider"] = providerObject
		};

		File.WriteAllText(outputPath, root.ToJsonString(JsonWriteOptions) + Environment.NewLine);
	}

	private static JsonObject ToJsonBlock(ProviderConfigBlock block)
	{
		var json = CloneObject(block.Settings);
		if (block.Alias != ProviderCollection.DefaultAlias)
			json["alias"] = block.Alias;
		return json;
	}

	private static void ExtractProviderBlocksFromJson(FileInfo file, ICollection<ProviderConfigBlock> extractedBlocks)
	{
		var content = File.ReadAllText(file.FullName);
		if (string.IsNullOrWhiteSpace(content))
			return;

		JsonNode? rootNode;
		try
		{
			rootNode = JsonNode.Parse(content);
		}
		catch (JsonException ex)
		{
			throw new Exception($"Failed to parse Terraform JSON file '{file.Name}'.", ex);
		}

		if (rootNode is not JsonObject rootObject)
			return;

		if (rootObject["provider"] is not JsonObject providersObject)
			return;

		foreach (var providerEntry in providersObject.ToList())
		{
			if (providerEntry.Value is null) continue;
			var providerName = providerEntry.Key;
			switch (providerEntry.Value)
			{
				case JsonObject providerBlock:
					extractedBlocks.Add(CreateBlockFromJson(providerName, providerBlock, file.Name));
					break;
				case JsonArray providerBlocks:
					foreach (var item in providerBlocks)
					{
						if (item is not JsonObject providerObject)
							throw new Exception($"Provider '{providerName}' in '{file.Name}' must be encoded as a JSON object.");
						extractedBlocks.Add(CreateBlockFromJson(providerName, providerObject, file.Name));
					}
					break;
				default:
					throw new Exception($"Provider '{providerName}' in '{file.Name}' must be encoded as a JSON object or array of objects.");
			}
		}

		rootObject.Remove("provider");
		WriteJsonRemainder(file, rootObject);
	}

	private static ProviderConfigBlock CreateBlockFromJson(string providerName, JsonObject providerBlock, string fileName)
	{
		var settings = CloneObject(providerBlock);
		var alias = ReadAndRemoveAlias(settings, providerName, fileName);
		return new ProviderConfigBlock(providerName, alias, settings);
	}

	private static void WriteJsonRemainder(FileInfo file, JsonObject rootObject)
	{
		if (rootObject.Count == 0)
		{
			File.Delete(file.FullName);
			return;
		}

		File.WriteAllText(file.FullName, rootObject.ToJsonString(JsonWriteOptions) + Environment.NewLine);
	}

	private static void ExtractProviderBlocksFromHcl(FileInfo file, ICollection<ProviderConfigBlock> extractedBlocks)
	{
		var content = File.ReadAllText(file.FullName);
		if (string.IsNullOrWhiteSpace(content))
			return;

		var matches = ProviderBlockScanner.Find(content);
		if (matches.Count == 0)
			return;

		foreach (var match in matches)
		{
			var settings = HclBodyParser.Parse(match.Body);
			var alias = ReadAndRemoveAlias(settings, match.ProviderName, file.Name);
			extractedBlocks.Add(new ProviderConfigBlock(match.ProviderName, alias, settings));
		}

		var updated = RemoveSpans(content, matches.Select(match => match.Span).OrderByDescending(span => span.Start));
		if (string.IsNullOrWhiteSpace(updated))
			File.Delete(file.FullName);
		else
			File.WriteAllText(file.FullName, updated.Trim() + Environment.NewLine);
	}

	private static string RemoveSpans(string content, IEnumerable<TextSpan> spans)
	{
		var updated = content;
		foreach (var span in spans)
			updated = updated.Remove(span.Start, span.Length);
		return updated;
	}

	private static string ReadAndRemoveAlias(JsonObject settings, string providerName, string fileName)
	{
		if (!settings.TryGetPropertyValue("alias", out var aliasNode) || aliasNode is null)
			return ProviderCollection.DefaultAlias;

		settings.Remove("alias");
		if (aliasNode is JsonValue aliasValue && aliasValue.TryGetValue<string>(out var alias))
			return NormalizeAlias(alias);

		throw new Exception($"Provider '{providerName}' in '{fileName}' must declare alias as a literal string.");
	}

	private static string NormalizeAlias(string? alias)
		=> string.IsNullOrWhiteSpace(alias) ? ProviderCollection.DefaultAlias : alias;

	private static string DisplayAlias(string alias)
		=> alias == ProviderCollection.DefaultAlias ? "<default>" : alias;

	private static JsonNode CloneNode(JsonNode node)
		=> JsonNode.Parse(node.ToJsonString())!;

	private static JsonObject CloneObject(JsonObject node)
		=> (JsonObject)JsonNode.Parse(node.ToJsonString())!;

	private sealed record ProviderConfigBlock(string ProviderName, string Alias, JsonObject Settings);
	private sealed record TextSpan(int Start, int Length);
	private sealed record ProviderBlockMatch(string ProviderName, string Body, TextSpan Span);

	private static class ProviderBlockScanner
	{
		internal static List<ProviderBlockMatch> Find(string content)
		{
			var matches = new List<ProviderBlockMatch>();
			var index = 0;
			var depth = 0;
			while (index < content.Length)
			{
				SkipWhitespaceAndComments(content, ref index, includeNewLines: true);
				if (index >= content.Length) break;

				var current = content[index];
				if (current == '{')
				{
					depth++;
					index++;
					continue;
				}

				if (current == '}')
				{
					depth = Math.Max(0, depth - 1);
					index++;
					continue;
				}

				if (depth == 0 && IsIdentifierStart(current))
				{
					var identifierStart = index;
					var identifier = ReadIdentifier(content, ref index);
					if (identifier == "provider" && TryReadProviderBlock(content, identifierStart, ref index, out var match))
					{
						matches.Add(match);
						continue;
					}

					continue;
				}

				index++;
			}

			return matches;
		}

		private static bool TryReadProviderBlock(string content, int blockStart, ref int index, out ProviderBlockMatch match)
		{
			match = null!;
			SkipWhitespaceAndComments(content, ref index, includeNewLines: true);
			if (!TryReadQuotedString(content, ref index, out var providerName))
				return false;

			SkipWhitespaceAndComments(content, ref index, includeNewLines: true);
			if (index >= content.Length || content[index] != '{')
				return false;

			var bodyStart = index + 1;
			var blockEnd = FindMatchingBrace(content, index);
			var body = content[bodyStart..blockEnd];
			match = new ProviderBlockMatch(providerName, body, new TextSpan(blockStart, blockEnd - blockStart + 1));
			index = blockEnd + 1;
			return true;
		}

		private static int FindMatchingBrace(string content, int openingBraceIndex)
		{
			var depth = 0;
			for (var index = openingBraceIndex; index < content.Length; index++)
			{
				if (StartsLineComment(content, index))
				{
					index = SkipToLineEnd(content, index + 2);
					continue;
				}

				if (StartsHashComment(content, index))
				{
					index = SkipToLineEnd(content, index + 1);
					continue;
				}

				if (StartsBlockComment(content, index))
				{
					index = SkipBlockComment(content, index + 2);
					continue;
				}

				if (content[index] == '"')
				{
					index = SkipQuotedString(content, index);
					continue;
				}

				if (content[index] == '{')
				{
					depth++;
					continue;
				}

				if (content[index] != '}') continue;
				depth--;
				if (depth == 0) return index;
			}

			throw new Exception("Unterminated provider block found while rewriting Terraform provider configuration.");
		}
	}

	private sealed class HclBodyParser
	{
		private readonly string _content;
		private int _index;

		private HclBodyParser(string content) => _content = content;

		internal static JsonObject Parse(string content) => new HclBodyParser(content).ParseBody();

		private JsonObject ParseBody()
		{
			var body = new JsonObject();
			while (true)
			{
				SkipWhitespaceAndComments(includeNewLines: true);
				if (IsAtEnd) return body;
				if (Peek() == '}')
				{
					_index++;
					return body;
				}

				var key = ReadIdentifierOrString();
				SkipWhitespaceAndComments(includeNewLines: true);
				if (!IsAtEnd && (Peek() == '=' || Peek() == ':'))
				{
					_index++;
					var value = ParseExpression();
					body[key] = value;
					SkipDelimiters();
					continue;
				}

				var labels = new List<string>();
				while (true)
				{
					SkipWhitespaceAndComments(includeNewLines: true);
					if (IsAtEnd)
						throw new Exception($"Unexpected end of HCL while parsing block '{key}'.");

					if (Peek() == '{') break;
					labels.Add(ReadIdentifierOrString());
				}

				Expect('{');
				var blockBody = ParseBody();
                AddBlock(body, key, labels, blockBody);
				SkipDelimiters();
			}
		}

		private JsonNode? ParseExpression()
		{
			SkipWhitespaceAndComments(includeNewLines: false);
			if (IsAtEnd)
				throw new Exception("Unexpected end of HCL while parsing provider expression.");

			return Peek() switch
			{
				'{' => ParseInlineObject(),
				'[' => ParseArray(),
				'"' => ParseStringNode(),
				_ => ParseScalarOrExpression()
			};
		}

		private JsonObject ParseInlineObject()
		{
			Expect('{');
			var json = new JsonObject();
			while (true)
			{
				SkipWhitespaceAndComments(includeNewLines: true);
				if (IsAtEnd)
					throw new Exception("Unexpected end of HCL while parsing object expression.");
				if (Peek() == '}')
				{
					_index++;
					return json;
				}

				var key = ReadIdentifierOrString();
				SkipWhitespaceAndComments(includeNewLines: true);
				if (IsAtEnd || (Peek() != '=' && Peek() != ':'))
					throw new Exception($"Expected '=' after object key '{key}'.");

				_index++;
				json[key] = ParseExpression();
				SkipDelimiters();
			}
		}

		private JsonArray ParseArray()
		{
			Expect('[');
			var array = new JsonArray();
			while (true)
			{
				SkipWhitespaceAndComments(includeNewLines: true);
				if (IsAtEnd)
					throw new Exception("Unexpected end of HCL while parsing array expression.");
				if (Peek() == ']')
				{
					_index++;
					return array;
				}

				array.Add(ParseExpression());
				SkipDelimiters();
			}
		}

		private JsonNode? ParseStringNode()
		{
			var start = _index;
			_index = SkipQuotedString(_content, _index) + 1;
			return JsonNode.Parse(_content[start.._index]);
		}

		private JsonValue? ParseScalarOrExpression()
		{
			var expression = ReadRawExpression();
			if (bool.TryParse(expression, out var booleanValue))
				return JsonValue.Create(booleanValue)!;

			if (expression == "null")
				return null;

			if (decimal.TryParse(expression, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var decimalValue))
				return JsonValue.Create(decimalValue)!;

			return JsonValue.Create("${" + expression + "}");
		}

		private string ReadRawExpression()
		{
			var start = _index;
			var braceDepth = 0;
			var bracketDepth = 0;
			var parenDepth = 0;
			while (!IsAtEnd)
			{
				if (StartsLineComment(_content, _index) || StartsHashComment(_content, _index))
					break;

				if (StartsBlockComment(_content, _index))
					break;

				var current = Peek();
				if (current == '"')
				{
					_index = SkipQuotedString(_content, _index) + 1;
					continue;
				}

				switch (current)
				{
					case '{':
						braceDepth++;
						break;
					case '}':
						if (braceDepth == 0 && bracketDepth == 0 && parenDepth == 0)
							return _content[start.._index].Trim();
						braceDepth--;
						break;
					case '[':
						bracketDepth++;
						break;
					case ']':
						if (braceDepth == 0 && bracketDepth == 0 && parenDepth == 0)
							return _content[start.._index].Trim();
						bracketDepth--;
						break;
					case '(':
						parenDepth++;
						break;
					case ')':
						parenDepth = Math.Max(0, parenDepth - 1);
						break;
					case ',':
						if (braceDepth == 0 && bracketDepth == 0 && parenDepth == 0)
							return _content[start.._index].Trim();
						break;
					case '\r':
					case '\n':
						if (braceDepth == 0 && bracketDepth == 0 && parenDepth == 0)
							return _content[start.._index].Trim();
						break;
				}

				_index++;
			}

			return _content[start.._index].Trim();
		}

		private void SkipDelimiters()
		{
			while (!IsAtEnd)
			{
				SkipWhitespaceAndComments(includeNewLines: true);
				if (!IsAtEnd && Peek() == ',')
				{
					_index++;
					continue;
				}

				if (!IsAtEnd && (Peek() == '\r' || Peek() == '\n'))
				{
					_index++;
					continue;
				}

				return;
			}
		}

		private static void AddBlock(JsonObject body, string key, List<string> labels, JsonObject blockBody)
		{
			var node = blockBody;
			for (var labelIndex = labels.Count - 1; labelIndex >= 0; labelIndex--)
				node = new JsonObject { [labels[labelIndex]] = node };

			if (!body.TryGetPropertyValue(key, out var existing) || existing is null)
			{
				body[key] = node;
				return;
			}

			if (existing is JsonArray array)
			{
				array.Add(node);
				return;
			}

			body[key] = new JsonArray(existing, node);
		}

		private string ReadIdentifierOrString()
		{
			SkipWhitespaceAndComments(includeNewLines: true);
			if (IsAtEnd)
				throw new Exception("Unexpected end of HCL while reading identifier.");

			if (Peek() == '"')
			{
				if (!TryReadQuotedString(_content, ref _index, out var value))
					throw new Exception("Failed to read quoted HCL string.");
				return value;
			}

			return ReadIdentifier(_content, ref _index);
		}

		private void SkipWhitespaceAndComments(bool includeNewLines)
			=> ProviderConfigurationRewriter.SkipWhitespaceAndComments(_content, ref _index, includeNewLines);

		private void Expect(char expected)
		{
			SkipWhitespaceAndComments(includeNewLines: true);
			if (IsAtEnd || Peek() != expected)
				throw new Exception($"Expected '{expected}' while parsing HCL.");
			_index++;
		}

		private bool IsAtEnd => _index >= _content.Length;
		private char Peek() => _content[_index];
	}

	private static void SkipWhitespaceAndComments(string content, ref int index, bool includeNewLines)
	{
		while (index < content.Length)
		{
			var current = content[index];
			if (char.IsWhiteSpace(current))
			{
				if (!includeNewLines && (current == '\r' || current == '\n'))
					return;
				index++;
				continue;
			}

			if (StartsLineComment(content, index))
			{
				index = SkipToLineEnd(content, index + 2);
				continue;
			}

			if (StartsHashComment(content, index))
			{
				index = SkipToLineEnd(content, index + 1);
				continue;
			}

			if (StartsBlockComment(content, index))
			{
				index = SkipBlockComment(content, index + 2) + 1;
				continue;
			}

			break;
		}
	}

	private static bool IsIdentifierStart(char value)
		=> char.IsLetter(value) || value == '_';

	private static string ReadIdentifier(string content, ref int index)
	{
		if (index >= content.Length || !IsIdentifierStart(content[index]))
			throw new Exception("Expected HCL identifier.");

		var start = index;
		index++;
		while (index < content.Length && (char.IsLetterOrDigit(content[index]) || content[index] is '_' or '-'))
			index++;
		return content[start..index];
	}

	private static bool TryReadQuotedString(string content, ref int index, out string value)
	{
		value = string.Empty;
		if (index >= content.Length || content[index] != '"')
			return false;

		var start = index;
		index = SkipQuotedString(content, index) + 1;
		value = JsonSerializer.Deserialize<string>(content[start..index])!;
		return true;
	}

	private static int SkipQuotedString(string content, int openingQuoteIndex)
	{
		for (var index = openingQuoteIndex + 1; index < content.Length; index++)
		{
			if (content[index] == '\\')
			{
				index++;
				continue;
			}

			if (content[index] == '"')
				return index;
		}

		throw new Exception("Unterminated quoted string found while parsing HCL.");
	}

	private static bool StartsLineComment(string content, int index)
		=> index + 1 < content.Length && content[index] == '/' && content[index + 1] == '/';

	private static bool StartsHashComment(string content, int index)
		=> content[index] == '#';

	private static bool StartsBlockComment(string content, int index)
		=> index + 1 < content.Length && content[index] == '/' && content[index + 1] == '*';

	private static int SkipToLineEnd(string content, int index)
	{
		while (index < content.Length && content[index] is not '\r' and not '\n')
			index++;
		return index;
	}

	private static int SkipBlockComment(string content, int index)
	{
		while (index + 1 < content.Length)
		{
			if (content[index] == '*' && content[index + 1] == '/')
				return index + 1;
			index++;
		}

		throw new Exception("Unterminated block comment found while parsing HCL.");
	}
}
