# C-PHASE 71 Approved Local File/Tool Access

Generated: 2026-05-12

## Goal

Add a default-deny unified policy layer for local file reads and local tool execution previews so Wevito can move toward supervised local autonomy without hidden file access, hidden command execution, or bypassing the kill switch.

## Scope

Implemented:

- `PolicyDecision` as the shared policy result shape.
- `LocalToolAccessPolicy` for approved roots, denylisted paths, traversal blocking, symlink/reparse blocking, and sha256-gated tool script previews.
- `UnifiedPolicyService` as the central gate for file reads, local tool execution preview, tool policy decisions, capture policy decisions, proof command allowlist decisions, and helper capability compatibility.
- `LocalToolExecutionPreviewAdapter` for dry-run-only PET TASKS local tool execution preview packets.
- Dispatcher routing for `localToolExec`.
- Unified read checks in `LocalDocsPreviewAdapter`, `AssetInventoryPreviewAdapter`, `CodeReviewPreviewAdapter`, and `LocalResearchPreviewAdapter`.
- Compatibility delegation from `HelperAllowlistEvaluator`, `ToolPolicyEvaluator`, `CapturePolicyEvaluator`, and `ProofExecutionAllowlistEvaluator`.
- Tool definition metadata for `local_tool_exec`.

## Safety Boundaries

- `localToolExec` defaults blocked through `local_tool_exec_enabled=false`.
- Even when enabled, C-PHASE 71 only writes a dry-run preview packet. It never starts a process or runs a script.
- Scripts must be under `tools/`, must be PowerShell scripts, and must match a sha256 allowlist entry.
- Denylist beats allowlist for `.git`, `secrets`, `.env*`, `credentials`, `%USERPROFILE%/.ssh`, `%LOCALAPPDATA%/Microsoft/`, and `%APPDATA%/Microsoft/`.
- Parent traversal using `..`, case-folded denylist traversal, and symlink/reparse-point paths are blocked.
- `UnifiedPolicyService` writes audit-ledger rows when an audit ledger is supplied.
- `KillSwitchService` blocks read and local-tool policy evaluations when active.
- No sprites, runtime PNGs, source boards, prop anchors, hosted-AI paths, training paths, or live network paths were changed.

## Validation

Passed:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Policy|Allow|Access|LocalTool"`  
  Result: 68/68 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`  
  Result: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`  
  Result: 366/366 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`  
  Result: passed. Broker and shell published under `vnext/artifacts/`.

## Next Phase

C-PHASE 72 remains a stop-and-ask phase before local model runtime integration. It should keep the local-first posture: localhost-only runtime probing, safe deterministic fallback when absent, no hosted AI, no credential reads, and kill-switch blocking.
