using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TF.Tests.Unit;

public class TFValueTests
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

	[Fact]
	public void TFValue_ShouldRoundTripNestedClrShapes()
	{
		var value = TFValue.FromObject(new Dictionary<string, object?>
		{
			["name"] = "pet",
			["count"] = 2,
			["enabled"] = true,
			["tags"] = new[] { "friendly", "small" },
			["settings"] = new Dictionary<string, object?>
			{
				["owner"] = "adam"
			}
		});

		var json = JsonSerializer.Serialize(value, JsonOptions);
		var roundTrip = JsonSerializer.Deserialize<TFValue>(json, JsonOptions);

		roundTrip.Should().BeOfType<TFObject>();
		var root = (TFObject)roundTrip!;
		root["name"].GetValue<string>().Should().Be("pet");
		root["count"].Should().BeOfType<TFNumber>();
		root["count"].GetValue<int>().Should().Be(2);
		root["count"].GetValue<decimal>().Should().Be(2m);
		root["enabled"].GetValue<bool>().Should().BeTrue();
		root["tags"].Should().BeOfType<TFArray>();
		((TFArray)root["tags"]).Select(item => item.GetValue<string>())
			.Should().Equal("friendly", "small");
		root["settings"].Should().BeOfType<TFObject>();
		((TFObject)root["settings"])["owner"].GetValue<string>().Should().Be("adam");
	}
}
