using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TF.Tests.Unit;

public class TFTypeTests
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

	[Fact]
	public void Parse_ShouldHandleTerraformConstraintSyntax()
	{
		var type = TFType.Parse("tuple([string, number, object({ name = string })])");

		type.Should().BeOfType<TFTupleType>();
		var tuple = (TFTupleType)type;
		tuple.ElementTypes.Should().HaveCount(3);
		tuple.ElementTypes[0].Should().Be(TFStringType.Instance);
		tuple.ElementTypes[1].Should().Be(TFNumberType.Instance);
		tuple.ElementTypes[2].Should().BeOfType<TFObjectType>();
		((TFObjectType)tuple.ElementTypes[2]).Attributes["name"].Should().Be(TFStringType.Instance);
	}

	[Fact]
	public void JsonConverter_ShouldHandleTerraformTypeSerialization()
	{
		var json = """
		           ["object",{"name":"string","tags":["list","string"],"shape":["tuple",["number","bool"]]}]
		           """;

		var type = JsonSerializer.Deserialize<TFType>(json, JsonOptions);

		type.Should().BeOfType<TFObjectType>();
		var obj = (TFObjectType)type!;
		obj.Attributes["name"].Should().Be(TFStringType.Instance);
		obj.Attributes["tags"].Should().Be(new TFListType(TFStringType.Instance));
		obj.Attributes["shape"].Should().BeOfType<TFTupleType>();
		((TFTupleType)obj.Attributes["shape"]).ElementTypes.Should().Equal(TFNumberType.Instance, TFBoolType.Instance);
	}

	[Fact]
	public void Parse_ShouldTreatListMapSetShorthandAsAny()
	{
		TFType.Parse("list").Should().Be(new TFListType(TFAnyType.Instance));
		TFType.Parse("map").Should().Be(new TFMapType(TFAnyType.Instance));
		TFType.Parse("set").Should().Be(new TFSetType(TFAnyType.Instance));
	}
}
