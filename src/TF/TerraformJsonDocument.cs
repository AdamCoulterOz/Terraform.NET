using System.Text.Json;

namespace TF;

public sealed class TerraformJsonDocument
{
	public TerraformBlockSet Data { get; } = new();

	public TerraformBlockSet Resources { get; } = new();

	public TerraformNamedBlockSet Modules { get; } = new();

	public TerraformNamedBlockSet Outputs { get; } = new();

	public Dictionary<string, object?> ToPayload()
	{
		var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
		if (!Data.IsEmpty)
			payload["data"] = Data.ToPayload();
		if (!Resources.IsEmpty)
			payload["resource"] = Resources.ToPayload();
		if (!Modules.IsEmpty)
			payload["module"] = Modules.ToPayload();
		if (!Outputs.IsEmpty)
			payload["output"] = Outputs.ToPayload();

		return payload;
	}

	public string ToJsonString(JsonSerializerOptions? options = null)
		=> JsonSerializer.Serialize(ToPayload(), options);

	public void WriteTo(string path, JsonSerializerOptions? options = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		File.WriteAllText(path, ToJsonString(options));
	}
}

public sealed class TerraformBlockSet
{
	private readonly Dictionary<string, Dictionary<string, TFValue>> _blocks = new(StringComparer.Ordinal);

	public bool IsEmpty => _blocks.Count == 0;

	public void Add(string type, string name, object? payload)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(type);
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		var blocks = _blocks.GetValueOrDefault(type);
		if (blocks is null)
		{
			blocks = new Dictionary<string, TFValue>(StringComparer.Ordinal);
			_blocks.Add(type, blocks);
		}

		if (!blocks.TryAdd(name, TFValue.FromObject(payload)))
			throw new InvalidOperationException($"Terraform block '{type}.{name}' is already defined.");
	}

	public IReadOnlyDictionary<string, Dictionary<string, TFValue>> ToPayload() => _blocks;
}

public sealed class TerraformNamedBlockSet
{
	private readonly Dictionary<string, TFValue> _blocks = new(StringComparer.Ordinal);

	public bool IsEmpty => _blocks.Count == 0;

	public void Add(string name, object? payload)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		if (!_blocks.TryAdd(name, TFValue.FromObject(payload)))
			throw new InvalidOperationException($"Terraform block '{name}' is already defined.");
	}

	public IReadOnlyDictionary<string, TFValue> ToPayload() => _blocks;
}
