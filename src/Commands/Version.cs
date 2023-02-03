using System.Text.RegularExpressions;

namespace TF.Commands;

public sealed class Version : Action<Model.Version>
{
	protected override string Command => "version";

	public override Model.Version Parse(string output)
	{
		var versionMatch = Regex.Match(output, @"v\d+\.\d+\.\d+");
		var architectureMatch = Regex.Match(output, @"on \w+_\w+");

		if (!versionMatch.Success || !architectureMatch.Success)
			throw new FormatException("Could not extract version and architecture information from input string.");

		return new Model.Version
		{
			Number = versionMatch.Value.AsSpan(1).ToString(),
			Architecture = architectureMatch.Value.AsSpan(3).ToString()
		};
	}
}
