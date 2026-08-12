using OcelotAdmin.Domain;

namespace OcelotAdmin.Infrastructure.ConfigStores;

public sealed class OcelotConfigStoreResolver
{
	private readonly IReadOnlyDictionary<ConfigStoreType, IOcelotConfigStore> _stores;

	public OcelotConfigStoreResolver(IEnumerable<IOcelotConfigStore> stores)
	{
		_stores = stores.ToDictionary(x=>x.Type, x=>x);
	}

	public IOcelotConfigStore Resolve(ConfigStoreType Type)
	{
		if (_stores.TryGetValue(Type, out var store))
		{
			return store;
		}
		throw new InvalidOperationException($"Could not find configuration store for type {Type}");
	}

}