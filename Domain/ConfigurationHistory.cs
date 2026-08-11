namespace OcelotAdmin.Domain;

public sealed class ConfigurationHistory
{
    public Guid Id { get; set; }
    public Guid GatewayId { get; set; }
    public string ConfigurationJson { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public Gateway Gateway { get; set; } = null!;
    
}