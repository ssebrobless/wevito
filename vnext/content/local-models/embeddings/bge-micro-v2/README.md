# bge-micro-v2 Local Embedder

This folder is the opt-in home for Wevito's local embedding model.

Wevito does not commit model weights and does not download them silently. Use
`tools/install-local-embedder.ps1` with an explicit `-ModelUrl` and
`-ExpectedSha256` when you are ready to install a local ONNX embedder.

Expected local files after install:

- `model.onnx`
- `tokenizer.json`
- `vocab.txt` (optional when `tokenizer.json` includes a readable `vocab`)
- `manifest.json`

If the model or tokenizer is missing, Wevito degrades to the deterministic
hashing embedder and records one safe-degrade audit line.

Install example:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\install-local-embedder.ps1 `
  -ModelUrl "<approved-model.onnx-url>" `
  -ExpectedSha256 "<model-sha256>" `
  -TokenizerJsonUrl "<approved-tokenizer.json-url>" `
  -ExpectedTokenizerJsonSha256 "<tokenizer-json-sha256>" `
  -VocabUrl "<approved-vocab.txt-url>" `
  -ExpectedVocabSha256 "<vocab-sha256>"
```

The installer never downloads silently. It requires the user to type `INSTALL`
and verifies every downloaded file with SHA-256 before moving it into this
folder.
