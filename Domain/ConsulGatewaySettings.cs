namespace OcelotAdmin.Domain;

public sealed class ConsulGatewaySettings
{
    public Guid GatewayId { get; set; }
    public string Address {get; set;} = string.Empty;
    public string ConfigurationKey { get; set; } = string.Empty;
    public string? Token { get; set; }
    public Gateway Gateway { get; set; } = null!;
}