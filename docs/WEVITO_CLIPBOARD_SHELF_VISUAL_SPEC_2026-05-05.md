# Wevito Clipboard Shelf Visual Spec

Date: 2026-05-05

Purpose: define the visual/product behavior for a future Clipboard Shelf webtool that fits Wevito's overlay-first desktop-pet experience.

This is a visual-side planning document. It does not authorize code implementation, passive clipboard monitoring, browser automation, data upload, PET TASKS execution, or storage of sensitive user data without explicit user action.

## Product Role

The Clipboard Shelf is the second practical webtool after Link Bin.

```text
Wevito TOOLS
  |
  +-- Link Bin
  |     +-- URLs the user explicitly captures
  |
  +-- Clipboard Shelf
  |     +-- snippets the user explicitly saves
  |
  +-- PET TASKS
  |     +-- report-only helper tasks
  |
  +-- future slots
        +-- proof summaries
        +-- visual review queues
```

It should feel like a little shelf or basket the pet can guard, not like a surveillance log.

## Core Rule

Clipboard Shelf saves only what the user explicitly asks it to save.

```text
allowed
  |
  +-- user clicks SAVE CLIP
  +-- user pastes text into shelf input
  +-- user drags/drops text or file shortcut into shelf
  +-- user deletes/clears saved snippets

blocked
  |
  +-- passive clipboard scraping
  +-- hidden clipboard history
  +-- background upload
  +-- saving passwords/tokens automatically
  +-- transmitting saved snippets without approval
```

## Target UI

```text
┌─────────────────────────────────────────────┐
│ CLIPBOARD SHELF                 MANUAL SAVE │
├─────────────────────────────────────────────┤
│ [ paste or drag snippet here...        SAVE]│
├─────────────────────────────────────────────┤
│ 3 saved                                      │
│                                             │
│ ┌ Note from docs                     COPY ┐ │
│ │ "PET TASKS remains report-only..." DEL  │ │
│ │ today 12:44 PM · manual                 │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ ┌ Sprite path                       COPY ┐ │
│ │ C:\Users\...\goose\baby\...       DEL  │ │
│ │ today 12:45 PM · path                  │ │
│ └─────────────────────────────────────────┘ │
├─────────────────────────────────────────────┤
│ CLEAR ALL                        REPORT ONLY│
└─────────────────────────────────────────────┘
```

## Shelf Item Fields

| Field | Required | Notes |
| --- | --- | --- |
| `id` | yes | Stable local id. |
| `title` | yes | User-provided or generated from first line. |
| `kind` | yes | `text`, `path`, `url`, `code`, `note`. |
| `preview` | yes | Short display text. |
| `full_text` | yes | Stored locally if user saved it. |
| `source_label` | no | Optional app/doc/source name. |
| `created_at` | yes | Local timestamp. |
| `tags` | no | Optional lightweight labels. |
| `sensitive_flag` | no | UI warning only; do not infer too aggressively. |

## Visual States

| State | UI treatment | Pet expression |
| --- | --- | --- |
| Empty | calm empty shelf with one CTA | pet idle/waiting |
| Has items | count badge and compact list | pet sits near shelf |
| New save | short highlight on item row | pet happy/jump if not busy |
| Sensitive-looking text | warning chip, manual confirmation | pet cautious/waiting |
| Copied | small success chip | pet happy |
| Deleted | row collapses, undo optional later | pet neutral |
| Full shelf | soft warning, ask user to clear | pet waiting |

## Overlay Placement

The Clipboard Shelf should live inside the existing TOOLS surface, not on the idle overlay by default.

```text
overlay HUD
  |
  +-- TOOLS
        |
        +-- LINK BIN
        +-- CLIPBOARD
        +-- PET TASKS
```

Recommended compact button:

- label: `CLIP`
- icon concept: small shelf, note, or clipboard icon
- status badge: item count

## Privacy And Safety

Required visible text:

```text
Manual save only.
```

Required behavior:

- never save clipboard changes automatically
- never save after every copy event
- never transmit saved snippets from this tool
- warn before saving text that resembles secrets
- include `CLEAR ALL`
- include per-item delete
- store locally only unless code-side later defines explicit sync/export

Sensitive-looking examples:

- API keys
- passwords
- tokens
- private keys
- credit cards
- SSNs
- one-time auth codes

If detected, the first version should simply warn:

```text
This looks sensitive. Save anyway?
```

Do not block user agency permanently, but make the risk visible.

## Relationship To PET TASKS

PET TASKS can reference Clipboard Shelf only in report-only ways at first.

Allowed later examples:

```text
Bean, summarize the saved notes
Pip, find sprite paths in my shelf
Nix, turn these notes into a task draft
```

Blocked:

```text
send this snippet
paste this into a website
upload all shelf items
run this command
```

## Relationship To Link Bin

Link Bin remains URL-focused.

Clipboard Shelf handles:

- snippets
- paths
- notes
- copied code fragments
- short manual text saves

If the saved item is a URL, the UI can suggest Link Bin:

```text
Looks like a URL. Save to Link Bin instead?
```

## First Implementation Slice For Code-Side

When code-side takes this up, the smallest safe slice is:

1. Add an empty Clipboard Shelf tab/button in TOOLS.
2. Add manual text entry and `SAVE`.
3. Store local snippets in the same local app-data style as other vNext state.
4. Show saved snippets in a compact list.
5. Add `COPY`, `DELETE`, and `CLEAR ALL`.
6. Add visible `Manual save only` copy.
7. Add tests/probe proving no passive clipboard capture occurs.

Do not include PET TASKS integration in the first slice.

## Visual Acceptance Checklist

- The user can tell it is manual-save only.
- Empty state is quiet and not scary.
- Saved snippets are readable but compact.
- Item count is visible from TOOLS.
- Copy/delete controls are obvious.
- No saved content appears on the idle overlay unless the user opens the shelf.
- Sensitive-looking text gets a warning before save.
- No UI suggests background monitoring.

