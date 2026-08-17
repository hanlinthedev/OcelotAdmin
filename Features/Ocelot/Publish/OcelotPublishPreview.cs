using OcelotAdmin.Features.Ocelot.Diff;
using OcelotAdmin.Features.Ocelot.Validation;

namespace OcelotAdmin.Features.Ocelot.Publish;

public sealed class OcelotPublishPreview
{
	public OcelotValidationResult Validation { get; init; }
		= new();

	public OcelotConfigurationDiff Diff { get; init; }
		= new();

	public bool HasConcurrencyConflict { get; init; }

	public string? DraftSourceVersion { get; init; }

	public string? CurrentVersion { get; init; }

	public bool CanPublish =>
		Validation.IsValid &&
		Diff.HasChanges &&
		!HasConcurrencyConflict;
}