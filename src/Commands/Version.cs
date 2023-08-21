using System.Text.RegularExpressions;

namespace TF.Commands;

public sealed partial class Version : Action<Model.Version>
{
	protected override string Command => "version";

	public override Model.Version Parse(string output)
	{
		var versionMatch = VersionMatch().Match(output);
		var architectureMatch = ArchitectureMatch().Match(output);

		if (!versionMatch.Success || !architectureMatch.Success)
			throw new FormatException("Could not extract version and architecture information from input string.");

		return new Model.Version
		{
			Number = versionMatch.Value.AsSpan(1).ToString(),
			Architecture = architectureMatch.Value.AsSpan(3).ToString()
		};
	}

    [GeneratedRegex(@"v\d+\.\d+\.\d+")]
    private static partial Regex VersionMatch();
	
    [GeneratedRegex(@"on \w+_\w+")]
    private static partial Regex ArchitectureMatch();
}
