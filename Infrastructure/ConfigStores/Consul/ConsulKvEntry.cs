namespace OcelotAdmin.Infrastructure.ConfigStores.Consul;

internal sealed class ConsulKvEntry
{
	public ulong CreateIndex { get; set; }

	public ulong ModifyIndex { get; set; }

	public ulong LockIndex { get; set; }

	public string Key { get; set; } = string.Empty;

	public ulong Flags { get; set; }

	public string? Value { get; set; }

	public string? Session { get; set; }
}