using TF.Attributes;

namespace TF.Commands;

public abstract class Main : Action
{
	/// <summary>Note that some actions may require interactive prompts and will error if this is disabled.</summary>
	[CliOption("input")]
	public bool? InteractiveInput { get; set; } = false;

	/// <summary>Default: `true`. Disabling the lock is dangerous if others might concurrently run commands against the same workspace.</summary>
	[CliOption("lock")]
	public bool? StateLock { get; set; } = true;

	/// <summary>Timeout in seconds to wait for state to unlock before giving up.</summary>
	[CliOption("lock-timeout")]
	public int? LockTimeout { get; set; } = 0;
}
