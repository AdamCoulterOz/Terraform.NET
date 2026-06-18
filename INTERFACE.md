# Terraform.NET Interface

## Purpose
`Terraform.NET` is a .NET package family that wraps Terraform CLI execution behind typed CLR configuration, provider binding, variable, command-result, plan-review, and Terraform JSON helper APIs.

## Responsibilities
- Own the generic Terraform execution boundary for .NET callers.
- Own Terraform value and type representations through `TFValue` and `TFType`.
- Own provider binding mechanics and generated provider configuration for the execution workspace.
- Own generic Terraform JSON document helpers and plan-review helpers.
- Own provider-auth extension packages for the currently supported Azure, Azure DevOps, Fabric, and Power Platform providers.
- Do not own consumer domain projections, protected-resource policies, platform deployment lifecycles, or cross-repository orchestration.

## Domain Model
- `Terraform` represents a configured Terraform command runner over a root workspace.
- `Backend`, `Provider`, `ProviderCollection`, and `Credential` represent Terraform runtime binding inputs.
- `Variables`, `TFValue`, and `TFType` represent CLR-owned Terraform inputs and JSON/type payloads.
- `CommandResponse`, `PlanResult`, `ApplyResult`, `DestroyResult`, `RefreshResult`, `InitResult`, `ValidateResult`, `ShowResult`, and `OutputValue` represent typed Terraform command outputs.
- `TerraformPlanReview` represents generic review records projected from `terraform show -json`.

## Public Interfaces
- NuGet packages:
  - `Terraform.NET`
  - `Terraform.NET.Azure`
  - `Terraform.NET.AzureDevOps`
  - `Terraform.NET.Fabric`
  - `Terraform.NET.PowerPlatform`
- Current package release line is `1.0.0-preview.7`; current SCH consumer minimum remains `1.0.0-preview.6` until SCH consumers intentionally move forward.
- `Terraform` exposes `Init`, `Validate`, `Plan`, `Show`, `Apply`, `ApplyPlan`, `Destroy`, `Refresh`, and `Output`.
- `Terraform.ForExternalWorkingDirectory(...)` allows callers to use a caller-owned working directory without disposal deleting it.
- `TerraformCommandResponseParser.ParseJsonUiStream(...)` exposes typed parsing for Terraform JSON UI streams without requiring callers to execute `Plan()`.
- `TerraformJsonDocument`, `TerraformLabel`, `TFValue`, and `TFType` are public composition/value helpers.
- `IEntraCredential` and `IEntraOidcCredential` are the shared semantic auth interfaces for Entra-backed provider credentials.
- `Terraform.NET.AzureDevOps` exposes PAT and service-principal client-secret credentials for the Microsoft Azure DevOps Terraform provider; the provider package owns the Terraform field and environment names for both modes.

## Invariants
- Do not mutate caller source Terraform files in place; provider rewriting occurs in the execution workspace.
- Prefer loud failures over silent fallback or best-effort parsing.
- Keep Terraform scratch plan files hidden behind `Terraform` APIs.
- Keep provider-specific Terraform field and environment variable names in their owning provider packages.
- Keep Terraform variables CLR-owned. External `.tfvars.json` file loading is not a public contract unless redesigned as CLR-owned variables.

## Side Effects
- Writes generated files such as `_backend.tf`, `providers.auto.tf.json`, `execute.tfvars.json`, and the managed plan file inside the execution workspace.
- Executes the configured Terraform CLI process.
- May set process environment variables for provider, backend, and CLI configuration.
- Deletes owned temporary execution roots and managed plan files according to the `Terraform` lifecycle.
- Does not persist remote state itself; Terraform and the configured backend own remote state effects.

## Dependency Boundaries
- Upstream dependencies include Terraform CLI, `CliWrap`, `System.Text.Json`, provider package dependencies, and provider-specific SDKs where needed.
- Downstream consumers may depend on public package APIs and documented result/value/provider/auth contracts.
- Downstream consumers must not depend on generated file names except where explicitly documented as side effects.
- Downstream consumers must not depend on undocumented parsing internals or provider rewriter implementation details.

## Lifecycle / Execution Model
- A `Terraform` instance owns command construction, workspace rewrite, command execution, and result projection for its root directory.
- `Plan()` creates a managed plan file; `Show(deleteManagedPlan: false)` preserves it for `ApplyPlan()`.
- `ApplyPlan()` applies the existing managed plan and then removes it.
- `Dispose()` deletes only roots owned by the `Terraform` instance.
- Command execution is process-based and asynchronous; shared mutable state on a single `Terraform` instance is not a concurrency contract.

## Anti-Goals
- No hidden loading of external `.tfvars.json` files.
- No consumer-domain policy decisions inside generic plan parsing.
- No provider-specific auth environment naming in shared Entra/OIDC abstractions.
- No replacement for Terraform provider/resource schemas.
- No direct ownership of Platform, SCH, or other consumer deployment lifecycle semantics.

## Agent Guidance
- Preserve the typed CLR boundary; avoid adding raw Terraform CLI switches as the primary consumer API when a CLR-owned concept is the real contract.
- Update this file when public APIs, package contracts, result models, side effects, auth abstractions, or lifecycle rules change.
- Keep `CONTEXT.md` current-state only and put durable historical changes in `HISTORY.md`.
- Validate with `dotnet test Terraform.NET.slnx --configuration Release` after code changes.
