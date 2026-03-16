#!/usr/bin/env python3
"""
Export authoring packs for every species in the incoming pose manifest.
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MANIFEST_PATH = ROOT / "tools" / "incoming_animal_pose_manifest.json"
EXPORT_SCRIPT = ROOT / "tools" / "export_species_authoring_pack.py"


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
            [sys.executable, str(EXPORT_SCRIPT), "--species", species],
            check=True,
        )
        print(f"exported {species}")


if __name__ == "__main__":
    main()
