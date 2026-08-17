namespace OcelotAdmin.Features.Gateways;

public sealed class GatewayConnectionResult
{
	public bool IsConnected { get; init; }

	public bool ConfigExists { get; init; }

	public bool ConfigReadable { get; init; }

	public bool ValidJson { get; init; }

	public bool ValidOcelotConfiguration { get; init; }

	public int RouteCount { get; init; }

	public string? Message { get; init; }
}