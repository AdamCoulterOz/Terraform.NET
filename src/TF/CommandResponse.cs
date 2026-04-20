using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TF;

public sealed class CommandResponse
{
	public string? TerraformVersion { get; init; }
	public string? UiVersion { get; init; }
	public TerraformInitStatus Init { get; init; } = new();
	public ChangeSummary? ChangeSummary { get; init; }
	public IReadOnlySet<string> MessageCodes { get; init; } = new HashSet<string>(StringComparer.Ordinal);
	public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];
	public IReadOnlyList<string> Messages { get; init; } = [];
	public IReadOnlyList<string> NonJsonLines { get; init; } = [];
	public IReadOnlyList<ProviderInstall> ProviderInstalls { get; init; } = [];
	public IReadOnlyList<ResourceChangeInfo> ResourceDrifts { get; init; } = [];
	public IReadOnlyList<ResourceChangeInfo> PlannedChanges { get; init; } = [];
	public IReadOnlyDictionary<string, OutputValue> Outputs { get; init; } = new Dictionary<string, OutputValue>(StringComparer.Ordinal);
	public IReadOnlyList<ResourceOperation> ResourceOperations { get; init; } = [];
	public IReadOnlyList<ProvisionOperation> ProvisionOperations { get; init; } = [];
	public IReadOnlyList<RefreshOperation> RefreshOperations { get; init; } = [];
	public IReadOnlyList<EphemeralOperation> EphemeralOperations { get; init; } = [];
}

public sealed class TerraformInitStatus
{
	public bool TerraformCloudInitializing { get; internal set; }
	public bool BackendInitializing { get; internal set; }
	public bool ProviderPluginsInitializing { get; internal set; }
	public bool ModulesInitializing { get; internal set; }
	public bool CopyingConfiguration { get; internal set; }
	public bool ModulesUpgrading { get; internal set; }
	public bool EmptyMessageEmitted { get; internal set; }
	public bool EmptyDirectoryInitialized { get; internal set; }
	public bool Initialized { get; internal set; }
	public bool CloudInitialized { get; internal set; }
	public bool HasCliInstructions { get; internal set; }
	public bool HasCloudCliInstructions { get; internal set; }
	public bool LockFileCreated { get; internal set; }
	public bool LockFileChanged { get; internal set; }
}

public sealed class Diagnostic
{
	public required DiagnosticSeverity Severity { get; init; }
	public required string Summary { get; init; }
	public string? Detail { get; init; }
}

public sealed class ChangeSummary
{
	public int Add { get; init; }
	public int Change { get; init; }
	public int Remove { get; init; }
	public int Import { get; init; }
	public int Forget { get; init; }
	public CommandOperation? Operation { get; init; }

	public bool HasChanges => Add > 0 || Change > 0 || Remove > 0 || Import > 0 || Forget > 0;
}

public sealed class ProviderInstall
{
	public required string Source { get; init; }
	public string? VersionConstraint { get; internal set; }
	public string? Version { get; internal set; }
	public bool FoundLatestVersion { get; internal set; }
	public bool ReusedFromLockFile { get; internal set; }
	public bool AlreadyInstalled { get; internal set; }
	public bool BuiltIn { get; internal set; }
	public bool Installing { get; internal set; }
	public bool Installed { get; internal set; }
	public string? SigningKey { get; internal set; }
}

public sealed class ResourceReference
{
	public required string Address { get; init; }
	public string Module { get; init; } = string.Empty;
	public required string Resource { get; init; }
	public required string ResourceType { get; init; }
	public required string ResourceName { get; init; }
	public string? ImpliedProvider { get; init; }
	public TFValue? ResourceKey { get; init; }
}

public sealed class ResourceChangeInfo
{
	public required ResourceReference Resource { get; init; }
	public ResourceReference? PreviousResource { get; init; }
	public required ResourceAction Action { get; init; }
	public string? Reason { get; init; }
}

public sealed class OutputValue
{
	public ResourceAction? Action { get; init; }
	public TFValue? Value { get; init; }
	public TFType? Type { get; init; }
	public bool Sensitive { get; init; }
}

public sealed class ResourceOperation
{
	public required ResourceOperationType Type { get; init; }
	public required ResourceReference Resource { get; init; }
	public ResourceAction? Action { get; init; }
	public string? IdKey { get; init; }
	public string? IdValue { get; init; }
	public TimeSpan? Elapsed { get; init; }
}

public sealed class ProvisionOperation
{
	public required ProvisionOperationType Type { get; init; }
	public required ResourceReference Resource { get; init; }
	public string? Provisioner { get; init; }
	public string? Output { get; init; }
}

public sealed class RefreshOperation
{
	public required RefreshOperationType Type { get; init; }
	public required ResourceReference Resource { get; init; }
	public string? IdKey { get; init; }
	public string? IdValue { get; init; }
}

public sealed class EphemeralOperation
{
	public required EphemeralOperationType Type { get; init; }
	public required ResourceReference Resource { get; init; }
	public ResourceAction? Action { get; init; }
	public TimeSpan? Elapsed { get; init; }
}

public enum DiagnosticSeverity
{
	Info,
	Warning,
	Error
}

public enum CommandOperation
{
	Plan,
	Apply,
	Destroy,
	Refresh
}

public enum ResourceAction
{
	NoOp,
	Create,
	Read,
	Update,
	Delete,
	Forget,
	Import,
	Move
}

public enum ResourceOperationType
{
	ApplyStart,
	ApplyProgress,
	ApplyComplete,
	ApplyErrored
}

public enum ProvisionOperationType
{
	ProvisionStart,
	ProvisionProgress,
	ProvisionComplete,
	ProvisionErrored
}

public enum RefreshOperationType
{
	RefreshStart,
	RefreshComplete
}

public enum EphemeralOperationType
{
	EphemeralOperationStart,
	EphemeralOperationProgress,
	EphemeralOperationComplete,
	EphemeralOperationErrored
}

internal static partial class CommandResponseParser
{
	private const string DiagnosticType = "diagnostic";
	private const string VersionType = "version";
	private const string ChangeSummaryType = "change_summary";
	private const string ResourceDriftType = "resource_drift";
	private const string PlannedChangeType = "planned_change";
	private const string OutputsType = "outputs";
	private const string ApplyStartType = "apply_start";
	private const string ApplyProgressType = "apply_progress";
	private const string ApplyCompleteType = "apply_complete";
	private const string ApplyErroredType = "apply_errored";
	private const string ProvisionStartType = "provision_start";
	private const string ProvisionProgressType = "provision_progress";
	private const string ProvisionCompleteType = "provision_complete";
	private const string ProvisionErroredType = "provision_errored";
	private const string RefreshStartType = "refresh_start";
	private const string RefreshCompleteType = "refresh_complete";
	private const string EphemeralStartType = "ephemeral_op_start";
	private const string EphemeralProgressType = "ephemeral_op_progress";
	private const string EphemeralCompleteType = "ephemeral_op_complete";
	private const string EphemeralErroredType = "ephemeral_op_errored";

	private const string InitializingTerraformCloudMessageCode = "initializing_terraform_cloud_message";
	private const string InitializingBackendMessageCode = "initializing_backend_message";
	private const string EmptyMessageCode = "empty_message";
	private const string OutputInitEmptyMessageCode = "output_init_empty_message";
	private const string OutputInitSuccessMessageCode = "output_init_success_message";
	private const string OutputInitSuccessCloudMessageCode = "output_init_success_cloud_message";
	private const string OutputInitSuccessCliMessageCode = "output_init_success_cli_message";
	private const string OutputInitSuccessCliCloudMessageCode = "output_init_success_cli_cloud_message";
	private const string InitializingProviderPluginMessageCode = "initializing_provider_plugin_message";
	private const string ProviderAlreadyInstalledMessageCode = "provider_already_installed_message";
	private const string BuiltInProviderAvailableMessageCode = "built_in_provider_available_message";
	private const string InstallingProviderMessageCode = "installing_provider_message";
	private const string LockInfoMessageCode = "lock_info";
	private const string DependenciesLockChangesInfoMessageCode = "dependencies_lock_changes_info";
	private const string CopyingConfigurationMessageCode = "copying_configuration_message";
	private const string UpgradingModulesMessageCode = "upgrading_modules_message";
	private const string InitializingModulesMessageCode = "initializing_modules_message";

	public static CommandResponse Parse(string? output, JsonSerializerOptions options)
	{
		if (string.IsNullOrWhiteSpace(output))
			return new CommandResponse();

		string? terraformVersion = null;
		string? uiVersion = null;
		var init = new TerraformInitStatus();
		ChangeSummary? changeSummary = null;
		var messageCodes = new HashSet<string>(StringComparer.Ordinal);
		var diagnostics = new List<Diagnostic>();
		var messages = new List<string>();
		var nonJsonLines = new List<string>();
		var providerInstalls = new Dictionary<string, ProviderInstall>(StringComparer.OrdinalIgnoreCase);
		var resourceDrifts = new List<ResourceChangeInfo>();
		var plannedChanges = new List<ResourceChangeInfo>();
		var outputs = new Dictionary<string, OutputValue>(StringComparer.Ordinal);
		var resourceOperations = new List<ResourceOperation>();
		var provisionOperations = new List<ProvisionOperation>();
		var refreshOperations = new List<RefreshOperation>();
		var ephemeralOperations = new List<EphemeralOperation>();

		var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (var line in lines)
		{
			var trimmed = line.Trim();
			if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
			{
				nonJsonLines.Add(trimmed);
				messages.Add(trimmed);
				continue;
			}

			var envelope = JsonSerializer.Deserialize<TerraformUiEnvelope>(trimmed, options)
				?? throw new InvalidOperationException("Unable to deserialize Terraform UI output line.");

			if (!string.IsNullOrWhiteSpace(envelope.Message))
				messages.Add(envelope.Message);

			if (!string.IsNullOrWhiteSpace(envelope.MessageCode))
			{
				messageCodes.Add(envelope.MessageCode);
				UpdateInitStatus(init, envelope.MessageCode);
			}

			if (envelope.Type == VersionType)
			{
				terraformVersion = envelope.TerraformVersion ?? terraformVersion;
				uiVersion = envelope.UiVersion ?? uiVersion;
			}

			if (envelope.Type == ChangeSummaryType && envelope.Changes is not null)
				changeSummary = ToChangeSummary(envelope.Changes);

			if (envelope.Type == DiagnosticType && envelope.Diagnostic is not null)
				diagnostics.Add(ToDiagnostic(envelope.Diagnostic, envelope));

			if (envelope.Type == ResourceDriftType && envelope.Change is not null)
				resourceDrifts.Add(ToResourceChange(envelope.Change));

			if (envelope.Type == PlannedChangeType && envelope.Change is not null)
				plannedChanges.Add(ToResourceChange(envelope.Change));

			if (envelope.Type == OutputsType && envelope.Outputs is not null)
			{
				foreach (var outputEntry in envelope.Outputs)
					outputs[outputEntry.Key] = ToOutputValue(outputEntry.Value);
			}

			switch (envelope.Type)
			{
				case ApplyStartType:
				case ApplyProgressType:
				case ApplyCompleteType:
				case ApplyErroredType:
					if (envelope.Hook?.Resource is not null)
						resourceOperations.Add(ToResourceOperation(envelope.Type, envelope.Hook));
					break;
				case ProvisionStartType:
				case ProvisionProgressType:
				case ProvisionCompleteType:
				case ProvisionErroredType:
					if (envelope.Hook?.Resource is not null)
						provisionOperations.Add(ToProvisionOperation(envelope.Type, envelope.Hook));
					break;
				case RefreshStartType:
				case RefreshCompleteType:
					if (envelope.Hook?.Resource is not null)
						refreshOperations.Add(ToRefreshOperation(envelope.Type, envelope.Hook));
					break;
				case EphemeralStartType:
				case EphemeralProgressType:
				case EphemeralCompleteType:
				case EphemeralErroredType:
					if (envelope.Hook?.Resource is not null)
						ephemeralOperations.Add(ToEphemeralOperation(envelope.Type, envelope.Hook));
					break;
			}

			UpdateProviderInstalls(providerInstalls, envelope);
		}

		return new CommandResponse
		{
			TerraformVersion = terraformVersion,
			UiVersion = uiVersion,
			Init = init,
			ChangeSummary = changeSummary,
			MessageCodes = messageCodes,
			Diagnostics = diagnostics,
			Messages = messages,
			NonJsonLines = nonJsonLines,
			ProviderInstalls = providerInstalls.Values.ToList(),
			ResourceDrifts = resourceDrifts,
			PlannedChanges = plannedChanges,
			Outputs = outputs,
			ResourceOperations = resourceOperations,
			ProvisionOperations = provisionOperations,
			RefreshOperations = refreshOperations,
			EphemeralOperations = ephemeralOperations,
		};
	}

	private static void UpdateInitStatus(TerraformInitStatus init, string messageCode)
	{
		switch (messageCode)
		{
			case InitializingTerraformCloudMessageCode:
				init.TerraformCloudInitializing = true;
				break;
			case InitializingBackendMessageCode:
				init.BackendInitializing = true;
				break;
			case EmptyMessageCode:
				init.EmptyMessageEmitted = true;
				break;
			case OutputInitEmptyMessageCode:
				init.EmptyDirectoryInitialized = true;
				break;
			case OutputInitSuccessMessageCode:
				init.Initialized = true;
				break;
			case OutputInitSuccessCloudMessageCode:
				init.CloudInitialized = true;
				break;
			case OutputInitSuccessCliMessageCode:
				init.HasCliInstructions = true;
				break;
			case OutputInitSuccessCliCloudMessageCode:
				init.HasCloudCliInstructions = true;
				break;
			case InitializingProviderPluginMessageCode:
				init.ProviderPluginsInitializing = true;
				break;
			case LockInfoMessageCode:
				init.LockFileCreated = true;
				break;
			case DependenciesLockChangesInfoMessageCode:
				init.LockFileChanged = true;
				break;
			case CopyingConfigurationMessageCode:
				init.CopyingConfiguration = true;
				break;
			case UpgradingModulesMessageCode:
				init.ModulesUpgrading = true;
				break;
			case InitializingModulesMessageCode:
				init.ModulesInitializing = true;
				break;
		}
	}

	private static void UpdateProviderInstalls(Dictionary<string, ProviderInstall> providerInstalls, TerraformUiEnvelope envelope)
	{
		if (!string.IsNullOrWhiteSpace(envelope.MessageCode))
		{
			if (envelope.MessageCode == ProviderAlreadyInstalledMessageCode)
				MarkProviderFromMessage(providerInstalls, envelope.Message, install => install.AlreadyInstalled = true);

			if (envelope.MessageCode == BuiltInProviderAvailableMessageCode)
				MarkProviderFromMessage(providerInstalls, envelope.Message, install => install.BuiltIn = true);

			if (envelope.MessageCode == InstallingProviderMessageCode)
				MarkProviderFromMessage(providerInstalls, envelope.Message, install => install.Installing = true);
		}

		if (string.IsNullOrWhiteSpace(envelope.Message))
			return;

		var findingMatch = FindingMatchingVersionsRegex().Match(envelope.Message);
		if (findingMatch.Success)
		{
			var install = GetOrCreateInstall(providerInstalls, findingMatch.Groups["source"].Value);
			install.FoundLatestVersion = true;
			install.VersionConstraint = findingMatch.Groups["constraint"].Success ? findingMatch.Groups["constraint"].Value : install.VersionConstraint;
			return;
		}

		var findingLatestMatch = FindingLatestVersionRegex().Match(envelope.Message);
		if (findingLatestMatch.Success)
		{
			GetOrCreateInstall(providerInstalls, findingLatestMatch.Groups["source"].Value).FoundLatestVersion = true;
			return;
		}

		var reusedMatch = ReusingLockedVersionRegex().Match(envelope.Message);
		if (reusedMatch.Success)
		{
			var install = GetOrCreateInstall(providerInstalls, reusedMatch.Groups["source"].Value);
			install.ReusedFromLockFile = true;
			install.AlreadyInstalled = true;
			return;
		}

		var installingMatch = InstallingProviderRegex().Match(envelope.Message);
		if (installingMatch.Success)
		{
			var install = GetOrCreateInstall(providerInstalls, installingMatch.Groups["source"].Value);
			install.Installing = true;
			install.Version = installingMatch.Groups["version"].Value;
			return;
		}

		var installedMatch = InstalledProviderRegex().Match(envelope.Message);
		if (installedMatch.Success)
		{
			var install = GetOrCreateInstall(providerInstalls, installedMatch.Groups["source"].Value);
			install.Version = installedMatch.Groups["version"].Value;
			install.Installed = true;
			install.SigningKey = installedMatch.Groups["signing"].Value;
		}
	}

	private static void MarkProviderFromMessage(Dictionary<string, ProviderInstall> providerInstalls, string? message, Action<ProviderInstall> mark)
	{
		var source = TryExtractProviderSource(message);
		if (source is null)
			return;

		mark(GetOrCreateInstall(providerInstalls, source));
	}

	private static string? TryExtractProviderSource(string? message)
	{
		if (string.IsNullOrWhiteSpace(message))
			return null;

		var sourceMatch = ProviderSourceRegex().Match(message);
		return sourceMatch.Success ? sourceMatch.Groups["source"].Value : null;
	}

	private static ProviderInstall GetOrCreateInstall(Dictionary<string, ProviderInstall> providerInstalls, string source)
	{
		if (!providerInstalls.TryGetValue(source, out var install))
		{
			install = new ProviderInstall
			{
				Source = source,
			};
			providerInstalls[source] = install;
		}

		return install;
	}

	private static ChangeSummary ToChangeSummary(TerraformUiChangeSummary changes)
		=> new()
		{
			Add = changes.Add,
			Change = changes.Change,
			Remove = changes.Remove,
			Import = changes.Import,
			Forget = changes.Forget,
			Operation = ParseNullableCommandOperation(changes.Operation),
		};

	private static Diagnostic ToDiagnostic(TerraformUiDiagnostic diagnostic, TerraformUiEnvelope envelope)
		=> new()
		{
			Severity = ParseDiagnosticSeverity(diagnostic.Severity ?? envelope.Level),
			Summary = diagnostic.Summary ?? envelope.Message ?? "Terraform diagnostic",
			Detail = diagnostic.Detail,
		};

	private static ResourceChangeInfo ToResourceChange(TerraformUiChange change)
		=> new()
		{
			Resource = ToResourceReference(change.Resource),
			PreviousResource = change.PreviousResource is null ? null : ToResourceReference(change.PreviousResource),
			Action = ParseResourceAction(change.Action),
			Reason = change.Reason,
		};

	private static ResourceOperation ToResourceOperation(string type, TerraformUiHook hook)
		=> new()
		{
			Type = ParseResourceOperationType(type),
			Resource = ToResourceReference(hook.Resource!),
			Action = ParseNullableResourceAction(hook.Action),
			IdKey = hook.IdKey,
			IdValue = hook.IdValue,
			Elapsed = ToElapsed(hook.ElapsedSeconds),
		};

	private static ProvisionOperation ToProvisionOperation(string type, TerraformUiHook hook)
		=> new()
		{
			Type = ParseProvisionOperationType(type),
			Resource = ToResourceReference(hook.Resource!),
			Provisioner = hook.Provisioner,
			Output = hook.Output,
		};

	private static RefreshOperation ToRefreshOperation(string type, TerraformUiHook hook)
		=> new()
		{
			Type = ParseRefreshOperationType(type),
			Resource = ToResourceReference(hook.Resource!),
			IdKey = hook.IdKey,
			IdValue = hook.IdValue,
		};

	private static EphemeralOperation ToEphemeralOperation(string type, TerraformUiHook hook)
		=> new()
		{
			Type = ParseEphemeralOperationType(type),
			Resource = ToResourceReference(hook.Resource!),
			Action = ParseNullableResourceAction(hook.Action),
			Elapsed = ToElapsed(hook.ElapsedSeconds),
		};

	private static OutputValue ToOutputValue(TerraformUiOutputValue output)
		=> new()
		{
			Action = ParseNullableResourceAction(output.Action),
			Value = output.Value?.DeepClone(),
			Type = output.Type,
			Sensitive = output.Sensitive,
		};

	private static TimeSpan? ToElapsed(int? elapsed)
		=> elapsed.HasValue ? TimeSpan.FromSeconds(elapsed.Value) : null;

	private static DiagnosticSeverity ParseDiagnosticSeverity(string? value)
		=> (value ?? string.Empty).ToLowerInvariant() switch
		{
			"info" => DiagnosticSeverity.Info,
			"warn" or "warning" => DiagnosticSeverity.Warning,
			"error" => DiagnosticSeverity.Error,
			_ => throw new InvalidOperationException($"Unknown Terraform diagnostic severity '{value}'."),
		};

	private static CommandOperation? ParseNullableCommandOperation(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : ParseCommandOperation(value);

	private static CommandOperation ParseCommandOperation(string value)
		=> value switch
		{
			"plan" => CommandOperation.Plan,
			"apply" => CommandOperation.Apply,
			"destroy" => CommandOperation.Destroy,
			"refresh" => CommandOperation.Refresh,
			_ => throw new InvalidOperationException($"Unknown Terraform command operation '{value}'."),
		};

	private static ResourceAction? ParseNullableResourceAction(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : ParseResourceAction(value);

	private static ResourceAction ParseResourceAction(string value)
		=> value switch
		{
			"no-op" => ResourceAction.NoOp,
			"create" => ResourceAction.Create,
			"read" => ResourceAction.Read,
			"update" => ResourceAction.Update,
			"delete" => ResourceAction.Delete,
			"forget" => ResourceAction.Forget,
			"import" => ResourceAction.Import,
			"move" => ResourceAction.Move,
			_ => throw new InvalidOperationException($"Unknown Terraform resource action '{value}'."),
		};

	private static ResourceOperationType ParseResourceOperationType(string value)
		=> value switch
		{
			ApplyStartType => ResourceOperationType.ApplyStart,
			ApplyProgressType => ResourceOperationType.ApplyProgress,
			ApplyCompleteType => ResourceOperationType.ApplyComplete,
			ApplyErroredType => ResourceOperationType.ApplyErrored,
			_ => throw new InvalidOperationException($"Unknown Terraform resource operation type '{value}'."),
		};

	private static ProvisionOperationType ParseProvisionOperationType(string value)
		=> value switch
		{
			ProvisionStartType => ProvisionOperationType.ProvisionStart,
			ProvisionProgressType => ProvisionOperationType.ProvisionProgress,
			ProvisionCompleteType => ProvisionOperationType.ProvisionComplete,
			ProvisionErroredType => ProvisionOperationType.ProvisionErrored,
			_ => throw new InvalidOperationException($"Unknown Terraform provision operation type '{value}'."),
		};

	private static RefreshOperationType ParseRefreshOperationType(string value)
		=> value switch
		{
			RefreshStartType => RefreshOperationType.RefreshStart,
			RefreshCompleteType => RefreshOperationType.RefreshComplete,
			_ => throw new InvalidOperationException($"Unknown Terraform refresh operation type '{value}'."),
		};

	private static EphemeralOperationType ParseEphemeralOperationType(string value)
		=> value switch
		{
			EphemeralStartType => EphemeralOperationType.EphemeralOperationStart,
			EphemeralProgressType => EphemeralOperationType.EphemeralOperationProgress,
			EphemeralCompleteType => EphemeralOperationType.EphemeralOperationComplete,
			EphemeralErroredType => EphemeralOperationType.EphemeralOperationErrored,
			_ => throw new InvalidOperationException($"Unknown Terraform ephemeral operation type '{value}'."),
		};

	private static ResourceReference ToResourceReference(TerraformUiResourceReference resource)
		=> new()
		{
			Address = resource.Address,
			Module = resource.Module ?? string.Empty,
			Resource = resource.Resource,
			ResourceType = resource.ResourceType,
			ResourceName = resource.ResourceName,
			ImpliedProvider = resource.ImpliedProvider,
			ResourceKey = resource.ResourceKey?.DeepClone(),
		};

	[GeneratedRegex(@"provider:\s+(?<source>[^,\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex ProviderSourceRegex();

	[GeneratedRegex(@"^Finding matching versions for provider:\s+(?<source>[^,]+),\s+version_constraint:\s+""(?<constraint>.+)""$", RegexOptions.Compiled)]
	private static partial Regex FindingMatchingVersionsRegex();

	[GeneratedRegex(@"^(?<source>.+): Finding latest version\.\.\.$", RegexOptions.Compiled)]
	private static partial Regex FindingLatestVersionRegex();

	[GeneratedRegex(@"^(?<source>.+): Reusing previous version from the dependency lock file$", RegexOptions.Compiled)]
	private static partial Regex ReusingLockedVersionRegex();

	[GeneratedRegex(@"^Installing provider version: (?<source>.+?) v(?<version>\S+)\.\.\.$", RegexOptions.Compiled)]
	private static partial Regex InstallingProviderRegex();

	[GeneratedRegex(@"^Installed provider version: (?<source>.+?) v(?<version>\S+) \((?<signing>.+)\)$", RegexOptions.Compiled)]
	private static partial Regex InstalledProviderRegex();

	private sealed class TerraformUiEnvelope
	{
		[JsonPropertyName("@level")]
		public string? Level { get; set; }

		[JsonPropertyName("@message")]
		public string? Message { get; set; }

		public required string Type { get; set; }

		public string? MessageCode { get; set; }

		[JsonPropertyName("terraform")]
		public string? TerraformVersion { get; set; }

		[JsonPropertyName("ui")]
		public string? UiVersion { get; set; }

		public TerraformUiChangeSummary? Changes { get; set; }

		public TerraformUiDiagnostic? Diagnostic { get; set; }

		public TerraformUiChange? Change { get; set; }

		public Dictionary<string, TerraformUiOutputValue>? Outputs { get; set; }

		public TerraformUiHook? Hook { get; set; }
	}

	private sealed class TerraformUiChangeSummary
	{
		public int Add { get; set; }
		public int Change { get; set; }
		public int Remove { get; set; }
		public int Import { get; set; }
		public int Forget { get; set; }
		public string? Operation { get; set; }
	}

	private sealed class TerraformUiDiagnostic
	{
		public string? Severity { get; set; }
		public string? Summary { get; set; }
		public string? Detail { get; set; }
	}

	private sealed class TerraformUiChange
	{
		public required TerraformUiResourceReference Resource { get; set; }

		public TerraformUiResourceReference? PreviousResource { get; set; }

		public required string Action { get; set; }
		public string? Reason { get; set; }
	}

	private sealed class TerraformUiOutputValue
	{
		public string? Action { get; set; }
		public TFValue? Value { get; set; }
		public TFType? Type { get; set; }
		public bool Sensitive { get; set; }
	}

	private sealed class TerraformUiHook
	{
		public TerraformUiResourceReference? Resource { get; set; }
		public string? Action { get; set; }

		public string? IdKey { get; set; }

		public string? IdValue { get; set; }

		public int? ElapsedSeconds { get; set; }
		public string? Provisioner { get; set; }
		public string? Output { get; set; }
	}

	private sealed class TerraformUiResourceReference
	{
		[JsonPropertyName("addr")]
		public required string Address { get; set; }

		public string? Module { get; set; }
		public required string Resource { get; set; }

		public required string ResourceType { get; set; }

		public required string ResourceName { get; set; }

		public string? ImpliedProvider { get; set; }

		public TFValue? ResourceKey { get; set; }
	}
}
