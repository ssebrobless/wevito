from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from pathlib import Path


COLORS = ("red", "orange", "yellow", "blue", "indigo", "violet")


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Repair raccoon adult male runtime rows by replacing the broken "
            "upright placeholder with the clean adult female raccoon runtime body."
        )
    )
    parser.add_argument("--repo-root", default=".", help="Repository root.")
    parser.add_argument("--output-root", required=True, help="Artifact output folder.")
    parser.add_argument("--apply", action="store_true", help="Write runtime PNG replacements.")
    args = parser.parse_args()

    repo = Path(args.repo_root).resolve()
    runtime = repo / "sprites_runtime" / "raccoon" / "adult"
    output_root = Path(args.output_root)
    output_root.mkdir(parents=True, exist_ok=True)

    records: list[dict[str, str]] = []
    for color in COLORS:
        source_dir = runtime / "female" / color
        target_dir = runtime / "male" / color
        if not source_dir.is_dir():
            raise FileNotFoundError(source_dir)
        if not target_dir.is_dir():
            raise FileNotFoundError(target_dir)

        for source in sorted(source_dir.glob("*.png")):
            target = target_dir / source.name
            if not target.exists():
                raise FileNotFoundError(target)

            source_hash = sha256(source)
            before_hash = sha256(target)
            if args.apply and source_hash != before_hash:
                shutil.copy2(source, target)
            after_hash = sha256(target) if args.apply else before_hash
            records.append(
                {
                    "color": color,
                    "frame": source.name,
                    "source": str(source.relative_to(repo)),
                    "target": str(target.relative_to(repo)),
                    "source_sha256": source_hash,
                    "before_sha256": before_hash,
                    "after_sha256": after_hash,
                    "changed": str(source_hash != before_hash).lower(),
                }
            )

    summary = {
        "species": "raccoon",
        "age": "adult",
        "gender": "male",
        "repair": "replace broken upright placeholder rows with clean adult female raccoon rows",
        "applied": args.apply,
        "records": records,
        "changed_count": sum(1 for r in records if r["changed"] == "true"),
    }
    (output_root / "raccoon-adult-male-runtime-consistency.json").write_text(
        json.dumps(summary, indent=2),
        encoding="utf-8",
    )

    lines = [
        "# Raccoon Adult Male Runtime Consistency",
        "",
        f"- Applied: `{args.apply}`",
        f"- Records: `{len(records)}`",
        f"- Changed runtime frames: `{summary['changed_count']}`",
        "- Source: `sprites_runtime/raccoon/adult/female/<color>/`",
        "- Target: `sprites_runtime/raccoon/adult/male/<color>/`",
        "",
        "The adult male runtime rows were the visible raccoon outlier: an upright, skinny placeholder that did not match the rest of the species. This deterministic pass uses the cleaner shipped adult raccoon body as a safe runtime fallback. It does not generate new art and does not touch authored source boards.",
    ]
    (output_root / "raccoon-adult-male-runtime-consistency.md").write_text(
        "\n".join(lines) + "\n",
        encoding="utf-8",
    )
    print(json.dumps({"applied": args.apply, "changed_count": summary["changed_count"]}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
