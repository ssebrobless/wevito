# Test Embedder Fixture

This folder intentionally does not contain a real model. C-PHASE 76 tests use
the tiny files here to prove local-path, tokenizer, cache, fallback, and
normalization behavior without downloading model weights or opening the network.

- `model.onnx` is a placeholder byte file used only with injected fake backends.
- `tokenizer.json` contains a minimal BERT-style vocab used for tokenizer-read
  validation.
