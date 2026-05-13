# bge-micro-v2 Local Embedder

This folder is the opt-in home for Wevito's local embedding model.

Wevito does not commit model weights and does not download them silently. Use
`tools/install-local-embedder.ps1` with an explicit `-ModelUrl` and
`-ExpectedSha256` when you are ready to install a local ONNX embedder.

Expected local files after install:

- `model.onnx`
- `tokenizer.json`
- `manifest.json`

If the model or tokenizer is missing, Wevito degrades to the deterministic
hashing embedder and records one safe-degrade audit line.
