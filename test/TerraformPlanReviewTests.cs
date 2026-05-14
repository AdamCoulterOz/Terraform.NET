using FluentAssertions;
using Xunit;

namespace TF.Tests.Unit;

public class TerraformPlanReviewTests
{
	[Fact]
	public void ProtectedChanges_ShouldReturnDestructiveProtectedResources()
	{
		var show = new TFObject(new Dictionary<string, TFValue>
		{
			["resource_changes"] = new TFArray([
				new TFObject(new Dictionary<string, TFValue>
				{
					["address"] = "azurerm_resource_group.main",
					["type"] = "azurerm_resource_group",
					["change"] = new TFObject(new Dictionary<string, TFValue>
					{
						["actions"] = new TFArray([new TFString("delete")])
					})
				}),
				new TFObject(new Dictionary<string, TFValue>
				{
					["address"] = "random_pet.name",
					["type"] = "random_pet",
					["change"] = new TFObject(new Dictionary<string, TFValue>
					{
						["actions"] = new TFArray([new TFString("delete")])
					})
				})
			])
		});

		var changes = TerraformPlanReview.From(show)
			.ProtectedChanges(new ProtectedResourcePolicy(["azurerm_resource_group"]));

		changes.Should().ContainSingle();
		changes.Single().Address.Should().Be("azurerm_resource_group.main");
		changes.Single().Type.Should().Be("azurerm_resource_group");
		changes.Single().Actions.Should().Equal("delete");
	}
}
