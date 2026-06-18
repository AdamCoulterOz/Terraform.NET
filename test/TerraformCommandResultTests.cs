using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TF.Tests.Unit;

public class TerraformCommandResultTests
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

	[Fact]
	public void ValidateResult_ShouldDeserializeJsonOutput_EvenWhenTerraformReturnsFailureExitCode()
	{
		var raw = new TFResult(
			success: false,
			output: """
			        {
			          "format_version": "1.0",
			          "valid": false,
			          "error_count": 1,
			          "warning_count": 0,
			          "diagnostics": [
			            {
			              "severity": "error",
			              "summary": "Bad config",
			              "detail": "Something is wrong"
			            }
			          ]
			        }
			        """,
			error: string.Empty,
			exitCode: 1);

		var result = new ValidateResult();
		((ITerraformCommandResult)result).LoadFromCommandResult(raw, JsonOptions);

		result.Success.Should().BeFalse();
		result.ExitCode.Should().Be(1);
		result.Valid.Should().BeFalse();
		result.ErrorCount.Should().Be(1);
		result.Diagnostics.Should().ContainSingle();
		result.Diagnostics.Single().Severity.Should().Be(DiagnosticSeverity.Error);
		result.Diagnostics.Single().Summary.Should().Be("Bad config");
	}

	[Fact]
	public void PlanResult_ShouldTransformJsonUiStream_IntoSummary()
	{
		var raw = new TFResult(
			success: true,
			output: """
			        {"@level":"info","@message":"Terraform 1.0.0","type":"version","terraform":"1.0.0","ui":"1.0"}
			        {"@level":"info","@message":"random_pet.animal: Drift detected (update)","type":"resource_drift","change":{"resource":{"addr":"random_pet.animal","module":"","resource":"random_pet.animal","implied_provider":"random","resource_type":"random_pet","resource_name":"animal","resource_key":null},"action":"update"}}
			        {"@level":"info","@message":"random_pet.animal: Plan to create","type":"planned_change","change":{"resource":{"addr":"random_pet.animal","module":"","resource":"random_pet.animal","implied_provider":"random","resource_type":"random_pet","resource_name":"animal","resource_key":null},"action":"create"}}
			        {"@level":"info","@message":"Plan: 1 to add, 0 to change, 0 to destroy.","type":"change_summary","changes":{"add":1,"change":0,"remove":0,"operation":"plan"}}
			        {"@level":"info","@message":"Outputs: 2","type":"outputs","outputs":{"pets":{"sensitive":false,"type":"string","action":"create"},"unchanged":{"sensitive":false,"action":"noop"}}}
			        """,
			error: string.Empty,
			exitCode: 2)
		{
			PlanHasChanges = true
		};

		var result = new PlanResult();
		((ITerraformCommandResult)result).LoadFromCommandResult(raw, JsonOptions);

		result.Success.Should().BeTrue();
		result.PlanHasChanges.Should().BeTrue();
		result.ChangeSummary.Should().NotBeNull();
		result.ChangeSummary!.Add.Should().Be(1);
		result.HasChanges.Should().BeTrue();
		result.Planned.Should().BeTrue();
		result.TerraformVersion.Should().Be("1.0.0");
		result.ResourceDrifts.Should().ContainSingle();
		result.ResourceDrifts.Single().Action.Should().Be(ResourceAction.Update);
		result.PlannedChanges.Should().ContainSingle();
		result.PlannedChanges.Single().Action.Should().Be(ResourceAction.Create);
		result.Outputs.Should().ContainKey("pets");
		result.Outputs["pets"].Action.Should().Be(ResourceAction.Create);
		result.Outputs["pets"].Type.Should().Be(TFStringType.Instance);
		result.Outputs.Should().ContainKey("unchanged");
		result.Outputs["unchanged"].Action.Should().Be(ResourceAction.NoOp);
	}

	[Fact]
	public void TerraformCommandResponseParser_ShouldParseFullerPlanJsonUiStream_WithNoopOutputActions()
	{
		var response = TerraformCommandResponseParser.ParseJsonUiStream(
			"""
			{"@level":"info","@message":"Terraform 1.11.0","@module":"terraform.ui","@timestamp":"2026-05-16T05:01:00.000000Z","type":"version","terraform":"1.11.0","ui":"1.2"}
			{"@level":"info","@message":"module.crm.powerplatform_environment.environment: Refreshing state... [id=00000000-0000-0000-0000-000000000001]","@module":"terraform.ui","@timestamp":"2026-05-16T05:01:01.000000Z","type":"refresh_start","hook":{"resource":{"addr":"module.crm.powerplatform_environment.environment","module":"module.crm","resource":"powerplatform_environment.environment","implied_provider":"power-platform","resource_type":"powerplatform_environment","resource_name":"environment","resource_key":null},"id_key":"id","id_value":"00000000-0000-0000-0000-000000000001"}}
			{"@level":"info","@message":"module.crm.powerplatform_environment.environment: Refresh complete [id=00000000-0000-0000-0000-000000000001]","@module":"terraform.ui","@timestamp":"2026-05-16T05:01:02.000000Z","type":"refresh_complete","hook":{"resource":{"addr":"module.crm.powerplatform_environment.environment","module":"module.crm","resource":"powerplatform_environment.environment","implied_provider":"power-platform","resource_type":"powerplatform_environment","resource_name":"environment","resource_key":null},"id_key":"id","id_value":"00000000-0000-0000-0000-000000000001"}}
			{"@level":"info","@message":"module.api.azurerm_resource_group.main: Drift detected (update)","@module":"terraform.ui","@timestamp":"2026-05-16T05:01:03.000000Z","type":"resource_drift","change":{"resource":{"addr":"module.api.azurerm_resource_group.main","module":"module.api","resource":"azurerm_resource_group.main","implied_provider":"azurerm","resource_type":"azurerm_resource_group","resource_name":"main","resource_key":null},"action":"update","reason":"read_because_config_unknown"}}
			{"@level":"info","@message":"module.crm.powerplatform_solution.solution: Plan to update","@module":"terraform.ui","@timestamp":"2026-05-16T05:01:04.000000Z","type":"planned_change","change":{"resource":{"addr":"module.crm.powerplatform_solution.solution","module":"module.crm","resource":"powerplatform_solution.solution","implied_provider":"power-platform","resource_type":"powerplatform_solution","resource_name":"solution","resource_key":null},"action":"update","reason":"attribute_changed"}}
			{"@level":"info","@message":"module.api.azurerm_key_vault_secret.setting[\"crm_url\"]: Plan to create","@module":"terraform.ui","@timestamp":"2026-05-16T05:01:05.000000Z","type":"planned_change","change":{"resource":{"addr":"module.api.azurerm_key_vault_secret.setting[\"crm_url\"]","module":"module.api","resource":"azurerm_key_vault_secret.setting","implied_provider":"azurerm","resource_type":"azurerm_key_vault_secret","resource_name":"setting","resource_key":"crm_url"},"previous_resource":{"addr":"module.api.azurerm_key_vault_secret.old_setting[\"crm_url\"]","module":"module.api","resource":"azurerm_key_vault_secret.old_setting","implied_provider":"azurerm","resource_type":"azurerm_key_vault_secret","resource_name":"old_setting","resource_key":"crm_url"},"action":"create","reason":"replace_because_cannot_update"}}
			{"@level":"info","@message":"module.crm.powerplatform_managed_environment.managed[0]: Plan to replace","@module":"terraform.ui","@timestamp":"2026-05-16T05:01:05.500000Z","type":"planned_change","change":{"resource":{"addr":"module.crm.powerplatform_managed_environment.managed[0]","module":"module.crm","resource":"powerplatform_managed_environment.managed[0]","implied_provider":"powerplatform","resource_type":"powerplatform_managed_environment","resource_name":"managed","resource_key":0},"action":"replace","reason":"tainted"}}
			{"@level":"info","@message":"Outputs: 3","@module":"terraform.ui","@timestamp":"2026-05-16T05:01:06.000000Z","type":"outputs","outputs":{"crm_environment_url":{"sensitive":false,"type":"string","value":"https://org.crm6.dynamics.com","action":"noop"},"crm_solution_id":{"sensitive":false,"type":"string","action":"noop"},"api_endpoint":{"sensitive":false,"type":["object",{"url":"string","ready":"bool"}],"value":{"url":"https://api.example.test","ready":true},"action":"update"}}}
			{"@level":"info","@message":"Plan: 1 to add, 1 to change, 0 to destroy.","@module":"terraform.ui","@timestamp":"2026-05-16T05:01:07.000000Z","type":"change_summary","changes":{"add":1,"change":1,"remove":0,"import":0,"forget":0,"operation":"plan"}}
			""");

		response.TerraformVersion.Should().Be("1.11.0");
		response.UiVersion.Should().Be("1.2");
		response.RefreshOperations.Should().HaveCount(2);
		response.ResourceDrifts.Should().ContainSingle();
		response.ResourceDrifts.Single().Reason.Should().Be("read_because_config_unknown");
		response.PlannedChanges.Should().HaveCount(3);
		response.PlannedChanges.Select(change => change.Action).Should().Contain([ResourceAction.Update, ResourceAction.Create, ResourceAction.Replace]);
		response.PlannedChanges[1].PreviousResource.Should().NotBeNull();
		response.PlannedChanges[1].Resource.ResourceKey!.GetValue<string>().Should().Be("crm_url");
		response.Outputs.Should().ContainKey("crm_environment_url");
		response.Outputs["crm_environment_url"].Action.Should().Be(ResourceAction.NoOp);
		response.Outputs["crm_environment_url"].Value!.GetValue<string>().Should().Be("https://org.crm6.dynamics.com");
		response.Outputs["crm_solution_id"].Action.Should().Be(ResourceAction.NoOp);
		response.Outputs["crm_solution_id"].Type.Should().Be(TFStringType.Instance);
		response.Outputs["api_endpoint"].Type.Should().BeOfType<TFObjectType>();
		response.ChangeSummary!.Add.Should().Be(1);
		response.ChangeSummary.Change.Should().Be(1);
		response.ChangeSummary.Operation.Should().Be(CommandOperation.Plan);
	}

	[Fact]
	public void ApplyResult_ShouldIgnoreJsonLinesWithoutTerraformUiType()
	{
		var raw = new TFResult(
			success: true,
			output: """
			        {"@level":"info","@message":"Terraform 1.15.5","@module":"terraform.ui","type":"version","terraform":"1.15.5","ui":"1.3"}
			        {"event":"provider-log","message":"provider emitted a JSON object outside Terraform UI"}
			        {"@level":"info","@message":"azapi_resource.example: Creation complete after 1.25s [id=/subscriptions/000/resourceGroups/rg/providers/Microsoft.Resources/deployments/example]","@module":"terraform.ui","type":"apply_complete","hook":{"resource":{"addr":"azapi_resource.example","module":"","resource":"azapi_resource.example","implied_provider":"azapi","resource_type":"azapi_resource","resource_name":"example","resource_key":null},"action":"create","id_key":"id","id_value":"/subscriptions/000/resourceGroups/rg/providers/Microsoft.Resources/deployments/example","elapsed_seconds":1.25}}
			        {"@level":"info","@message":"Apply complete! Resources: 1 added, 0 changed, 0 destroyed.","@module":"terraform.ui","type":"change_summary","changes":{"add":1,"change":0,"remove":0,"import":0,"forget":0,"operation":"apply"}}
			        """,
			error: string.Empty,
			exitCode: 0);

		var result = new ApplyResult();
		((ITerraformCommandResult)result).LoadFromCommandResult(raw, JsonOptions);

		result.Applied.Should().BeTrue();
		result.ChangeSummary.Should().NotBeNull();
		result.ChangeSummary!.Operation.Should().Be(CommandOperation.Apply);
		result.ResourceOperations.Should().ContainSingle();
		result.ResourceOperations.Single().Type.Should().Be(ResourceOperationType.ApplyComplete);
		result.ResourceOperations.Single().Elapsed.Should().Be(TimeSpan.FromSeconds(1.25));
	}

	[Fact]
	public void InitResult_ShouldTransformKnownInitOutput()
	{
		var raw = new TFResult(
			success: true,
			output: """
				        {"@level":"info","@message":"Terraform 1.0.0","type":"version","terraform":"1.0.0","ui":"1.0"}
				        {"@level":"info","@message":"Initializing the backend...","type":"init_output","message_code":"initializing_backend_message"}
				        {"@level":"info","@message":"Initializing provider plugins...","type":"init_output","message_code":"initializing_provider_plugin_message"}
				        {"@level":"info","@message":"hashicorp/random: Finding latest version...","type":"log"}
				        {"@level":"info","@message":"Installing provider version: hashicorp/random v3.8.1...","type":"log"}
				        {"@level":"info","@message":"Installed provider version: hashicorp/random v3.8.1 (signed by HashiCorp)","type":"log"}
				        {"@level":"info","@message":"Terraform has created a lock file .terraform.lock.hcl","type":"init_output","message_code":"lock_info"}
				        Successfully configured the backend "local"!
				        {"@level":"warn","@message":"Warning: Provider development overrides are in effect","type":"diagnostic","diagnostic":{"severity":"warning","summary":"Provider development overrides are in effect","detail":"Skip terraform init"}}
				        {"@level":"info","@message":"Terraform has been successfully initialized!","type":"init_output","message_code":"output_init_success_message"}
				        """,
			error: string.Empty,
			exitCode: 0);

		var result = new InitResult();
		((ITerraformCommandResult)result).LoadFromCommandResult(raw, JsonOptions);

		result.TerraformVersion.Should().Be("1.0.0");
		result.UiVersion.Should().Be("1.0");
		result.BackendInitializing.Should().BeTrue();
		result.ProviderPluginsInitializing.Should().BeTrue();
		result.LockFileCreated.Should().BeTrue();
		result.Initialized.Should().BeTrue();
		result.Diagnostics.Should().ContainSingle();
		result.Diagnostics.Single().Severity.Should().Be(DiagnosticSeverity.Warning);
		result.NonJsonLines.Should().ContainSingle().Which.Should().Be("Successfully configured the backend \"local\"!");
		result.ProviderInstalls.Should().ContainSingle();
		result.ProviderInstalls.Single().Source.Should().Be("hashicorp/random");
		result.ProviderInstalls.Single().Version.Should().Be("3.8.1");
		result.ProviderInstalls.Single().Installed.Should().BeTrue();
	}

	[Fact]
	public void ApplyResult_ShouldTransformOperationHooks_AndOutputs()
	{
		var raw = new TFResult(
			success: true,
			output: """
			        {"@level":"info","@message":"Terraform 1.0.0","type":"version","terraform":"1.0.0","ui":"1.0"}
			        {"@level":"info","@message":"random_pet.animal: Plan to create","type":"planned_change","change":{"resource":{"addr":"random_pet.animal","module":"","resource":"random_pet.animal","implied_provider":"random","resource_type":"random_pet","resource_name":"animal","resource_key":null},"action":"create"}}
			        {"@level":"info","@message":"random_pet.animal: Creating...","type":"apply_start","hook":{"resource":{"addr":"random_pet.animal","module":"","resource":"random_pet.animal","implied_provider":"random","resource_type":"random_pet","resource_name":"animal","resource_key":null},"action":"create"}}
			        {"@level":"info","@message":"random_pet.animal: Creation complete after 1.25s [id=smart-lizard]","type":"apply_complete","hook":{"resource":{"addr":"random_pet.animal","module":"","resource":"random_pet.animal","implied_provider":"random","resource_type":"random_pet","resource_name":"animal","resource_key":null},"action":"create","id_key":"id","id_value":"smart-lizard","elapsed_seconds":1.25}}
			        {"@level":"info","@message":"null_resource.none[0]: Provisioning with 'local-exec'...","type":"provision_start","hook":{"resource":{"addr":"null_resource.none[0]","module":"","resource":"null_resource.none[0]","implied_provider":"null","resource_type":"null_resource","resource_name":"none","resource_key":0},"provisioner":"local-exec"}}
			        {"@level":"info","@message":"Outputs: 1","type":"outputs","outputs":{"pets":{"sensitive":false,"type":"string","value":"smart-lizard"}}}
			        {"@level":"info","@message":"Apply complete! Resources: 1 added, 0 changed, 0 destroyed.","type":"change_summary","changes":{"add":1,"change":0,"remove":0,"operation":"apply"}}
			        """,
			error: string.Empty,
			exitCode: 0);

		var result = new ApplyResult();
		((ITerraformCommandResult)result).LoadFromCommandResult(raw, JsonOptions);

		result.Applied.Should().BeTrue();
		result.PlannedChanges.Should().ContainSingle();
		result.ResourceOperations.Should().HaveCount(2);
		result.ResourceOperations.First().Type.Should().Be(ResourceOperationType.ApplyStart);
		result.ResourceOperations.First().Action.Should().Be(ResourceAction.Create);
		result.ResourceOperations.Last().Type.Should().Be(ResourceOperationType.ApplyComplete);
		result.ResourceOperations.Last().IdValue.Should().Be("smart-lizard");
		result.ResourceOperations.Last().Elapsed.Should().Be(TimeSpan.FromSeconds(1.25));
		result.ProvisionOperations.Should().ContainSingle();
		result.ProvisionOperations.Single().Type.Should().Be(ProvisionOperationType.ProvisionStart);
		result.ProvisionOperations.Single().Provisioner.Should().Be("local-exec");
		result.ProvisionOperations.Single().Resource.ResourceKey.Should().NotBeNull();
		result.ProvisionOperations.Single().Resource.ResourceKey!.GetValue<int>().Should().Be(0);
		result.Outputs.Should().ContainKey("pets");
		result.Outputs["pets"].Value.Should().NotBeNull();
		result.Outputs["pets"].Value!.GetValue<string>().Should().Be("smart-lizard");
		result.ChangeSummary!.Operation.Should().Be(CommandOperation.Apply);
	}

	[Fact]
	public void ShowJsonResult_ShouldDeserializePlanDocument()
	{
		var raw = new TFResult(
			success: true,
			output: """
			        {
			          "format_version": "1.0",
			          "applyable": true,
			          "complete": true,
			          "errored": false,
			          "variables": {
			            "name": {
			              "value": "smart-lizard"
			            }
			          },
			          "resource_changes": [
			            {
			              "address": "random_pet.animal",
			              "mode": "managed",
			              "type": "random_pet",
			              "name": "animal",
			              "change": {
			                "actions": ["create"],
			                "before": null,
			                "after": {
			                  "id": "smart-lizard"
			                },
			                "replace_paths": []
			              }
			            }
			          ],
			          "output_changes": {
			            "pets": {
			              "change": {
			                "actions": ["create"],
			                "before": null,
			                "after": "smart-lizard",
			                "replace_paths": []
			              }
			            }
			          }
			        }
			        """,
			error: string.Empty,
			exitCode: 0);

		var result = new ShowJsonResult();
		((ITerraformCommandResult)result).LoadFromCommandResult(raw, JsonOptions);

		result.Success.Should().BeTrue();
		result.Document["format_version"].GetValue<string>().Should().Be("1.0");
		result.Document["applyable"].GetValue<bool>().Should().BeTrue();
		result.Document["variables"].Should().BeOfType<TFObject>();
		result.Document["resource_changes"].Should().BeOfType<TFArray>();
		result.Document["output_changes"].Should().BeOfType<TFObject>();
	}

	[Fact]
	public void ShowFileResult_ShouldRetainRenderedPlanOutput()
	{
		var raw = new TFResult(
			success: true,
			output: """
			        Terraform used the selected providers to generate the following execution plan.
			          + create random_pet.animal
			        """,
			error: string.Empty,
			exitCode: 0);

		var result = ShowFileResult.From(raw);

		result.Success.Should().BeTrue();
		result.ExitCode.Should().Be(0);
		result.Output.Should().Contain("execution plan");
		result.Error.Should().BeNull();
	}
}
