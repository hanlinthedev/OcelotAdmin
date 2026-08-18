using OcelotAdmin.Domain;

namespace OcelotAdmin.Features.Gateways;

public sealed class UpdateGatewayRequest
{
	public string Name { get; set; } = string.Empty;

	public string? Description { get; set; }

	public ConfigStoreType ConfigStoreType { get; set; }

	public FileGatewayRequest? File { get; set; }

	public ConsulGatewayRequest? Consul { get; set; }
}