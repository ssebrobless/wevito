#!/usr/bin/env python3
"""
Generate animal sound effects using ElevenLabs AI.
Run this script after setting your ELEVENLABS_API_KEY environment variable.

Usage:
    set ELEVENLABS_API_KEY=your_api_key_here
    python generate_wevito_sounds.py
"""

import os
import sys
from pathlib import Path

# Configuration
OUTPUT_DIR = Path(r"C:\users\fishe\pet-overlay\sounds")
SOUND_DIR = OUTPUT_DIR
PROMPT_FILE = OUTPUT_DIR / "prompts.txt"

# Animal sound prompts - customize these for better results
ANIMAL_PROMPTS = {
    "rat": {
        "idle": "Small rat squeaking softly, cute high-pitched chirp",
        "feed": "Rat nibbling and chomping, eating sounds",
        "rest": "Soft rat breathing, gentle sleepy sounds",
        "pet": "Rat squeaking happily, content chirping",
        "bathe": "Rat shaking and rustling, grooming sounds",
        "groom": "Rat cleaning itself, small licking and scratching",
        "exercise": "Rat scurrying, quick scampering sounds",
    },
    "crow": {
        "idle": "Crow cawing, deep bird call",
        "feed": "Crow eating, beak sounds and swallowing",
        "rest": "Crow cooing softly, peaceful bird sounds",
        "pet": "Crow making content sounds, gentle caw",
        "bathe": "Crow flapping wings, splashing in water",
        "groom": "Crow preening, feather cleaning sounds",
        "exercise": "Crow flapping wings, taking flight",
    },
    "fox": {
        "idle": "Fox yap or bark, sharp canine sound",
        "feed": "Fox eating, tearing and chewing sounds",
        "rest": "Fox breathing softly, sleeping sounds",
        "pet": "Fox whining happily, soft yip",
        "bathe": "Fox shaking fur, splashing sounds",
        "groom": "Fox licking fur, cleaning sounds",
        "exercise": "Fox running, paws hitting ground",
    },
    "snake": {
        "idle": "Snake hissing, serpentine warning sound",
        "feed": "Snake swallowing, large gulp sounds",
        "rest": "Snake breathing, subtle air movement",
        "pet": "Snake hissing softly, gentle tongue flick",
        "bathe": "Snake slithering in water, splashing",
        "groom": "Snake rubbing against surface, scales scraping",
        "exercise": "Snake slithering quickly, moving through grass",
    },
    "deer": {
        "idle": "Deer snorting softly, gentle breathing",
        "feed": "Deer chewing, grass and leaves sounds",
        "rest": "Deer breathing slowly, peaceful rest sounds",
        "pet": "Deer making soft bleat, gentle sound",
        "bathe": "Deer shaking, hoof stamping in water",
        "groom": "Deer rubbing antlers, scratching sounds",
        "exercise": "Deer running, hoofbeats on ground",
    },
    "frog": {
        "idle": "Frog croaking, amphibian ribbit sound",
        "feed": "Frog catching insect, quick tongue snap",
        "rest": "Frog sitting still, occasional croak",
        "pet": "Frog making content croaking sounds",
        "bathe": "Frog splashing into water, jumping in pond",
        "groom": "Frog cleaning eyes with front legs",
        "exercise": "Frog hopping, jumping sounds",
    },
    "pigeon": {
        "idle": "Pigeon cooing, soft bird call",
        "feed": "Pigeon eating seeds, beak picking sounds",
        "rest": "Pigeon cooing softly, peaceful sounds",
        "pet": "Pigeon making gentle cooing, content sounds",
        "bathe": "Pigeon flapping wings, splashing in water",
        "groom": "Pigeon preening, feather cleaning",
        "exercise": "Pigeon taking flight, wing flapping",
    },
    "raccoon": {
        "idle": "Raccoon chittering, curious animal sounds",
        "feed": "Raccoon eating, hands picking and chewing",
        "rest": "Raccoon snoring softly, sleeping sounds",
        "pet": "Raccoon purring, content growling",
        "bathe": "Raccoon splashing in water, washing hands",
        "groom": "Raccoon cleaning face with hands",
        "exercise": "Raccoon running, scampering quickly",
    },
    "squirrel": {
        "idle": "Squirrel chirping, quick alert sounds",
        "feed": "Squirrel cracking nuts, eating seeds",
        "rest": "Squirrel resting, occasional chirp",
        "pet": "Squirrel making happy chirps, content sounds",
        "bathe": "Squirrel shaking fur, grooming shake",
        "groom": "Squirrel cleaning tail, licking fur",
        "exercise": "Squirrel running along branch, quick scamper",
    },
    "goose": {
        "idle": "Goose honking, loud bird call",
        "feed": "Goose eating grass, beak grazing",
        "rest": "Goose honking softly, peaceful sounds",
        "pet": "Goose making gentle honking, content sounds",
        "bathe": "Goose splashing in water, flapping wings",
        "groom": "Goose preening, feather cleaning",
        "exercise": "Goose flapping wings, running with wings",
    },
}

# Special sounds
SPECIAL_PROMPTS = {
    "hatch": "Egg cracking, baby chick peeping, hatching sounds",
    "ghost": "Spooky ethereal ghost sound, eerie whisper, supernatural whoosh",
}


def install_dependencies():
    """Install required packages."""
    print("Installing elevenlabs SDK...")
    import subprocess

    subprocess.check_call([sys.executable, "-m", "pip", "install", "elevenlabs", "python-dotenv"])


def generate_sound(client, prompt: str, output_path: Path):
    """Generate a sound effect and save it."""
    try:
        # Generate the sound
        audio = client.text_to_sound_effects.convert(
            text=prompt,
            model_id="sunset",  # Use sunset model for sound effects
        )

        # Save as MP3
        with open(output_path, "wb") as f:
            for chunk in audio:
                if chunk:
                    f.write(chunk)

        print(f"  [SAVED] {output_path.name}")
        return True

    except Exception as e:
        print(f"  [ERROR] {e}")
        return False


def main():
    print("=" * 60)
    print("Wevito Sound Generator - ElevenLabs AI")
    print("=" * 60)

    # Check for API key
    api_key = os.getenv("ELEVENLABS_API_KEY")
    if not api_key:
        print("\nERROR: ELEVENLABS_API_KEY not found!")
        print("\nTo get started:")
        print("1. Go to https://elevenlabs.io and create a free account")
        print("2. Go to Profile → API Keys")
        print("3. Create a new API key")
        print("4. Run: set ELEVENLABS_API_KEY=your_key_here")
        print("5. Run this script again")
        print("\nOr add to .env file in this folder:")
        print("ELEVENLABS_API_KEY=your_key_here")
        return

    print(f"\nUsing API key: {api_key[:8]}...{api_key[-4:]}")

    # Try to import elevenlabs, install if needed
    try:
        from elevenlabs.client import ElevenLabs
    except ImportError:
        install_dependencies()
        from elevenlabs.client import ElevenLabs

    # Initialize client
    client = ElevenLabs(api_key=api_key)

    print(f"\nOutput directory: {OUTPUT_DIR}")
    print("\nGenerating sounds...")

    total_generated = 0

    # Generate animal sounds
    for animal, actions in ANIMAL_PROMPTS.items():
        print(f"\n{animal.upper()}:")
        for action, prompt in actions.items():
            output_file = SOUND_DIR / f"{animal}_{action}.mp3"
            print(f"  {action}: {prompt[:50]}...")
            if generate_sound(client, prompt, output_file):
                total_generated += 1

    # Generate special sounds
    print(f"\nSPECIAL:")
    for action, prompt in SPECIAL_PROMPTS.items():
        output_file = SOUND_DIR / f"special_{action}.mp3"
        print(f"  {action}: {prompt[:50]}...")
        if generate_sound(client, prompt, output_file):
            total_generated += 1

    print("\n" + "=" * 60)
    print(f"Complete! Generated {total_generated} sound files.")
    print("=" * 60)


if __name__ == "__main__":
    main()
