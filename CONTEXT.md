# Terraform.NET Context

Last updated: 2026-04-20 (Australia/Sydney)

## Project Purpose
`Terraform.NET` is a .NET wrapper around the Terraform CLI. It creates a temporary working directory, can write Terraform configuration inputs into that workspace, runs Terraform commands through `CliWrap`, and projects Terraform JSON output into typed, operation-specific .NET results.

## Current Baseline
- Default branch: `main`
- Runtime target: `.NET 10` (`src/TF/TF.csproj`)
- Verification status on `main`:
  - `dotnet test test/TF.Tests.Unit.csproj` passes
  - `dotnet restore src/TF/TF.csproj` is clean after replacing the deprecated Fluent ARM package
- CI workflow now publishes from `main` in `.github/workflows/main.yml` and builds `Terraform.NET.slnx`

## High-Level Architecture
- Main wrapper: `src/TF/Terraform.cs`
- Backend abstraction: `src/TF/Backend.cs`
- Azure assembly: `src/TF.Azure/TF.Azure.csproj`
- MySQL assembly: `src/TF.MySql/TF.MySql.csproj`
- Provider abstraction: `src/TF/Provider.cs`
- Provider alias collection: `src/TF/ProviderCollection.cs`
- Variable serialization: `src/TF/Variables.cs`
- CLI configuration file writer: `src/TF/Configuration.cs`
- Typed command result models:
  - `src/TF/CommandJsonResult.cs`
  - `src/TF/OperationResult.cs`
  - `src/TF/CommandResponse.cs`
  - `src/TF/ValidateResult.cs`
  - `src/TF/Model/Plan.cs`
- Built-in providers/backends:
  - `src/TF/BuiltIn/*`
  - `src/TF.Azure/*`
  - `src/TF.MySql/*`
- Unit tests:
  - `test/ProvisioningTests.cs`
  - `test/TerraformCommandResultTests.cs`

## Command Execution Flow
1. A `Terraform` instance is created with a backend, working directory, and Terraform executable path.
2. `Init`, `Validate`, `Plan`, `Apply`, `Destroy`, `Refresh`, and `Show` build CLI arguments directly in `src/TF/Terraform.cs`.
3. Variables are written to `execute.tfvars.json` when needed.
4. Backend settings are added through backend arguments plus a generated `_backend.tf`.
5. Provider bindings are rewritten into `providers.auto.tf.json` inside the execution root before Terraform runs.
6. Raw command execution returns an internal `TFResult`, and `Command<TResult>` maps JSON output into typed command result models.
7. JSONL UI output from commands such as `init`, `plan`, `apply`, `destroy`, and `refresh` is parsed into a canonical internal command response carrying:
   - init status and provider install state
   - resource drift and planned change records
   - outputs
   - resource apply/provision/refresh/ephemeral operation records
   - diagnostics and change summaries
   - enums for operation/action/severity discriminators and `TimeSpan` for elapsed durations
8. The canonical command response is then transformed into operation-specific result objects (`InitResult`, `PlanResult`, `ApplyResult`, `DestroyResult`, `RefreshResult`).
9. `Plan()` writes a managed scratch plan file inside the execution root, and `Show<TResult>()` consumes that file and cleans it up after deserializing it.

## Provider Alias Model (Current)
- Providers are stored in `ProviderCollection` under `(alias, provider)` keys.
- `ProviderConfigurationRewriter` scans the root execution workspace for provider blocks in `.tf` and `.tf.json` files.
- Extracted provider settings are merged with bound provider values from `ProviderCollection`.
- The final resolved provider configuration is written to `providers.auto.tf.json`.
- Original provider blocks are removed from the copied root files so the generated file is the single source of truth for that run.

## Current Limitations
- The HCL extraction path is intentionally focused on provider blocks and common provider-setting expressions. If a provider block uses unsupported HCL constructs, the runtime should fail loudly rather than silently misrewrite it.
- Tests currently cover the rewrite/merge flow directly, but not a full Terraform integration run with multiple real aliased providers.
- Consumers now need explicit references to the cloud/database-specific assemblies when they use those provider/backend types; the core assembly no longer carries those implementations.

## Direction For Provider Mapping Work
- Extend test coverage for more provider block shapes, especially nested/repeated blocks and expression-heavy settings.
- Decide whether provider rewrite should expose diagnostics or dry-run inspection output for callers.

## Key Invariants
- Never mutate the caller's source Terraform files in place; only rewrite the copied execution workspace.
- Prefer explicit failures over silent fallback behavior.
- Keep provider binding logic deterministic so the generated runtime config is the single source of truth for that run.
- Keep Terraform scratch artifacts such as the managed plan file hidden behind the `Terraform` API rather than exposing file paths to callers.
- Keep raw Terraform event-stream details behind canonical parsing and transformed command results; callers should consume logical operation results rather than JSONL message envelopes.

## Outstanding Follow-Up
- Decide whether package dependency upgrades should be done before or after the provider mapping work.
- Add an end-to-end Terraform integration test for aliased providers once a stable sample configuration is in place.
- Decide whether the new Azure backend assembly should become a separately published package or stay as a solution-level assembly only.

## Technical Debt
- `src/TF/Terraform.cs` assembles command arguments inline; command construction is not yet modeled as strongly typed command objects.
- Test coverage is still fairly thin on `main`, even after the provider rewrite unit tests.
- The provider rewriter currently hand-parses HCL instead of using a dedicated parser library.
- Packaging and release flow still assume a single main package; the new provider/backend assemblies are solution-level splits but not yet a finalized multi-package publishing model.
