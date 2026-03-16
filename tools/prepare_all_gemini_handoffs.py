#!/usr/bin/env python3
"""
Prepare Gemini handoff folders for every species in the incoming pose manifest.
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MANIFEST_PATH = ROOT / "tools" / "incoming_animal_pose_manifest.json"
PREPARE_SCRIPT = ROOT / "tools" / "prepare_gemini_handoff.py"


def load_species() -> list[str]:
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    seen: list[str] = []
    for entry in manifest:
        species = entry["species"]
        if species not in seen:
            seen.append(species)
    return seen


def main() -> None:
    for species in load_species():
        subprocess.run(
            [sys.executable, str(PREPARE_SCRIPT), "--species", species],
            check=True,
        )
        print(f"prepared {species}")


if __name__ == "__main__":
    main()
