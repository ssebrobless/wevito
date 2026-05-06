from __future__ import annotations

import argparse
import json
import re
from dataclasses import dataclass
from pathlib import Path

import fitz
import pdfplumber
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import (
    Flowable,
    KeepTogether,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = REPO_ROOT / "docs" / "WEVITO_PROJECT_COMPLETION_DASHBOARD_PACKET_2026-05-05.pdf"
DEFAULT_VALIDATION = REPO_ROOT / "tmp" / "pdfs" / "wevito-project-dashboard-packet-validation.json"
DEFAULT_RENDER_DIR = REPO_ROOT / "tmp" / "pdfs" / "wevito-project-dashboard-packet-render"


@dataclass(frozen=True)
class ProjectArea:
    name: str
    status: str
    percent: int
    remaining: str


@dataclass(frozen=True)
class WorkCategory:
    title: str
    percent: int
    priority: str
    purpose: str
    bullets: tuple[str, ...]


AREAS: tuple[ProjectArea, ...] = (
    ProjectArea("Git/build baseline", "Clean and current", 90, "Keep main clean, rebuild only when needed, avoid regenerating noisy local folders."),
    ProjectArea("Core pet sim", "Strong foundation", 68, "Tighten personality development, aging/death/ghost state, visible care outcomes, save/load proof."),
    ProjectArea("Game interactions", "Usable but uneven", 50, "Real staged fetch loop, visible drink/eat/care actions, forced scenario testing."),
    ProjectArea("Sprites/visuals", "Much better, not final", 72, "Final in-motion QA, optional animations, no artifacts, no boxing/shrink-grow, final proof sweeps."),
    ProjectArea("Color variants", "Mostly green", 85, "Only targeted review/fixes, not broad regeneration."),
    ProjectArea("Care/medicine/items", "Assets exist", 45, "Map items into gameplay/UI, confirm readability, remove stale expectations."),
    ProjectArea("Habitat/environment", "Planned well", 35, "Runtime anchors, depth, occlusion, shadows, enter/perch/hide/use zones."),
    ProjectArea("Overlay UI", "Direction is clear", 50, "Implement Claude/visual design direction in vNext without replacing pet overlay."),
    ProjectArea("PET TASKS/tool hub", "Functional seed exists", 40, "Simpler UI, artifact buttons, report-only polish, then approval-gated execution."),
    ProjectArea("Screenshot/capture tools", "Planned", 20, "Wevito-window screenshot first, then region capture, later short proof clips."),
    ProjectArea("Translation/audio tools", "Early implementation/plans", 30, "Confirm provider UX, DeepL/API handling, safe Windows volume only, external booster handoff."),
    ProjectArea("Coding helpers", "Planned/partial", 25, "Code review reports, patch plans, then tightly approved patch execution."),
    ProjectArea("Sprite Workflow V2", "Designed, not built", 20, "One-screen workbench, provenance, contact sheets, dry-run/apply/rollback."),
    ProjectArea("Creative Learning Lab", "Concept planned", 15, "Reviewed examples, preference memory, dataset/export dashboard, no uncontrolled training."),
    ProjectArea("Pet AI agents", "Concept planned", 18, "Three named helper pets, roles, task cards, permissions, logs, no unsafe autonomy."),
    ProjectArea("Final release QA", "Not ready yet", 42, "Full tests, Godot proof, in-game sweeps, final docs/help, packaging."),
)

SUMMARY_BARS: tuple[tuple[str, int], ...] = (
    ("Playable pet game foundation", 68),
    ("Visual/sprite asset lane", 72),
    ("Habitat/items/care integration", 45),
    ("PET TASKS/tool hub", 40),
    ("Advanced helper/AI/ML systems", 18),
    ("Final QA/release readiness", 42),
)

CATEGORIES: tuple[WorkCategory, ...] = (
    WorkCategory(
        "Game Core",
        68,
        "High",
        "The game spine: simulation, visible interactions, persistence, and repeatable proof scenarios.",
        (
            "Finish personality development, needs/drives/emotions, health/body condition, aging, death, and ghost state.",
            "Make feeding, drinking, medicine/care, bathing/grooming, sleep, and ball play visibly map to actions.",
            "Add forced dev scenarios and in-game sweeps so behavior can be verified without guessing.",
        ),
    ),
    WorkCategory(
        "Visuals And Animation",
        72,
        "High",
        "The highest visible product risk: every sprite and animation must read cleanly in motion.",
        (
            "Keep current color coverage, shared asset cleanup, runtime overlay prop contract, and visual review discipline.",
            "Finish in-motion QA, optional ball animations, size consistency, artifact cleanup, and no-boxing motion policy.",
            "Defer ghost/fat/skinny variants until healthy base sprites and animations are accepted.",
        ),
    ),
    WorkCategory(
        "Habitat, Items, And Environment",
        45,
        "Medium-high",
        "Turns Wevito from pets on a screen into pets that live in a readable world.",
        (
            "Map care, medicine, food, water, toys, egg lifecycle, and status assets into gameplay/UI records.",
            "Implement habitat object anchors, interaction zones, depth bands, occlusion, and contact shadows.",
            "Remove stale references for abandoned features so the game expects only real assets.",
        ),
    ),
    WorkCategory(
        "Overlay UI And Tool Hub",
        40,
        "High",
        "The bridge between the living pet overlay and powerful helper tools.",
        (
            "Keep the overlay as home; summon tools as compact popups/workbenches.",
            "Polish PET TASKS with helper pets, command input, task cards, result cards, and report-only labels.",
            "Add artifact actions such as Open Report, Copy Path, and Open Folder before execution tools.",
        ),
    ),
    WorkCategory(
        "Helper Tools",
        30,
        "Medium-high",
        "Useful tools should appear as simple pet task cards, not a complicated technical dashboard.",
        (
            "Keep localDocs, spriteAudit, assetInventory, petState, codeReview, and codePatchPlan report-only first.",
            "Add approval-gated buildProof, Wevito-window screenshot, region screenshot, translation, and safe volume control.",
            "Keep screen recording, browser automation, external audio boosters, sprite import, and mutation workflows gated.",
        ),
    ),
    WorkCategory(
        "Sprite Workflow V2",
        20,
        "Medium",
        "A compact replacement for the confusing earlier sprite workflow experience.",
        (
            "Show one selected row with source, runtime, candidate, proof, validator findings, provenance, and hashes.",
            "Start read-only with no-mutation previews, contact sheets, and dry-run apply plans.",
            "Add one-row apply, backup-before-apply, exact rollback, and post-apply proof only after gates are stable.",
        ),
    ),
    WorkCategory(
        "Creative Learning Lab And AI Agents",
        18,
        "Medium-low until tool foundations are stable",
        "The most exciting long-term lane, but it depends on clean artifacts, permissions, and rollback rules.",
        (
            "Use exactly three active helper pets with user names, roles, current task cards, and clear permission boundaries.",
            "Capture reviewed examples, preference snapshots, issue labels, and artifact-backed memory.",
            "Avoid uncontrolled self-training; start with reviewed-data dashboards and simple preference learning.",
        ),
    ),
    WorkCategory(
        "Release Readiness",
        42,
        "High near the end",
        "The final gate that proves the player-facing game and helper surfaces behave correctly.",
        (
            "Run full code validation, Godot/package validation, and visual proof sweeps.",
            "Run forced in-game scenarios for all pets, ages, genders, colors, and key interactions.",
            "Package the game/tools and write final user-facing controls/help docs.",
        ),
    ),
)

SOURCE_DOCS: tuple[str, ...] = (
    "docs/WEVITO_FULL_PROJECT_STATUS_AND_COMPLETION_ROADMAP_2026-05-05.md",
    "docs/WEVITO_VISUAL_COMPLETION_TRACKER_2026-05-05.md",
    "docs/WEVITO_VISUAL_STATUS_DASHBOARD_2026-05-05.md",
    "docs/WEVITO_TOOL_AGENT_UI_MASTER_PLAN_2026-05-05.md",
    "docs/WEVITO_PET_HELPER_FUNCTIONS_ROADMAP_2026-05-05.md",
    "docs/WEVITO_OVERLAY_FIRST_IMPLEMENTATION_ROADMAP_2026-05-05.md",
    "docs/CODE_SIDE_DEEP_REVIEW_AND_AGENT_TOOLS_PLAN_2026-05-05.md",
)


class ProgressBar(Flowable):
    def __init__(self, label: str, percent: int, width: float = 4.7 * inch, height: float = 0.22 * inch):
        super().__init__()
        self.label = label
        self.percent = percent
        self.width = width
        self.height = height

    def wrap(self, availWidth, availHeight):
        return min(self.width, availWidth), self.height + 0.24 * inch

    def draw(self):
        fill_width = self.width * max(0, min(self.percent, 100)) / 100.0
        label_y = self.height + 0.09 * inch
        self.canv.setFillColor(colors.HexColor("#E8EEF6"))
        self.canv.roundRect(0, 0, self.width, self.height, 5, fill=True, stroke=False)
        color = "#2E6F9E" if self.percent >= 60 else "#B8792F" if self.percent >= 35 else "#9E4A4A"
        self.canv.setFillColor(colors.HexColor(color))
        self.canv.roundRect(0, 0, fill_width, self.height, 5, fill=True, stroke=False)
        self.canv.setFillColor(colors.HexColor("#142235"))
        self.canv.setFont("Helvetica-Bold", 8.5)
        self.canv.drawString(0, label_y, self.label)
        self.canv.setFont("Helvetica", 8)
        self.canv.drawRightString(self.width, label_y, f"{self.percent}%")


def clean_text(text: str) -> str:
    replacements = {
        "\u2010": "-",
        "\u2011": "-",
        "\u2012": "-",
        "\u2013": "-",
        "\u2014": "-",
        "\u2018": "'",
        "\u2019": "'",
        "\u201c": '"',
        "\u201d": '"',
    }
    for src, dest in replacements.items():
        text = text.replace(src, dest)
    return text


def paragraph(text: str, style: ParagraphStyle) -> Paragraph:
    return Paragraph(clean_text(text), style)


def page_footer(canvas, doc):
    canvas.saveState()
    canvas.setStrokeColor(colors.HexColor("#D8E0EA"))
    canvas.line(doc.leftMargin, 0.52 * inch, letter[0] - doc.rightMargin, 0.52 * inch)
    canvas.setFillColor(colors.HexColor("#556273"))
    canvas.setFont("Helvetica", 8)
    canvas.drawString(doc.leftMargin, 0.34 * inch, "Wevito Project Completion Dashboard")
    canvas.drawRightString(letter[0] - doc.rightMargin, 0.34 * inch, f"Page {doc.page}")
    canvas.restoreState()


def build_styles():
    base = getSampleStyleSheet()
    return {
        "title": ParagraphStyle(
            "Title",
            parent=base["Title"],
            fontName="Helvetica-Bold",
            fontSize=26,
            leading=31,
            textColor=colors.HexColor("#102033"),
            alignment=TA_CENTER,
            spaceAfter=16,
        ),
        "subtitle": ParagraphStyle(
            "Subtitle",
            parent=base["Normal"],
            fontSize=11,
            leading=16,
            textColor=colors.HexColor("#415168"),
            alignment=TA_CENTER,
            spaceAfter=22,
        ),
        "h1": ParagraphStyle(
            "Heading1",
            parent=base["Heading1"],
            fontName="Helvetica-Bold",
            fontSize=17,
            leading=21,
            textColor=colors.HexColor("#163450"),
            spaceBefore=12,
            spaceAfter=9,
        ),
        "h2": ParagraphStyle(
            "Heading2",
            parent=base["Heading2"],
            fontName="Helvetica-Bold",
            fontSize=13,
            leading=17,
            textColor=colors.HexColor("#1E4E73"),
            spaceBefore=9,
            spaceAfter=5,
        ),
        "body": ParagraphStyle(
            "Body",
            parent=base["BodyText"],
            fontSize=9.2,
            leading=13,
            textColor=colors.HexColor("#1E2937"),
            spaceAfter=6,
        ),
        "small": ParagraphStyle(
            "Small",
            parent=base["BodyText"],
            fontSize=7.6,
            leading=10,
            textColor=colors.HexColor("#334155"),
        ),
        "chip": ParagraphStyle(
            "Chip",
            parent=base["BodyText"],
            fontName="Helvetica-Bold",
            fontSize=8,
            leading=10,
            textColor=colors.HexColor("#102033"),
            alignment=TA_LEFT,
        ),
    }


def make_table(data, col_widths, style_commands):
    table = Table(data, colWidths=col_widths, repeatRows=1)
    table.setStyle(TableStyle(style_commands))
    return table


def build_story():
    styles = build_styles()
    story = []

    story.append(Spacer(1, 0.25 * inch))
    story.append(paragraph("Wevito Project Completion Dashboard", styles["title"]))
    story.append(paragraph("Reference packet for game, visual, tool, helper-agent, and release readiness work", styles["subtitle"]))

    overview_data = [
        [paragraph("Overall Full-Vision Completion", styles["chip"]), paragraph("55%", styles["chip"])],
        [paragraph("Remaining To Full Vision", styles["chip"]), paragraph("45%", styles["chip"])],
        [paragraph("Basic Playable Pet Game Estimate", styles["chip"]), paragraph("65-70%", styles["chip"])],
        [paragraph("Repo Baseline", styles["chip"]), paragraph("main clean and pushed at ac3d1fe6a", styles["chip"])],
    ]
    story.append(make_table(
        overview_data,
        [3.4 * inch, 2.6 * inch],
        [
            ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#F3F7FC")),
            ("BOX", (0, 0), (-1, -1), 0.8, colors.HexColor("#C7D5E5")),
            ("INNERGRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#D8E0EA")),
            ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
            ("LEFTPADDING", (0, 0), (-1, -1), 8),
            ("RIGHTPADDING", (0, 0), (-1, -1), 8),
            ("TOPPADDING", (0, 0), (-1, -1), 7),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
        ],
    ))
    story.append(Spacer(1, 0.18 * inch))

    story.append(paragraph("Current Completion Shape", styles["h1"]))
    for label, percent in SUMMARY_BARS:
        story.append(ProgressBar(label, percent))
    story.append(Spacer(1, 0.15 * inch))
    story.append(paragraph(
        "This dashboard treats the full vision as more than a basic pet game: it includes clean sprites and animation proofing, habitats, items, helper tools, screenshot/capture, translation, audio assist, coding helpers, Sprite Workflow V2, Creative Learning Lab, and pet-agent behavior.",
        styles["body"],
    ))

    story.append(PageBreak())
    story.append(paragraph("Area Status", styles["h1"]))
    table_rows = [[
        paragraph("Area", styles["chip"]),
        paragraph("Status", styles["chip"]),
        paragraph("Done", styles["chip"]),
        paragraph("What Remains", styles["chip"]),
    ]]
    for area in AREAS:
        table_rows.append([
            paragraph(area.name, styles["small"]),
            paragraph(area.status, styles["small"]),
            paragraph(f"{area.percent}%", styles["small"]),
            paragraph(area.remaining, styles["small"]),
        ])
    story.append(make_table(
        table_rows,
        [1.35 * inch, 1.25 * inch, 0.55 * inch, 3.55 * inch],
        [
            ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#E7F0FA")),
            ("TEXTCOLOR", (0, 0), (-1, 0), colors.HexColor("#102033")),
            ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#CDD8E5")),
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F8FAFD")]),
            ("LEFTPADDING", (0, 0), (-1, -1), 5),
            ("RIGHTPADDING", (0, 0), (-1, -1), 5),
            ("TOPPADDING", (0, 0), (-1, -1), 5),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ],
    ))

    story.append(PageBreak())
    story.append(paragraph("Remaining Work By Category", styles["h1"]))
    for category in CATEGORIES:
        block = [
            paragraph(category.title, styles["h2"]),
            ProgressBar(f"{category.title} readiness", category.percent, width=5.7 * inch),
            paragraph(f"<b>Priority:</b> {category.priority}", styles["body"]),
            paragraph(category.purpose, styles["body"]),
        ]
        bullet_rows = [[paragraph("No.", styles["chip"]), paragraph("Remaining Work", styles["chip"])]]
        for index, bullet in enumerate(category.bullets, start=1):
            bullet_rows.append([paragraph(str(index), styles["small"]), paragraph(bullet, styles["small"])])
        block.append(make_table(
            bullet_rows,
            [0.45 * inch, 5.85 * inch],
            [
                ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#EDF4FA")),
                ("GRID", (0, 0), (-1, -1), 0.3, colors.HexColor("#D5DEEA")),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 5),
                ("RIGHTPADDING", (0, 0), (-1, -1), 5),
                ("TOPPADDING", (0, 0), (-1, -1), 4),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
            ],
        ))
        story.append(KeepTogether(block))
        story.append(Spacer(1, 0.08 * inch))

    story.append(PageBreak())
    story.append(paragraph("Recommended Execution Order", styles["h1"]))
    order = (
        "Finish Tool Hub/PET TASKS UI clarity.",
        "Finish action/animation/prop contract.",
        "Implement staged fetch/drink/care proof flows.",
        "Map care/items/habitats into runtime UI.",
        "Let visual-side finish targeted sprite/animation QA.",
        "Add safe helper tools one at a time.",
        "Build Sprite Workflow V2 read-only workbench.",
        "Add controlled apply/proof/rollback gates.",
        "Add Creative Learning Lab reviewed-data surface.",
        "Run final in-game QA and release packaging.",
    )
    for i, item in enumerate(order, start=1):
        story.append(paragraph(f"<b>{i}.</b> {item}", styles["body"]))

    story.append(Spacer(1, 0.15 * inch))
    story.append(paragraph("Source-Of-Truth Docs", styles["h1"]))
    for doc in SOURCE_DOCS:
        story.append(paragraph(doc, styles["small"]))

    story.append(Spacer(1, 0.15 * inch))
    story.append(paragraph("Update Workflow", styles["h1"]))
    story.append(paragraph("Update the living markdown dashboard first, then regenerate this packet with:", styles["body"]))
    story.append(paragraph("python .\\tools\\export-project-dashboard-packet-pdf.py", styles["small"]))

    return story


def validate_pdf(pdf_path: Path, render_dir: Path, validation_path: Path) -> dict:
    render_dir.mkdir(parents=True, exist_ok=True)
    validation_path.parent.mkdir(parents=True, exist_ok=True)

    doc = fitz.open(pdf_path)
    rendered_pages = []
    blank_pages = []
    for index, page in enumerate(doc):
        pix = page.get_pixmap(matrix=fitz.Matrix(1.25, 1.25), alpha=False)
        output = render_dir / f"page-{index + 1:02d}.png"
        pix.save(output)
        rendered_pages.append(str(output))
        if pix.width == 0 or pix.height == 0:
            blank_pages.append(index + 1)
    page_count = doc.page_count
    doc.close()

    with pdfplumber.open(pdf_path) as pdf:
        text = "\n".join(page.extract_text() or "" for page in pdf.pages)

    required_phrases = [
        "Wevito Project Completion Dashboard",
        "Overall Full-Vision Completion",
        "Area Status",
        "Remaining Work By Category",
        "Recommended Execution Order",
    ]
    missing_phrases = [phrase for phrase in required_phrases if phrase not in text]
    suspicious_unicode = sorted(set(re.findall(r"[\u2010-\u2015\uFFFD]", text)))

    validation = {
        "pdf": str(pdf_path),
        "page_count": page_count,
        "rendered_pages": rendered_pages,
        "blank_pages": blank_pages,
        "missing_required_phrases": missing_phrases,
        "suspicious_unicode": suspicious_unicode,
        "text_length": len(text),
        "pass": page_count > 0 and not blank_pages and not missing_phrases and not suspicious_unicode,
    }
    validation_path.write_text(json.dumps(validation, indent=2), encoding="utf-8")
    return validation


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate the polished Wevito project completion PDF packet.")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--validation", type=Path, default=DEFAULT_VALIDATION)
    parser.add_argument("--render-dir", type=Path, default=DEFAULT_RENDER_DIR)
    args = parser.parse_args()

    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)

    doc = SimpleDocTemplate(
        str(output),
        pagesize=letter,
        rightMargin=0.48 * inch,
        leftMargin=0.48 * inch,
        topMargin=0.54 * inch,
        bottomMargin=0.65 * inch,
        title="Wevito Project Completion Dashboard",
        author="Codex",
        subject="Wevito project completion reference packet",
    )
    doc.build(build_story(), onFirstPage=page_footer, onLaterPages=page_footer)

    validation = validate_pdf(output, args.render_dir.resolve(), args.validation.resolve())
    print(json.dumps(validation, indent=2))
    if not validation["pass"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
