using System.Text.Json;
using System.Text.Json.Serialization;

namespace OcelotAdmin.Domain.Ocelot;

public sealed class QoSOptions
{
	public int? ExceptionsAllowedBeforeBreaking { get; set; }

	public int? DurationOfBreak { get; set; }

	public int? TimeoutValue { get; set; }

	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}