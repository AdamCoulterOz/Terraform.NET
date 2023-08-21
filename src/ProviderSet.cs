[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TF.Tests.Unit")]
namespace TF
{
	public class ProviderSet
	{
		private record ProviderInstance(string Provider, string Alias)
		{
			internal string Prefix => string.IsNullOrEmpty(Alias) ? Provider : $"{Provider}.{Alias}";
		};
		private readonly Dictionary<ProviderInstance, IProvider> _providers = new();

		public bool TryGet(out IProvider? instance, string provider, string alias = "")
			=> _providers.TryGetValue(new ProviderInstance(provider, alias), out instance);

		public void Add(IProvider provider, string alias = "")
		{
			if (!_providers.TryAdd(new(provider.Name, alias), provider))
				throw new ArgumentException($"Alias '{alias}' already exists for '{provider.GetType().Name}' provider.", nameof(alias));
		}

		internal Dictionary<string, string?> CombinedProviderConfigs
			=> _providers.SelectMany(p => p.Value.GetConfig()
							.ToDictionary(c => $"{p.Key.Prefix}.{c.Key}", c => c.Value))
						 .ToDictionary(pair => pair.Key, pair => (string?)pair.Value);
	}
}