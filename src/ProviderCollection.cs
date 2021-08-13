using System.Collections.ObjectModel;

namespace TF;
public class ProviderCollection
{
	private const string _defaultAlias = "default";

	private Dictionary<(string alias, string provider), Provider> ProviderDictionary { get; } = new();
	public ReadOnlyDictionary<(string alias, string provider), Provider> Aliases => new(ProviderDictionary);

	public void SetDefault(Provider provider) => SetAliasInternal(_defaultAlias, provider);

	/// <param name="alias">Can't be 'default', as it is reserved.</param>
	public void SetAlias(string alias, Provider provider)
	{
		if (alias == _defaultAlias)
			throw new Exception($"Can't use '{_defaultAlias}' alias key as it is reserved.");
		SetAliasInternal(alias, provider);
	}

	internal Dictionary<string, string> CombinedProviderConfigs
		=> Aliases.Select(p => p.Value.GetConfig())
				  .SelectMany(c => c)
				  .ToDictionary(pair => pair.Key, pair => pair.Value);

	private void SetAliasInternal(string alias, Provider provider)
	{
		if (ProviderDictionary.ContainsKey((alias, provider.Name)))
			throw new Exception($"Alias '{alias}' already exists for '{provider.GetType().Name}' provider.");
		ProviderDictionary.Add((alias, provider.Name), provider);
	}
}
