# Wevito Soak Driver CLI

Internal C-PHASE 85b helper used by `tools/run-soak-driver.ps1` and `tools/check-evidence-readiness.ps1`.

Commands:

- `status`
- `heartbeat --reason scheduled`
- `day-end`
- `window-end --reason completed`

The CLI is local-only. It does not open network connections, flip settings, call hosted AI, train models, or mutate assets.
