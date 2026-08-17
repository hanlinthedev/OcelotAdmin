namespace OcelotAdmin.Infrastructure.ConfigStores;

public sealed class ConfigStoreHealthResult
{
	public bool IsReachable { get; init; }

	public bool KeyExists { get; init; }

	public bool IsReadable { get; init; }

	public bool ConfigurationIsValid { get; init; }

	public string? Message { get; init; }

	public bool IsHealthy =>
		IsReachable &&
		KeyExists &&
		IsReadable &&
		ConfigurationIsValid;
}