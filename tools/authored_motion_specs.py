#!/usr/bin/env python3
"""
Shared motion-family specs for focused sprite authoring packs.
"""

from __future__ import annotations

from typing import Final


MOTION_FAMILIES: Final[dict[str, dict[str, object]]] = {
    "locomotion": {
        "title": "locomotion",
        "description": "idle and walk motion refinement",
        "frame_layout": [
            ["idle_00", "idle_01", "idle_02", "idle_03", "walk_00"],
            ["walk_01", "walk_02", "walk_03", "walk_04", "walk_05"],
        ],
        "prompt_focus": [
            "Only improve idle_00 through idle_03 and walk_00 through walk_05.",
            "Idle should preserve the species' neutral silhouette while adding subtle life.",
            "Walk should show realistic species-specific locomotion, stable ground contact, and clear readable weight transfer.",
        ],
    },
    "locomotion_idle": {
        "title": "locomotion idle",
        "description": "idle-only motion refinement",
        "frame_layout": [
            ["idle_00", "idle_01", "idle_02", "idle_03", None],
        ],
        "prompt_focus": [
            "Only improve idle_00 through idle_03.",
            "Idle should preserve the species' neutral silhouette while adding subtle life.",
            "Keep the animal on-model, calm, and readable with no unnecessary motion blur or distortion.",
        ],
    },
    "locomotion_walk_a": {
        "title": "locomotion walk A",
        "description": "walk-cycle refinement, first half",
        "frame_layout": [
            ["walk_00", "walk_01", "walk_02", None, None],
        ],
        "prompt_focus": [
            "Only improve walk_00 through walk_02.",
            "These frames should establish the first half of a realistic species-specific locomotion cycle.",
            "Keep the animal grounded, readable, and on-model with clear weight transfer and stable contact.",
        ],
    },
    "locomotion_walk_b": {
        "title": "locomotion walk B",
        "description": "walk-cycle refinement, second half",
        "frame_layout": [
            ["walk_03", "walk_04", "walk_05", None, None],
        ],
        "prompt_focus": [
            "Only improve walk_03 through walk_05.",
            "These frames should complete the second half of a realistic species-specific locomotion cycle.",
            "Keep the animal grounded, readable, and on-model with stable pacing that matches the first half.",
        ],
    },
    "care": {
        "title": "care",
        "description": "eat, drink-usable head-down, and sleep pose refinement",
        "frame_layout": [
            ["eat_00", "eat_01", "eat_02", "eat_03", "sleep_00"],
            ["sleep_01", None, None, None, None],
        ],
        "prompt_focus": [
            "Only improve eat_00 through eat_03 and sleep_00 through sleep_01.",
            "The eat frames should work as convincing head-down food or water interaction poses.",
            "The sleep frames should become clear static resting poses that feel species-appropriate and calm.",
        ],
    },
    "expression": {
        "title": "expression",
        "description": "happy, sad, sick, and bathe pose refinement",
        "frame_layout": [
            ["happy_00", "happy_01", "happy_02", "happy_03", "sad_00"],
            ["sad_01", "sick_00", "sick_01", "sick_02", "sick_03"],
            ["bathe_00", "bathe_01", "bathe_02", "bathe_03", None],
        ],
        "prompt_focus": [
            "Only improve happy, sad, sick, and bathe frames in this board.",
            "Each emotional/action pose should stay on-model and readable without adding blur or noisy motion.",
            "Sick frames should feel low-energy and heavy rather than distorted.",
        ],
    },
}


SPECIES_LOCOMOTION_NOTES: Final[dict[str, str]] = {
    "rat": "Walk as a small quadruped with clear front and rear paw stepping, slight torso shift, and a controlled tail follow-through.",
    "crow": "Move as a grounded hop-step bird with leg motion visible and occasional tiny balancing wing flicks during the hop.",
    "fox": "Walk as a fluid quadruped with real stepping front and rear legs, mild shoulder and hip shift, and a subtle tail wag.",
    "snake": "Idle should stay coiled and calm. Walk must show an extended full-body side-view slither inside each walk frame, not a chopped segment or a continuous row-spanning snake.",
    "deer": "Walk as a long-legged quadruped with clear hoof stepping, shoulder and hip timing, and a stable head/neck rhythm.",
    "frog": "Idle should crouch naturally. Walk should really be a frog hop cycle with crouch, hind-leg extension, forward travel, and compact landing poses.",
    "pigeon": "Move as a grounded hop-step bird with visible leg motion, full chest silhouette, and tiny balancing wing flutters when hopping.",
    "raccoon": "Idle should read upright on two legs. Walk should drop to all fours with clear stepping paws and a ringed-tail balance shift.",
    "squirrel": "Idle should read upright on two legs. Walk should drop to all fours with fast small steps and the tail acting as a balancing counterweight.",
    "goose": "Walk as a longer-stride bird with visible leg motion, steady body carriage, and occasional subtle wing balance motion.",
}


SPECIES_CARE_NOTES: Final[dict[str, str]] = {
    "rat": "Head-down interaction should read like cautious nibbling or drinking with whiskers and nose preserved. Sleep should curl the body gently.",
    "crow": "Head-down interaction should read like pecking or drinking. Sleep should settle low with the body tucked and calm.",
    "fox": "Head-down interaction should read like lowering the muzzle to food or water. Sleep should curl into a believable resting pose.",
    "snake": "Head-down interaction should read like a deliberate forward dip without losing the full body silhouette. Sleep should coil into a restful compact pose.",
    "deer": "Head-down interaction should read like grazing or drinking. Sleep should feel tucked and gentle rather than collapsed.",
    "frog": "Head-down interaction should read like a compact crouched sip or nibble. Sleep should be a low resting crouch.",
    "pigeon": "Head-down interaction should read like pecking or drinking. Sleep should feel tucked and still.",
    "raccoon": "Head-down interaction should read like paw-forward nibbling or bowl investigation. Sleep should curl or settle heavily.",
    "squirrel": "Head-down interaction should read like quick nibbling or bowl drinking. Sleep should curl with the tail helping frame the body.",
    "goose": "Head-down interaction should read like foraging or drinking with the neck lowered. Sleep should feel folded and restful.",
}


SPECIES_EXPRESSION_NOTES: Final[dict[str, str]] = {
    "rat": "Happy should brighten with small buoyant body shifts; sad and sick should feel lower and smaller without clipping the face.",
    "crow": "Happy can use tiny wing lift or alert posture; sad and sick should feel drooped and compact.",
    "fox": "Happy can include a light tail response; sad and sick should read through lowered head and body weight.",
    "snake": "Happy should not uncoil wildly. Sad and sick should feel lower-energy through posture, not random distortion.",
    "deer": "Happy should stay elegant; sad and sick should lower the neck and reduce energy clearly.",
    "frog": "Happy should feel springy; sad and sick should settle flatter to the ground.",
    "pigeon": "Happy can use a light chest lift; sad and sick should read as tucked and low-energy.",
    "raccoon": "Happy should feel curious and lively; sad and sick should feel heavier and less upright.",
    "squirrel": "Happy should feel alert and upright; sad and sick should feel tucked and drooped.",
    "goose": "Happy should feel proud but contained; sad and sick should reduce neck lift and energy.",
}


def get_family_layout(family: str) -> list[list[str | None]]:
    family_spec = MOTION_FAMILIES.get(family)
    if family_spec is None:
        raise KeyError(f"Unknown motion family: {family}")
    return family_spec["frame_layout"]  # type: ignore[return-value]


def get_species_note(species: str, family: str) -> str:
    if family.startswith("locomotion"):
        return SPECIES_LOCOMOTION_NOTES.get(species, "Use realistic, species-appropriate locomotion while preserving the canonical design.")
    if family == "care":
        return SPECIES_CARE_NOTES.get(species, "Use convincing head-down interaction and restful sleep poses while preserving the canonical design.")
    if family == "expression":
        return SPECIES_EXPRESSION_NOTES.get(species, "Keep expressive poses readable and on-model while preserving the canonical design.")
    return "Preserve the canonical design and improve motion only where requested."
