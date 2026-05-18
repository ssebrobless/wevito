# C-PHASE 140 - Tool Hub Simplification And Reliability Pass

## Goal

Make the Tool Hub easier to understand without removing capabilities. The primary row now keeps common work visible, moves specialist surfaces behind an **Advanced** toggle, and uses a shared catalog for tab labels and tool-family descriptions.

## Scope

Changed:

- `vnext/src/Wevito.VNext.Core/Tools/ToolCatalog.cs`
- `vnext/src/Wevito.VNext.Core/ToolRegistry.cs`
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/tests/Wevito.VNext.Tests/ToolCatalogTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ToolPopupWindowActionTextTests.cs`
- `vnext/artifacts/c-phase-140-tool-hub/inventory.json`

Not changed:

- No model calls.
- No sprite or asset mutation.
- No command identifiers removed.
- No audit packet kind renamed.
- No capability flag default changed except the new `tool_hub_advanced_visible=false`.

## Implemented

- Added `ToolCatalog` as the single reviewable source for Tool Hub tab names, automation IDs, plain-language descriptions, risk level, and approval requirement metadata.
- Rewired `ToolRegistry.BuildDefaultDescriptors` to use `ToolCatalog.ToolFamilies`, preserving the existing tool-family IDs and execution behavior.
- Simplified the top tab strip from `Chat / Activity / Agents / Tools / Benchmarks / Autonomy / Local Docs / Creative Lab / Settings` to primary tabs:
  `Pets / Tasks / Tools / Local AI / Autonomy / Local Docs`.
- Moved `Activity`, `Benchmarks`, and `Creative Lab` behind the `Advanced` toggle. The commands still route to the same `toolId` values.
- Added catalog descriptions to the Tool Registry checkbox list so users can see what each AI-callable tool family does.
- Added `tool_hub_layout_changed` to `PlainLanguageExplainer.KnownPacketKinds`.
- Added a one-time startup audit packet for the simplified Tool Hub layout unless Stop Everything is active.
- Wrote `vnext/artifacts/c-phase-140-tool-hub/inventory.json` with tab/control inventory, descriptions, relative source paths, and last-touched commit references.

## Safety Boundaries

- The Advanced toggle only hides specialist tabs by default; it does not remove or disable the underlying command routes.
- The layout inventory uses repo-relative paths only and does not include user PII or machine-local absolute paths.
- The new audit packet reports layout metadata only and sets network, hosted-AI, local-model, and mutation flags to false.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ToolCatalog|ToolPopupWindow|PlainLanguage"`: passed, 226/226.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1044/1044.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with line-ending normalization warnings only.

## Stop-Gate Check

- UIAutomation reachability did not intentionally lose any prior command route; `actions`, `helpers`, `basket`, `settings`, `autonomous-scopes`, `local-docs`, `activity`, `benchmarks`, and `creative-lab` remain cataloged.
- Tool catalog drift is covered by tests against the inventory artifact and registry descriptors.
- The inventory artifact contains only repo-relative paths.

## Next Phase

C-PHASE 140 is Auto-continue=No. Stop after the PR is open for user review.
