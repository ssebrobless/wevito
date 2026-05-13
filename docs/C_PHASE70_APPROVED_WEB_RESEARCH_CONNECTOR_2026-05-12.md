# C-PHASE 70: Approved Web Research Connector

## Goal

Add the first network-capable research surface while keeping Wevito local-first by default.

## Scope

- Added a `WebResearchConnector` that requires approved task cards, runtime supervisor permission, rate-limit checks, privacy filtering, and kill-switch clearance before any network backend can fetch.
- Added an offline default backend that performs no network work.
- Added backend shells for Brave, Tavily, and Firecrawl using the credential target contract `Wevito/web-search/<backend>`.
- Added connector-level cache records under `%LOCALAPPDATA%/Wevito/web-cache/<yyyymmdd>/`.
- Added web fetch evidence packets under `vnext/artifacts/pet-tasks/<timestamp>-web-research/`.
- Added a simple Settings web research panel: disabled by default, offline backend by default.
- Extended local research evidence to accept fetched web records as source records without using hosted AI.

## Implemented

- `vnext/src/Wevito.VNext.Core/WebResearchConnector.cs`
- `vnext/src/Wevito.VNext.Core/IWebSearchBackend.cs`
- `vnext/src/Wevito.VNext.Core/OfflineWebSearchBackend.cs`
- `vnext/src/Wevito.VNext.Core/HttpWebSearchBackends.cs`
- `vnext/src/Wevito.VNext.Core/WebFetchRecord.cs`
- `vnext/src/Wevito.VNext.Core/WebQueryPrivacyFilter.cs`
- `vnext/src/Wevito.VNext.Core/WindowsCredentialStore.cs`
- `vnext/tests/Wevito.VNext.Tests/WebResearchConnectorTests.cs`
- `vnext/tests/Wevito.VNext.Tests/WebQueryPrivacyFilterTests.cs`

## Safety Boundaries

- Default backend is `offline`.
- `web_search_enabled=false` by default.
- No hosted AI is called.
- No test makes a live network call.
- Privacy filter strips Windows paths, env vars, email addresses, and credential-shaped strings.
- KillSwitch blocks fetches.
- Quiet/PetOnly/fullscreen-quiet runtime state blocks fetches.
- Live backends require an approved task card and credential target.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "WebResearch|WebQueryPrivacy"`
- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

## Next Phase

C-PHASE 71 adds approved local file/tool access and a unified policy engine. It should remain stopped for user approval because it broadens local access.
