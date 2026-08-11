namespace OcelotAdmin.Domain;

public sealed class FileGatewaySettings
{
    public Guid GatewayId { get; set; }
    public string ConfigurationPath { get; set; }= string.Empty;
    public Gateway Gateway { get; set; } = null!;
}