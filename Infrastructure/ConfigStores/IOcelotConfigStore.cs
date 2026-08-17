using OcelotAdmin.Domain;

namespace OcelotAdmin.Infrastructure.ConfigStores;

public interface IOcelotConfigStore
{
	ConfigStoreType Type { get; }

	Task<ConfigStoreReadResult> ReadAsync(
		Gateway gateway,
		CancellationToken cancellationToken = default);

	Task PublishAsync(
		Gateway gateway,
		string configurationJson,
		string? expectedVersion = null,
		CancellationToken cancellationToken = default);

	Task<ConfigStoreHealthResult> CheckHealthAsync(
		Gateway gateway,
		CancellationToken cancellationToken = default);
}