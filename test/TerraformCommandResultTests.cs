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
			        {"@level":"info","@message":"Outputs: 1","type":"outputs","outputs":{"pets":{"sensitive":false,"type":"string","action":"create"}}}
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
			        {"@level":"info","@message":"random_pet.animal: Creation complete after 0s [id=smart-lizard]","type":"apply_complete","hook":{"resource":{"addr":"random_pet.animal","module":"","resource":"random_pet.animal","implied_provider":"random","resource_type":"random_pet","resource_name":"animal","resource_key":null},"action":"create","id_key":"id","id_value":"smart-lizard","elapsed_seconds":0}}
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
		result.ResourceOperations.Last().Elapsed.Should().Be(TimeSpan.Zero);
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
}
