# Terraform.NET History

## 2026-06-18: Fractional Terraform UI elapsed seconds parsed
Terraform JSON UI hook payloads can report `elapsed_seconds` as a fractional JSON number. The command-response parser now treats elapsed seconds as a numeric value instead of an integer so successful apply streams with sub-second precision do not fail result projection.

## 2026-06-08: Azure DevOps Service-Principal Credential Added
`Terraform.NET.AzureDevOps` added an Azure DevOps provider credential for Entra service-principal client-secret authentication. This keeps Azure DevOps provider auth field names in the Azure DevOps package while allowing SCH local harness runs to use the same generated pipeline identity secret as Azure and Fabric providers instead of requiring a user PAT.

## 2026-05-14: Preview Package Family and Consumer Pinning
`Terraform.NET` moved into a multi-package preview line for the core package plus Azure, Azure DevOps, Fabric, and Power Platform provider/auth extensions. SCH consumers needed explicit prerelease constraints because older stable package versions can still participate in NuGet restore resolution.

## 2026-05-16: Preview.6 Consumer Floor
`1.0.0-preview.6` remained the current SCH consumer minimum while Terraform.NET prepared a new package version for parser/interface changes. Main must not carry code that differs from an already-published immutable NuGet version.

## 2026-05-16: Preview.7 Typed Parsing/Auth Boundaries
`1.0.0-preview.7` carries the Terraform JSON UI stream parsing API through `TerraformCommandResponseParser.ParseJsonUiStream(...)`, so consumers such as Platform no longer need to depend on parser behavior only indirectly exercised through `Plan()`.

The provider auth boundary now exposes `IEntraCredential` and `IEntraOidcCredential` as shared semantic interfaces while leaving provider-specific Terraform environment variable names in each provider package. External `.tfvars.json` loading remains intentionally out of scope unless variables are redesigned as CLR-owned inputs rather than exposed CLI file mechanics.
