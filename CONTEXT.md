# Terraform.NET Context

Last updated: 2026-04-20 (Australia/Sydney)

## Project Purpose
`Terraform.NET` is a .NET wrapper around the Terraform CLI. It creates a temporary working directory, can write Terraform configuration inputs into that workspace, runs Terraform commands through `CliWrap`, and returns command results to .NET callers.

## Current Baseline
- Default branch: `main`
- Runtime target: `.NET 6` (`src/TF.csproj`)
- Verification status on `main`:
  - `dotnet test test/TF.Tests.Unit.csproj` passes
  - restore/build emits dependency vulnerability warnings for `Azure.Identity`, `Azure.Storage.Blobs`, and `System.IdentityModel.Tokens.Jwt`
- CI workflow now publishes from `main` in `.github/workflows/main.yml`

## High-Level Architecture
- Main wrapper: `src/Terraform.cs`
- Backend abstraction: `src/Backend.cs`
- Provider abstraction: `src/Provider.cs`
- Provider alias collection: `src/ProviderCollection.cs`
- Variable serialization: `src/Variables.cs`
- CLI configuration file writer: `src/Configuration.cs`
- Built-in providers/backends:
  - `src/BuiltIn/*`
  - `src/Providers/Azure/*`
  - `src/Providers/MySql/*`
- Unit tests:
  - `test/ProvisioningTests.cs`

## Command Execution Flow
1. A `Terraform` instance is created with a backend, working directory, and Terraform executable path.
2. `Init`, `Validate`, `Plan`, `Apply`, `Destroy`, and `Refresh` build CLI arguments directly in `src/Terraform.cs`.
3. Variables are written to `execute.tfvars.json` when needed.
4. Backend settings are added through backend arguments plus a generated `_backend.tf`.
5. Provider bindings are rewritten into `providers.auto.tf.json` inside the execution root before Terraform runs.
6. Results come back as `TFResult`.

## Provider Alias Model (Current)
- Providers are stored in `ProviderCollection` under `(alias, provider)` keys.
- `ProviderConfigurationRewriter` scans the root execution workspace for provider blocks in `.tf` and `.tf.json` files.
- Extracted provider settings are merged with bound provider values from `ProviderCollection`.
- The final resolved provider configuration is written to `providers.auto.tf.json`.
- Original provider blocks are removed from the copied root files so the generated file is the single source of truth for that run.

## Current Limitations
- The HCL extraction path is intentionally focused on provider blocks and common provider-setting expressions. If a provider block uses unsupported HCL constructs, the runtime should fail loudly rather than silently misrewrite it.
- Tests currently cover the rewrite/merge flow directly, but not a full Terraform integration run with multiple real aliased providers.

## Direction For Provider Mapping Work
- Extend test coverage for more provider block shapes, especially nested/repeated blocks and expression-heavy settings.
- Decide whether provider rewrite should expose diagnostics or dry-run inspection output for callers.

## Key Invariants
- Never mutate the caller's source Terraform files in place; only rewrite the copied execution workspace.
- Prefer explicit failures over silent fallback behavior.
- Keep provider binding logic deterministic so the generated runtime config is the single source of truth for that run.

## Outstanding Follow-Up
- Decide whether package dependency upgrades should be done before or after the provider mapping work.
- Add an end-to-end Terraform integration test for aliased providers once a stable sample configuration is in place.

## Technical Debt
- `src/Terraform.cs` assembles command arguments inline; command construction is not yet modeled as strongly typed command objects.
- Test coverage is still fairly thin on `main`, even after the provider rewrite unit tests.
- The provider rewriter currently hand-parses HCL instead of using a dedicated parser library.
