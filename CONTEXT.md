# Terraform.NET Context

Last updated: 2026-04-21 (Australia/Sydney)

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
- Terraform value system: `src/TF/TFValue.cs`
- Terraform type-constraint system: `src/TF/TFType.cs`
- CLI configuration file writer: `src/TF/Configuration.cs`
- Typed command result and projection models:
  - `src/TF/CommandJsonResult.cs`
  - `src/TF/OperationResult.cs`
  - `src/TF/CommandResponse.cs`
  - `src/TF/ShowResult.cs`
  - `src/TF/ValidateResult.cs`
  - `src/TF/Model/Plan.cs`
- Built-in providers/backends:
  - `src/TF/BuiltIn/*`
  - `src/TF.Azure/*`
  - `src/TF.MySql/*`
- Unit tests:
  - `test/ProvisioningTests.cs`
  - `test/TerraformCommandResultTests.cs`
  - `test/TFValueTests.cs`

## Command Execution Flow
1. A `Terraform` instance is created with a backend, working directory, and Terraform executable path.
2. `Init`, `Validate`, `Plan`, `Apply`, `Destroy`, `Refresh`, and `Show` build CLI arguments directly in `src/TF/Terraform.cs`.
3. Variables are written to `execute.tfvars.json` when needed, using the shared `TFValue` JSON converter for Terraform-native serialization.
4. Backend settings are added through backend arguments plus a generated `_backend.tf`.
5. Provider bindings are rewritten into `providers.auto.tf.json` inside the execution root before Terraform runs.
6. Raw command execution returns an internal `TFResult`, and `Command<TResult>` maps JSON output into typed command result models.
7. JSONL UI output from commands such as `init`, `plan`, `apply`, `destroy`, and `refresh` is parsed into a canonical internal command response carrying:
   - init status and provider install state
   - resource drift and planned change records
   - outputs and resource keys as `TFValue`
   - resource apply/provision/refresh/ephemeral operation records
   - diagnostics and change summaries
   - enums for operation/action/severity discriminators and `TimeSpan` for elapsed durations
8. The canonical command response is then transformed into operation-specific result objects (`InitResult`, `PlanResult`, `ApplyResult`, `DestroyResult`, `RefreshResult`).
9. `Plan()` writes a managed scratch plan file inside the execution root.
10. `Show()` consumes that managed plan file and returns a composite result:
   - `Json`: the `terraform show -json` document as a `TFObject`-backed `ShowJsonResult`
   - `File`: the human-readable `terraform show` text as a `ShowFileResult`
11. `Show<TResult>()` still exists for callers that want to deserialize only the JSON form directly into a custom type.

## Terraform Value Model
- Core now uses a shared `TFValue` hierarchy instead of loose `object`, `dynamic`, `JsonElement`, and `JsonValue` payloads where practical.
- `TFValue` covers:
  - closed scalar wrapper types (`TFString`, `TFBool`, `TFNumber`)
  - structured values (`TFObject`, `TFArray`)
  - null (`TFNull`)
  - HCL expression strings (`TFExpression`)
- `TFValue<T>` is the generic base shape, but scalar instances are intentionally limited to those concrete Terraform-shaped wrappers rather than an open generic scalar type.
- The runtime model intentionally treats:
  - list and tuple as the same in-memory sequence shape (`TFArray`)
  - map and object as the same in-memory keyed shape (`TFObject`)
- `TFType` separately models Terraform type constraints and serialized type metadata:
  - primitives: `string`, `number`, `bool`, `any`
  - collection types: `list(...)`, `map(...)`, `set(...)`
  - structural types: `tuple([...])`, `object({...})`
- `TFType` parses both Terraform source-style constraints like `list(string)` and Terraform JSON type serialization like `["object",{"a":"number"}]`.
- `TFType` is now used for plan variable types and command-response output types.
- `TFValue` is JSON-convertible in both directions, so the same type is used for:
  - `Variables`
  - provider binding/config rewrite
  - command-response output values and resource keys
  - remaining plan/show model payloads in `src/TF/Model/*`
- The value model is intended to be the in-memory representation. `JsonNode`/`JsonObject` now exist only at the JSON file boundary in the provider rewriter and in the converter internals.

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
- `src/TF/Model/*` is still only lightly exercised. In particular, `src/TF/Model/Plan.cs` does not currently line up with the full current `terraform show -json` plan document shape and should not be treated as the canonical show model yet.

## Direction For Provider Mapping Work
- Extend test coverage for more provider block shapes, especially nested/repeated blocks and expression-heavy settings.
- Decide whether provider rewrite should expose diagnostics or dry-run inspection output for callers.

## Key Invariants
- Never mutate the caller's source Terraform files in place; only rewrite the copied execution workspace.
- Prefer explicit failures over silent fallback behavior.
- Keep provider binding logic deterministic so the generated runtime config is the single source of truth for that run.
- Keep Terraform scratch artifacts such as the managed plan file hidden behind the `Terraform` API rather than exposing file paths to callers.
- Keep raw Terraform event-stream details behind canonical parsing and transformed command results; callers should consume logical operation results rather than JSONL message envelopes.
- When a Terraform value needs to survive beyond the raw JSON/HCL boundary, represent it as `TFValue`, not `object`, `dynamic`, or `JsonElement`.
- When Terraform type information needs to survive beyond the raw JSON boundary, represent it as `TFType`, not raw strings or ad hoc JSON arrays.

## Outstanding Follow-Up
- Decide whether package dependency upgrades should be done before or after the provider mapping work.
- Add an end-to-end Terraform integration test for aliased providers once a stable sample configuration is in place.
- Decide whether the new Azure backend assembly should become a separately published package or stay as a solution-level assembly only.
- Decide whether the old `src/TF/Model/*` plan/show-related classes should be updated to match the current Terraform JSON schema and folded into `ShowResult`, or removed if the raw `TFValue` document surface is the intended public API.
- Decide whether `TFType` should eventually also drive validation/coercion helpers for `Variables` and other user-supplied input.

## Technical Debt
- `src/TF/Terraform.cs` assembles command arguments inline; command construction is not yet modeled as strongly typed command objects.
- Test coverage is still fairly thin on `main`, even after the provider rewrite unit tests.
- The provider rewriter currently hand-parses HCL instead of using a dedicated parser library.
- Packaging and release flow still assume a single main package; the new provider/backend assemblies are solution-level splits but not yet a finalized multi-package publishing model.
- `TFValue` currently models arrays/objects/scalars/null cleanly, but higher-order Terraform-specific states such as unknown values or first-class expression ASTs are not yet modeled beyond `TFExpression` string preservation.
- `TFType` currently models Terraform constraints and output type metadata, but it is not yet integrated into value validation or conversion rules beyond parsing/serialization.
