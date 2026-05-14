namespace TF;

public sealed class TerraformPlanReview
{
	private TerraformPlanReview(IReadOnlyList<TerraformResourceChange> resourceChanges)
		=> ResourceChanges = resourceChanges;

	public IReadOnlyList<TerraformResourceChange> ResourceChanges { get; }

	public static TerraformPlanReview From(TFObject showDocument)
	{
		ArgumentNullException.ThrowIfNull(showDocument);

		if (!showDocument.TryGetValue("resource_changes", out var resourceChangesValue) ||
		    resourceChangesValue is not TFArray resourceChanges)
			return new TerraformPlanReview([]);

		return new TerraformPlanReview(resourceChanges
			.OfType<TFObject>()
			.Select(ResourceChange)
			.Where(change => change is not null)
			.Cast<TerraformResourceChange>()
			.ToArray());
	}

	public IReadOnlyList<ProtectedResourceChange> ProtectedChanges(ProtectedResourcePolicy policy)
	{
		ArgumentNullException.ThrowIfNull(policy);

		return ResourceChanges
			.Where(change => policy.Contains(change.Type) && change.Actions.Any(IsDestructiveAction))
			.Select(change => new ProtectedResourceChange(change.Address, change.Type, change.Actions))
			.ToArray();
	}

	private static TerraformResourceChange? ResourceChange(TFObject resourceChange)
	{
		var address = String(resourceChange, "address");
		var type = String(resourceChange, "type");
		if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(type))
			return null;

		if (!resourceChange.TryGetValue("change", out var changeValue) || changeValue is not TFObject change)
			return null;

		return new TerraformResourceChange(address, type, Actions(change));
	}

	private static IReadOnlyList<string> Actions(TFObject change)
	{
		if (!change.TryGetValue("actions", out var actionsValue) || actionsValue is not TFArray actions)
			return [];

		return actions
			.OfType<TFString>()
			.Select(action => action.Value)
			.ToArray();
	}

	private static string String(TFObject obj, string key)
		=> obj.TryGetValue(key, out var value) && value is TFString text
			? text.Value
			: string.Empty;

	private static bool IsDestructiveAction(string action)
		=> StringComparer.Ordinal.Equals(action, "delete") ||
		   StringComparer.Ordinal.Equals(action, "replace");
}

public sealed record TerraformResourceChange(
	string Address,
	string Type,
	IReadOnlyList<string> Actions);

public sealed record ProtectedResourceChange(
	string Address,
	string Type,
	IReadOnlyList<string> Actions);

public sealed class ProtectedResourcePolicy
{
	private readonly IReadOnlySet<string> _resourceTypes;

	public ProtectedResourcePolicy(IEnumerable<string> resourceTypes)
	{
		ArgumentNullException.ThrowIfNull(resourceTypes);
		_resourceTypes = resourceTypes
			.Where(resourceType => !string.IsNullOrWhiteSpace(resourceType))
			.ToHashSet(StringComparer.Ordinal);
	}

	public bool Contains(string resourceType) => _resourceTypes.Contains(resourceType);
}
