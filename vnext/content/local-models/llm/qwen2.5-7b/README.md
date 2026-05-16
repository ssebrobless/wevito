# Qwen 2.5 7B Local Reasoning Model

Decision IDs: `Q15-L2`, `I-Reframe`

Wevito's default reasoning LLM is `qwen2.5:7b-instruct-q4_K_M` served by Ollama on `http://127.0.0.1:11434`.

This folder is documentation only. Wevito does not store model weights here and Codex/test runs must not download weights automatically.

## Why This Model

- It is a local-first reasoning model suitable for a Windows desktop helper.
- It keeps normal Wevito runtime in `LocalOnly` mode with no hosted-AI dependency.
- It can degrade safely: if Ollama or the model is missing, Wevito uses deterministic local fallback behavior.

## User Setup

1. Install Ollama manually from `https://ollama.com/download`.
2. From the repo root, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\pull-default-model.ps1
```

3. Restart Wevito. The startup bootstrap probe will record whether the runtime/model is ready.

## Safety Contract

- No hosted-AI provider is called by this default.
- No model weights are downloaded by tests or app startup.
- Pulling the model requires an explicit user action.
- Evidence packets must mark localhost probing as not hosted AI, not file mutation, and not local model inference.
