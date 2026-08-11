namespace OcelotAdmin.Domain;

public sealed class Gateway
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ConfigStoreType  ConfigStoreType { get; set; } = ConfigStoreType.File;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public FileGatewaySettings? FileSettings { get; set; }
    public ConsulGatewaySettings?  ConsulSettings { get; set; }
    public GatewayDraft? Draft { get; set; }
    public ICollection<ConfigurationHistory> ConfigurationHistory { get; set; } = new List<ConfigurationHistory>();
}