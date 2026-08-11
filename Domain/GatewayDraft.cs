namespace OcelotAdmin.Domain;

public sealed class GatewayDraft
{
    public Guid Id { get; set; }
    public Guid GatewayId { get; set; }
    public string ConfigurationJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Gateway? Gateway { get; set; }
}
