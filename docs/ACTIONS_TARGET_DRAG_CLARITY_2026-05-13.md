# ACTIONS Target And Drag Clarity - 2026-05-13

## Goal

Make the ACTIONS surface less confusing while keeping the existing safe action pipeline.

## Implemented

- ACTION option rows now name the active target pet instead of showing a generic `USE` button.
- ACTION summaries now explicitly say that items can be dragged and dropped onto the pet or applied with the target button.
- ACTION option rows start a WPF drag payload containing the exact `actionId|itemId` pair.
- The focused home stage accepts that action payload and routes it through the same `ApplyActionSelectionAsync` path as the button click.

## Safety Boundaries

- No sprite or asset PNGs were mutated.
- No save reset was performed.
- No hosted AI, web, model, or training path was touched.
- Dropping an ACTION item does not create a new execution path; it calls the same validated action-selection handler.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ToolPopupWindowActionTextTests|HomePanelWindowRenderingTests|ShellPresentationRulesTests"`

## Remaining Work

- Full object-following drag visuals are still a later polish layer. This phase makes drag/drop functionally real and target-clear without adding animated dragged item sprites.
- Broader visual sprite cleanup remains separate from ACTIONS UX.
