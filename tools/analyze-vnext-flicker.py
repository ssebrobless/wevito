from __future__ import annotations

import argparse
from datetime import datetime
import json
import shutil
import subprocess
from pathlib import Path

import numpy
from PIL import Image, ImageChops


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--video", required=True)
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--fps", type=float, default=12.0)
    parser.add_argument("--regions", required=True)
    parser.add_argument("--automation-log")
    parser.add_argument("--top", type=int, default=6)
    return parser.parse_args()


def extract_frames(video_path: Path, frames_dir: Path, fps: float) -> list[Path]:
    if frames_dir.exists():
        shutil.rmtree(frames_dir)
    frames_dir.mkdir(parents=True, exist_ok=True)

    pattern = frames_dir / "frame_%05d.png"
    subprocess.run(
        [
            "ffmpeg",
            "-y",
            "-i",
            str(video_path),
            "-vf",
            f"fps={fps}",
            str(pattern),
        ],
        check=True,
        capture_output=True,
        text=True,
    )
    return sorted(frames_dir.glob("frame_*.png"))


def clamp_region(region: dict, width: int, height: int) -> tuple[int, int, int, int]:
    x = max(0, int(region["x"]))
    y = max(0, int(region["y"]))
    w = max(1, int(region["width"]))
    h = max(1, int(region["height"]))
    return x, y, min(width, x + w), min(height, y + h)


def parse_automation_markers(automation_log_path: Path | None) -> list[dict]:
    if automation_log_path is None or not automation_log_path.exists():
        return []

    markers: list[dict] = []
    start_time: datetime | None = None
    for raw_line in automation_log_path.read_text(encoding="utf-8-sig").splitlines():
        line = raw_line.strip()
        if not line:
            continue

        parts = [part.strip() for part in line.split("|", 2)]
        if len(parts) != 3:
            continue

        timestamp = datetime.fromisoformat(parts[0])
        if start_time is None:
            start_time = timestamp

        markers.append(
            {
                "timestamp": timestamp,
                "label": parts[2],
                "timestamp_sec": round((timestamp - start_time).total_seconds(), 3),
            }
        )

    return markers


def annotate_entry(entry: dict, automation_markers: list[dict]) -> dict:
    annotated = dict(entry)
    if not automation_markers:
        annotated["nearest_marker"] = None
        annotated["marker_delta_sec"] = None
        annotated["expected_transition"] = False
        return annotated

    nearest = min(
        automation_markers,
        key=lambda marker: abs(entry["timestamp_sec"] - marker["timestamp_sec"]),
    )
    delta = round(entry["timestamp_sec"] - nearest["timestamp_sec"], 3)
    annotated["nearest_marker"] = nearest["label"]
    annotated["marker_delta_sec"] = delta
    annotated["expected_transition"] = abs(delta) <= 1.0
    return annotated


def score_frame_diffs(frames: list[Path], regions: dict, fps: float, automation_markers: list[dict]) -> dict:
    spikes_dir = Path(regions["output_dir"]) / "spikes"
    spikes_dir.mkdir(parents=True, exist_ok=True)

    region_defs = regions["regions"]
    all_scores: list[dict] = []
    region_scores: dict[str, list[dict]] = {name: [] for name in region_defs}

    previous_image = None
    previous_path = None

    for index, frame_path in enumerate(frames):
        current_image = Image.open(frame_path).convert("RGB")
        if previous_image is None:
            previous_image = current_image
            previous_path = frame_path
            continue

        for region_name, region in region_defs.items():
            box = clamp_region(region, current_image.width, current_image.height)
            current_crop = current_image.crop(box)
            previous_crop = previous_image.crop(box)

            current_array = numpy.asarray(current_crop, dtype=numpy.int16)
            previous_array = numpy.asarray(previous_crop, dtype=numpy.int16)
            diff = numpy.abs(current_array - previous_array)
            score = float(diff.mean() / 255.0)

            entry = {
                "region": region_name,
                "frame_index": index,
                "timestamp_sec": round(index / fps, 3),
                "score": round(score, 6),
                "frame_path": str(frame_path),
                "previous_frame_path": str(previous_path),
            }
            entry = annotate_entry(entry, automation_markers)
            all_scores.append(entry)
            region_scores[region_name].append(entry)

        previous_image = current_image
        previous_path = frame_path

    summary = {
        "fps": fps,
        "frame_count": len(frames),
        "automation_markers": [
            {
                "label": marker["label"],
                "timestamp_sec": marker["timestamp_sec"],
            }
            for marker in automation_markers
        ],
        "top_overall": sorted(all_scores, key=lambda entry: entry["score"], reverse=True)[: regions["top"]],
        "top_by_region": {},
    }

    for region_name, entries in region_scores.items():
        top_entries = sorted(entries, key=lambda entry: entry["score"], reverse=True)[: regions["top"]]
        summary["top_by_region"][region_name] = top_entries

        for ordinal, entry in enumerate(top_entries, start=1):
            region = region_defs[region_name]
            box = clamp_region(region, Image.open(entry["frame_path"]).width, Image.open(entry["frame_path"]).height)
            current_crop = Image.open(entry["frame_path"]).convert("RGB").crop(box)
            previous_crop = Image.open(entry["previous_frame_path"]).convert("RGB").crop(box)
            diff_image = ImageChops.difference(previous_crop, current_crop)

            base_name = f"{region_name}_{ordinal:02d}_t{entry['timestamp_sec']:.3f}".replace(".", "_")
            previous_crop.save(spikes_dir / f"{base_name}_prev.png")
            current_crop.save(spikes_dir / f"{base_name}_curr.png")
            diff_image.save(spikes_dir / f"{base_name}_diff.png")

    return summary


def main() -> int:
    args = parse_args()
    video_path = Path(args.video)
    output_dir = Path(args.output_dir)
    regions_path = Path(args.regions)
    automation_log_path = Path(args.automation_log) if args.automation_log else None

    frames_dir = output_dir / "frames"
    regions = json.loads(regions_path.read_text(encoding="utf-8-sig"))
    regions["output_dir"] = str(output_dir)
    regions["top"] = args.top

    frames = extract_frames(video_path, frames_dir, args.fps)
    automation_markers = parse_automation_markers(automation_log_path)
    summary = score_frame_diffs(frames, regions, args.fps, automation_markers)
    (output_dir / "flicker-summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
