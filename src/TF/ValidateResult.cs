using System.Text.Json;

namespace TF;

public sealed class ValidateResult : CommandJsonResult
{
	public string FormatVersion { get; set; } = string.Empty;
	public bool Valid { get; set; }
	public int ErrorCount { get; set; }
	public int WarningCount { get; set; }
	public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = [];

	protected override void LoadJson(string? output, JsonSerializerOptions options)
	{
		var payload = DeserializeJson<ValidateResultPayload>(output, options);
		FormatVersion = payload.FormatVersion;
		Valid = payload.Valid;
		ErrorCount = payload.ErrorCount;
		WarningCount = payload.WarningCount;
		Diagnostics = payload.Diagnostics.Select(diagnostic => new Diagnostic
		{
			Severity = (diagnostic.Severity ?? string.Empty).ToLowerInvariant() switch
			{
				"info" => DiagnosticSeverity.Info,
				"warn" or "warning" => DiagnosticSeverity.Warning,
				"error" => DiagnosticSeverity.Error,
				_ => throw new InvalidOperationException($"Unknown Terraform validate diagnostic severity '{diagnostic.Severity}'."),
			},
			Summary = diagnostic.Summary ?? "Terraform validate diagnostic",
			Detail = diagnostic.Detail,
		}).ToList();
	}

	private sealed class ValidateResultPayload
	{
		public required string FormatVersion { get; set; }
		public bool Valid { get; set; }
		public int ErrorCount { get; set; }
		public int WarningCount { get; set; }
		public List<TerraformValidateDiagnosticPayload> Diagnostics { get; set; } = [];
	}

	private sealed class TerraformValidateDiagnosticPayload
	{
		public string? Severity { get; set; }
		public string? Summary { get; set; }
		public string? Detail { get; set; }
	}
}
