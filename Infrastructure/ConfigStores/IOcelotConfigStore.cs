using OcelotAdmin.Domain;

namespace OcelotAdmin.Infrastructure.ConfigStores;

public interface IOcelotConfigStore
{
	ConfigStoreType Type { get; }
	
	Task<string> ReadAsync(Gateway gateway,CancellationToken  cancellationToken =  default);
	Task PublishAsync(Gateway gateway,string configurationJson,CancellationToken  cancellationToken = default);
}