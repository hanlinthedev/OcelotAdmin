using System.Text.Json;
using System.Text.Json.Serialization;

namespace OcelotAdmin.Domain.Ocelot;

public sealed class HttpHandlerOptions
{
	public bool? AllowAutoRedirect { get; set; }

	public bool? UseCookieContainer { get; set; }

	public bool? UseTracing { get; set; }

	public int? MaxConnectionsPerServer { get; set; }

	public int? Timeout { get; set; }

	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}