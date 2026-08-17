namespace OcelotAdmin.Features.Ocelot.Validation;

public sealed class OcelotValidationIssue
{
	public ValidationSeverity Severity { get; init; }

	public int? RouteIndex { get; init; }

	public string? Field { get; init; }

	public string Message { get; init; } = string.Empty;
}