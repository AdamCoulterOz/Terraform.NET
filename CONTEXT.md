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
5. Provider credentials/config are injected as environment variables before executing Terraform.
6. Results come back as `TFResult`.

## Provider Alias Model (Current)
- Providers are stored in `ProviderCollection` under `(alias, provider)` keys.
- `CombinedProviderConfigs` currently merges only the provider environment variables themselves and does not encode the alias into the emitted keys.
- That means aliased instances of the same provider cannot safely coexist when they need different credentials or settings, because the environment variables collide before Terraform runs.

## Important Limitation
The current runtime assumes provider configuration can be expressed through process environment variables. That works for single-provider cases, but it is the wrong mechanism for per-alias provider binding. Terraform resolves provider aliases from configuration, not from separate environment-variable namespaces.

## Direction For Provider Mapping Work
The next change should happen only inside the isolated temporary execution workspace:
- inspect the target Terraform root for provider parameter blocks
- extract their names, types, aliases, and current settings
- remove those in-workspace definitions from the copied configuration
- apply bound provider alias settings over the extracted defaults
- preserve existing settings where bindings do not override them
- write the resolved provider configuration to `providers.auto.tf.json`

## Key Invariants
- Never mutate the caller's source Terraform files in place; only rewrite the copied execution workspace.
- Prefer explicit failures over silent fallback behavior.
- Keep provider binding logic deterministic so the generated runtime config is the single source of truth for that run.

## Outstanding Follow-Up
- Decide whether package dependency upgrades should be done before or after the provider mapping work.
- Add tests that cover aliased providers with different bound settings.

## Technical Debt
- `src/Terraform.cs` assembles command arguments inline; command construction is not yet modeled as strongly typed command objects.
- Test coverage is very thin on `main`; only a minimal provisioning lifecycle test currently runs.
- Provider alias support is structurally incomplete until runtime-generated provider configuration replaces env-only injection for alias-specific settings.
