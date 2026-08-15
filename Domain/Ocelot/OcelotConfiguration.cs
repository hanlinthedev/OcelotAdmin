using System.Text.Json;
using System.Text.Json.Serialization;

namespace OcelotAdmin.Domain.Ocelot;

public sealed class OcelotConfiguration
{
	public List<OcelotRoute> Routes { get; set; } = [];

	public OcelotGlobalConfiguration? GlobalConfiguration { get; set; }

	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}