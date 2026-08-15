using System.Text.Json;
using System.Text.Json.Serialization;

namespace OcelotAdmin.Domain.Ocelot;

public sealed class LoadBalancerOptions
{
	public string? Type { get; set; }

	public string? Key { get; set; }

	public int? Expiry { get; set; }

	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}