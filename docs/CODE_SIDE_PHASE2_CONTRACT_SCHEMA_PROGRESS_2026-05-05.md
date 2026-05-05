# Code-Side Phase 2 Contract Schema Progress

Date: 2026-05-05

Scope: additive vNext contract/schema foundation for pet helpers, task cards, tool policy, learning feedback, sprite row manifests, candidate manifests, and apply/proof manifests.

No runtime PNGs, source boards, Godot scripts, sprite assets, browser automation, or production apply paths were changed.

## Shape

```text
User command text
  │
  ▼
TaskIntent
  │
  ├── named/selected/best-helper routing
  ├── task kind
  ├── requested tool family
  ├── target paths/assets
  ├── risk level
  └── approval flag
  │
  ▼
TaskCard
  │
  ├── assigned pet snapshot
  ├── policy snapshot
  ├── task status
  ├── timeline
  └── audit/result paths
```

```text
Visual candidate
  │
  ├── SpriteRowKey
  ├── AnimationCandidateManifest
  ├── validation/proof paths
  └── runtime prop overlay flag
  │
  ▼
ApplyProofManifest
  │
  ├── backup-before-apply hashes
  ├── after-apply hashes
  ├── proof surfaces
  ├── dry-run report
  └── rollback procedure
```

## Files Added

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\SpriteWorkflowContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\SpriteWorkflowContractTests.cs`

## Contract Additions

Pet helper and task contracts:

- `PetHelperProfile`
- `ActiveHelperRoster`
- `PetAgentContractLimits.MaxActiveHelpers = 3`
- `TaskIntent`
- `TaskCard`
- `ToolDescriptor`
- `ToolPolicy`
- `ReviewedExample`
- `LearningFeedback`

Sprite workflow and apply/proof contracts:

- `SpriteRowKey`
- `SpriteFrameGeometry`
- `SpriteFrameManifest`
- `SpriteRowManifest`
- `AnimationCandidateManifest`
- `ApplyProofManifest`

Important boundary encoded:

- Pet helper role and pet name routing do not grant tool permission.
- Tool permission lives in `ToolPolicy`.
- Runtime prop overlays are explicitly modeled with `AnimationCandidateManifest.UsesRuntimePropOverlay`.
- Apply/proof requires backup-before-apply hashes and proof-surface metadata.

## Validation

Focused new tests:

```powershell
dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~PetAgentContractTests|FullyQualifiedName~SpriteWorkflowContractTests"
```

Result: passed `7 / 7`.

Build:

```powershell
dotnet build vnext\Wevito.VNext.sln
```

Result: passed with `0` warnings and `0` errors.

Full vNext tests:

```powershell
dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `33 / 33`.

Note: an initial parallel build/test attempt produced a transient DLL file-lock on `Wevito.VNext.Contracts.dll`. Rerunning the build by itself passed cleanly.

## Audit

```text
Phase audit
  ├── additive only: yes
  ├── no sprite/runtime mutation: yes
  ├── pet personality separate from tool policy: yes
  ├── three-helper model represented: yes
  ├── task-card schema represented: yes
  ├── sprite candidate/apply proof schema represented: yes
  ├── tests added: yes
  └── vNext build/tests green: yes
```

## Next Safe Phase

Implement the first deterministic parser/service layer around these contracts:

- parse explicit `@PetName` and `PetName, ...` addressing into `TaskIntent`
- route unaddressed commands to selected pet or best-helper suggestion
- produce draft/blocked task cards without executing tools
- keep all execution behind future Tool Broker policy checks

Still paused:

- sprite generation
- runtime/source PNG mutation
- broad runtime asset normalization
- autonomous tool execution

