using OcelotAdmin.Domain;

namespace OcelotAdmin.Features.Gateways;

public sealed class CreateGatewayRequest
{
    public string Name { get; set; } =  string.Empty;
    public string? Description { get; set; }
    public ConfigStoreType ConfigStoreType { get; set; }
    public FileGatewayRequest? File { get; set; }
    public ConsulGatewayRequest? Consul { get; set; }
}

public sealed class FileGatewayRequest 
{
    public string ConfigurationPath { get; set; } = string.Empty;
}

public sealed class ConsulGatewayRequest
{
    public string Address { get; set; } = string.Empty;
    public string ConfigurationKey { get; set; } = string.Empty;
    public string? Token { get; set; }
}