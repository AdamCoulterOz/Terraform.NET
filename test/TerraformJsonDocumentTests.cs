using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TF.Tests.Unit;

public class TerraformJsonDocumentTests
{
	[Fact]
	public void TerraformLabel_ShouldNormalizeResourceLabels()
	{
		TerraformLabel.From("123 core-api").ToString().Should().Be("pkg_123_core_api");
		TerraformLabel.From("core.api").ToString().Should().Be("core_api");
	}

	[Fact]
	public void TerraformJsonDocument_ShouldSerializeTerraformJsonShape()
	{
		var document = new TerraformJsonDocument();
		document.Resources.Add("example_resource", "main", new Dictionary<string, object?>
		{
			["name"] = "core",
			["enabled"] = true,
			["source_id"] = TFExpression.Interpolation("data.example.current.id")
		});
		document.Outputs.Add("resource_id", new Dictionary<string, object?>
		{
			["value"] = TFExpression.Interpolation("example_resource.main.id")
		});

		var payload = JsonSerializer.Deserialize<JsonElement>(document.ToJsonString());

		payload.GetProperty("resource")
			.GetProperty("example_resource")
			.GetProperty("main")
			.GetProperty("source_id")
			.GetString()
			.Should()
			.Be("${data.example.current.id}");
		payload.GetProperty("output")
			.GetProperty("resource_id")
			.GetProperty("value")
			.GetString()
			.Should()
			.Be("${example_resource.main.id}");
	}
}
