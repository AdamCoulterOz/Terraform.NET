using TF.Attributes;
using TF.Model;

namespace TF.Commands;

public abstract class Main<T> : Action<T>
	where T : IOutput
{
	/// <summary>Note that some actions may require interactive prompts and will error if this is disabled.</summary>
	[CliOption("input")]
	internal static bool InteractiveInput => false;

	/// <summary>Default: `true`. Disabling the lock is dangerous if others might concurrently run commands against the same workspace.</summary>
	[CliOption("lock")]
	public bool? StateLock { get; set; }

	/// <summary>Default: `0`. Timeout in seconds to wait for state to unlock before giving up.</summary>
	[CliOption("lock-timeout")]
	public int? LockTimeout { get; set; }
}
