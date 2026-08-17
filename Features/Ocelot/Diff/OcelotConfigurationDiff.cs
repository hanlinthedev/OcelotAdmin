namespace OcelotAdmin.Features.Ocelot.Diff;

public sealed class OcelotConfigurationDiff
{
	public int AddedRoutes { get; init; }

	public int RemovedRoutes { get; init; }

	public int ModifiedRoutes { get; init; }

	public bool GlobalConfigurationChanged { get; init; }

	public bool HasChanges =>
		AddedRoutes > 0 ||
		RemovedRoutes > 0 ||
		ModifiedRoutes > 0 ||
		GlobalConfigurationChanged;
}