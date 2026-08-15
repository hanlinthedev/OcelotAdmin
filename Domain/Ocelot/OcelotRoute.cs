using System.Text.Json;
using System.Text.Json.Serialization;

namespace OcelotAdmin.Domain.Ocelot;

public sealed class OcelotRoute
{
	public string? UpstreamPathTemplate { get; set; }

	public List<string> UpstreamHttpMethod { get; set; } = [];

	public bool? RouteIsCaseSensitive { get; set; }

	public string? DownstreamScheme { get; set; }

	public string? ServiceName { get; set; }

	public string? DownstreamPathTemplate { get; set; }

	public List<string> DelegatingHandlers { get; set; } = [];

	public LoadBalancerOptions? LoadBalancerOptions { get; set; }

	public QoSOptions? QoSOptions { get; set; }

	public HttpHandlerOptions? HttpHandlerOptions { get; set; }

	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}