using TF.Attributes;

namespace TF.Commands;

public abstract class Action : ICliAttributed
{
	protected abstract string Command { get; }
	public string GetCommand() => $"{CliGlobalOptionAttribute.BuildArguments(this)} {Command} {CliOptionAttribute.BuildArguments(this)} {CliArgumentAttribute.BuildArguments(this)}".Trim();

	/// <summary>Switch to a different working directory before executing the given subcommand.</summary>
	[CliGlobalOption("chdir")]
	public DirectoryInfo? WorkingDirectory { get; set; }

	/// <summary>Show this help output, or the help for a specified subcommand.</summary>
	[CliGlobalOption("help")]
	public bool? Help { get; set; }

	/// <summary>Remove color formatting in console command output.</summary>
	[CliOption("no-color")]
	public bool? NoColor { get; set; } = true;
}
