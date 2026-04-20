using System.Text.Json;

namespace TF;

public abstract class OperationResult : CommandJsonResult
{
	public string? TerraformVersion { get; private set; }
	public string? UiVersion { get; private set; }
	public IReadOnlyList<Diagnostic> Diagnostics { get; private set; } = [];
	public IReadOnlyList<string> Messages { get; private set; } = [];
	public IReadOnlyList<string> NonJsonLines { get; private set; } = [];

	protected sealed override void LoadJson(string? output, JsonSerializerOptions options)
	{
		var response = CommandResponseParser.Parse(output, options);
		TerraformVersion = response.TerraformVersion;
		UiVersion = response.UiVersion;
		Diagnostics = response.Diagnostics;
		Messages = response.Messages;
		NonJsonLines = response.NonJsonLines;

		LoadResponse(response);
	}

	protected abstract void LoadResponse(CommandResponse response);

	protected static bool HasErrors(CommandResponse response)
		=> response.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}

public sealed class InitResult : OperationResult
{
	public bool TerraformCloudInitializing { get; private set; }
	public bool BackendInitializing { get; private set; }
	public bool ProviderPluginsInitializing { get; private set; }
	public bool ModulesInitializing { get; private set; }
	public bool CopyingConfiguration { get; private set; }
	public bool ModulesUpgrading { get; private set; }
	public bool EmptyDirectoryInitialized { get; private set; }
	public bool Initialized { get; private set; }
	public bool CloudInitialized { get; private set; }
	public bool HasCliInstructions { get; private set; }
	public bool HasCloudCliInstructions { get; private set; }
	public bool LockFileCreated { get; private set; }
	public bool LockFileChanged { get; private set; }
	public IReadOnlyList<ProviderInstall> ProviderInstalls { get; private set; } = [];

	protected override void LoadResponse(CommandResponse response)
	{
		TerraformCloudInitializing = response.Init.TerraformCloudInitializing;
		BackendInitializing = response.Init.BackendInitializing;
		ProviderPluginsInitializing = response.Init.ProviderPluginsInitializing;
		ModulesInitializing = response.Init.ModulesInitializing;
		CopyingConfiguration = response.Init.CopyingConfiguration;
		ModulesUpgrading = response.Init.ModulesUpgrading;
		EmptyDirectoryInitialized = response.Init.EmptyDirectoryInitialized;
		Initialized = response.Init.Initialized;
		CloudInitialized = response.Init.CloudInitialized;
		HasCliInstructions = response.Init.HasCliInstructions;
		HasCloudCliInstructions = response.Init.HasCloudCliInstructions;
		LockFileCreated = response.Init.LockFileCreated;
		LockFileChanged = response.Init.LockFileChanged;
		ProviderInstalls = response.ProviderInstalls;
	}
}

public sealed class RefreshResult : OperationResult
{
	public IReadOnlyList<RefreshOperation> RefreshOperations { get; private set; } = [];
	public bool Refreshed { get; private set; }

	protected override void LoadResponse(CommandResponse response)
	{
		RefreshOperations = response.RefreshOperations;
		Refreshed = Success && !HasErrors(response);
	}
}

public abstract class ChangeOperationResult : OperationResult
{
	public ChangeSummary? ChangeSummary { get; private set; }
	public IReadOnlyList<ResourceChangeInfo> ResourceDrifts { get; private set; } = [];
	public IReadOnlyList<ResourceChangeInfo> PlannedChanges { get; private set; } = [];
	public IReadOnlyDictionary<string, OutputValue> Outputs { get; private set; } = new Dictionary<string, OutputValue>(StringComparer.Ordinal);
	public bool HasChanges => PlanHasChanges ?? ChangeSummary?.HasChanges ?? false;

	protected override void LoadResponse(CommandResponse response)
	{
		ChangeSummary = response.ChangeSummary;
		ResourceDrifts = response.ResourceDrifts;
		PlannedChanges = response.PlannedChanges;
		Outputs = response.Outputs;
		LoadChangeResponse(response);
	}

	protected virtual void LoadChangeResponse(CommandResponse response)
	{
	}
}

public sealed class PlanResult : ChangeOperationResult
{
	public bool Planned { get; private set; }

	protected override void LoadChangeResponse(CommandResponse response)
	{
		Planned = Success && !HasErrors(response);
	}
}

public abstract class ExecutionResult : ChangeOperationResult
{
	public IReadOnlyList<ResourceOperation> ResourceOperations { get; private set; } = [];
	public IReadOnlyList<ProvisionOperation> ProvisionOperations { get; private set; } = [];
	public IReadOnlyList<EphemeralOperation> EphemeralOperations { get; private set; } = [];

	protected override void LoadChangeResponse(CommandResponse response)
	{
		ResourceOperations = response.ResourceOperations;
		ProvisionOperations = response.ProvisionOperations;
		EphemeralOperations = response.EphemeralOperations;
		LoadExecutionResponse(response);
	}

	protected virtual void LoadExecutionResponse(CommandResponse response)
	{
	}
}

public sealed class ApplyResult : ExecutionResult
{
	public bool Applied { get; private set; }

	protected override void LoadExecutionResponse(CommandResponse response)
	{
		Applied = Success && !HasErrors(response);
	}
}

public sealed class DestroyResult : ExecutionResult
{
	public bool Destroyed { get; private set; }

	protected override void LoadExecutionResponse(CommandResponse response)
	{
		Destroyed = Success && !HasErrors(response);
	}
}
