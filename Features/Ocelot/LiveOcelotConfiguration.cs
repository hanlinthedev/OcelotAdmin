using OcelotAdmin.Domain.Ocelot;

namespace OcelotAdmin.Features.Ocelot;

public sealed class LiveOcelotConfiguration
{
	public required OcelotConfiguration Configuration { get; init; }

	public string? Version { get; init; }
	
	
}