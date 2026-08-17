namespace OcelotAdmin.Infrastructure.ConfigStores;

public sealed class ConfigStoreReadResult
{
	public required string ConfigurationJson { get; init; }

	/// <summary>
	/// Provider-specific version used for optimistic concurrency.
	///
	/// Consul: ModifyIndex
	/// File: future file version/hash support
	/// </summary>
	public string? Version { get; init; }
}