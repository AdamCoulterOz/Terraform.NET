using System.Collections.ObjectModel;

namespace TF;
public class ProviderCollection
{
	internal const string DefaultAlias = "default";

	private Dictionary<(string alias, string provider), Provider> ProviderDictionary { get; } = [];
	public ReadOnlyDictionary<(string alias, string provider), Provider> Aliases => new(ProviderDictionary);

	public void SetDefault(Provider provider) => SetAliasInternal(DefaultAlias, provider);

	/// <param name="alias">Value can't be "default", as it is reserved.</param>
	public void SetAlias(string alias, Provider provider)
	{
		if (alias == DefaultAlias)
			throw new ArgumentOutOfRangeException(nameof(alias), $"Can't use \"{DefaultAlias}\" as alias key because it is reserved.");
		SetAliasInternal(alias, provider);
	}

	internal Dictionary<string, string> CombinedProviderConfigs
		=> Aliases.Select(p => p.Value.GetEnvironmentConfig())
					  .SelectMany(c => c)
					  .ToDictionary(pair => pair.Key, pair => pair.Value);

	internal IEnumerable<ProviderBinding> Bindings
		=> Aliases.Select(pair => new ProviderBinding(
			pair.Key.provider,
			pair.Key.alias,
			pair.Value.GetTerraformConfig()));

	private void SetAliasInternal(string alias, Provider provider)
	{
		if (ProviderDictionary.ContainsKey((alias, provider.Name)))
			throw new Exception($"Alias '{alias}' already exists for '{provider.GetType().Name}' provider.");
		ProviderDictionary.Add((alias, provider.Name), provider);
	}
}

internal sealed record ProviderBinding(string ProviderName, string Alias, Dictionary<string, TFValue> Settings);
