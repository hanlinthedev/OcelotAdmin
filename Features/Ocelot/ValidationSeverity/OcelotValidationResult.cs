namespace OcelotAdmin.Features.Ocelot.Validation;

public sealed class OcelotValidationResult
{
	public List<OcelotValidationIssue> Issues { get; init; } = [];

	public bool IsValid =>
		Issues.All(x =>
			x.Severity != ValidationSeverity.Error);

	public int ErrorCount =>
		Issues.Count(x =>
			x.Severity == ValidationSeverity.Error);

	public int WarningCount =>
		Issues.Count(x =>
			x.Severity == ValidationSeverity.Warning);
}