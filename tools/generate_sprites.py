#!/usr/bin/env python3
"""
Wevito Sprite Generator - Simplified version
Generates proper pixel art sprites for all animals with animations
"""

from PIL import Image
import os

# Color palettes for egg colors
EGG_PALETTES = {
    "red": {"body_tint": (255, 180, 180), "accent_tint": (255, 120, 120), "outline": (180, 40, 40)},
    "orange": {"body_tint": (255, 210, 180), "accent_tint": (255, 160, 100), "outline": (200, 100, 40)},
    "yellow": {"body_tint": (255, 255, 180), "accent_tint": (255, 255, 100), "outline": (180, 160, 40)},
    "blue": {"body_tint": (180, 200, 255), "accent_tint": (120, 160, 255), "outline": (40, 80, 180)},
    "indigo": {"body_tint": (200, 180, 255), "accent_tint": (150, 130, 255), "outline": (60, 40, 180)},
    "violet": {"body_tint": (240, 200, 255), "accent_tint": (220, 150, 255), "outline": (140, 60, 180)},
}

# Animal definitions with colors and sizes
ANIMALS = {
    "rat": {
        "sizes": {"male": (24, 20), "female": (18, 14)},
        "colors": {
            "body": (140, 120, 100),
            "belly": (200, 180, 160),
            "accent": (100, 80, 60),
            "eyes": (20, 20, 20),
            "nose": (180, 100, 120),
            "ears": (160, 140, 120),
        },
        "img_size": (28, 24),
    },
    "crow": {
        "sizes": {"male": (26, 22), "female": (22, 18)},
        "colors": {
            "body": (20, 20, 25),
            "belly": (50, 50, 60),
            "accent": (80, 80, 90),
            "eyes": (20, 20, 20),
            "beak": (50, 40, 30),
            "feet": (40, 30, 20),
        },
        "img_size": (30, 26),
    },
    "fox": {
        "sizes": {"male": (32, 28), "female": (24, 20)},
        "colors": {
            "body": (220, 120, 40),
            "belly": (255, 220, 180),
            "accent": (180, 80, 20),
            "eyes": (20, 20, 20),
            "nose": (20, 20, 20),
            "ears": (220, 100, 40),
        },
        "img_size": (36, 30),
    },
    "snake": {
        "sizes": {"male": (44, 12), "female": (36, 10)},
        "colors": {
            "body": (40, 140, 60),
            "belly": (160, 200, 120),
            "accent": (30, 100, 40),
            "eyes": (20, 20, 20),
            "tongue": (180, 40, 40),
            "scales": (50, 120, 70),
        },
        "img_size": (48, 16),
    },
    "deer": {
        "sizes": {"male": (28, 36), "female": (22, 28)},
        "colors": {
            "body": (140, 100, 60),
            "belly": (200, 180, 150),
            "accent": (100, 70, 40),
            "eyes": (20, 20, 20),
            "nose": (30, 20, 20),
            "antlers": (120, 90, 60),
        },
        "img_size": (32, 40),
    },
    "frog": {
        "sizes": {"male": (26, 22), "female": (22, 18)},
        "colors": {
            "body": (60, 180, 60),
            "belly": (200, 240, 180),
            "accent": (40, 140, 40),
            "eyes": (20, 20, 20),
            "belly_spot": (255, 255, 200),
            "feet": (50, 120, 50),
        },
        "img_size": (30, 26),
    },
    "pigeon": {
        "sizes": {"male": (26, 24), "female": (24, 20)},
        "colors": {
            "body": (140, 140, 150),
            "belly": (200, 200, 210),
            "accent": (100, 100, 120),
            "eyes": (20, 20, 20),
            "beak": (40, 35, 30),
            "neck": (80, 100, 120),
        },
        "img_size": (30, 28),
    },
    "raccoon": {
        "sizes": {"male": (28, 24), "female": (24, 20)},
        "colors": {
            "body": (100, 100, 110),
            "belly": (160, 160, 170),
            "accent": (60, 60, 70),
            "eyes": (20, 20, 20),
            "mask": (20, 20, 20),
            "tail_stripe": (40, 40, 50),
        },
        "img_size": (32, 28),
    },
    "squirrel": {
        "sizes": {"male": (22, 26), "female": (18, 22)},
        "colors": {
            "body": (180, 130, 60),
            "belly": (240, 220, 180),
            "accent": (140, 90, 40),
            "eyes": (20, 20, 20),
            "nose": (20, 20, 20),
            "tail": (200, 140, 80),
        },
        "img_size": (28, 32),
    },
    "goose": {
        "sizes": {"male": (32, 30), "female": (24, 22)},
        "colors": {
            "body": (240, 240, 250),
            "belly": (220, 220, 230),
            "accent": (200, 200, 210),
            "eyes": (20, 20, 20),
            "beak": (240, 140, 40),
            "feet": (220, 120, 40),
        },
        "img_size": (36, 34),
    },
}


def blend_colors(base, tint, strength=0.4):
    return tuple(int(base[i] * (1 - strength) + tint[i] * strength) for i in range(3))


def adjust_saturation(color, multiplier):
    r, g, b = color
    gray = (r + g + b) // 3
    r = min(255, max(0, int(gray + (r - gray) * multiplier)))
    g = min(255, max(0, int(gray + (g - gray) * multiplier)))
    b = min(255, max(0, int(gray + (b - gray) * multiplier)))
    return (r, g, b)


def create_simple_sprite(animal, gender, egg_color, animation, frame, img_w, img_h, animal_colors):
    """Create a simple but recognizable sprite"""
    img = Image.new("RGBA", (img_w, img_h), (0, 0, 0, 0))

    egg_palette = EGG_PALETTES[egg_color]
    sat_boost = 1.3 if gender == "male" else 0.7

    colors = {}
    for name, base in animal_colors.items():
        tinted = blend_colors(base, egg_palette["body_tint"], 0.3)
        colors[name] = adjust_saturation(tinted, sat_boost)

    cx, cy = img_w // 2, img_h // 2

    def safe_put(x, y, c):
        if 0 <= x < img_w and 0 <= y < img_h:
            img.putpixel((x, y), (*c, 255))

    body = colors.get("body", (128, 128, 128))
    belly = colors.get("belly", (180, 180, 180))
    accent = colors.get("accent", (80, 80, 80))
    eyes = colors.get("eyes", (20, 20, 20))

    # Animation variations
    offset_y = 0
    if animation == "walk":
        offset_y = (frame % 2) * 1
    elif animation == "happy":
        offset_y = -frame * 2
    elif animation == "sad":
        offset_y = frame * 2
    elif animation == "sleep":
        offset_y = 2

    # Simple body shapes for each animal
    if animal == "rat":
        # Body
        for dy in range(-6, 8):
            for dx in range(-8, 8):
                if dx * dx / 36 + dy * dy / 25 < 1:
                    safe_put(cx + dx, cy + dy + offset_y, body)
        # Belly
        for dy in range(0, 4):
            for dx in range(-4, 4):
                if dy * dy / 4 + dx * dx / 12 < 1:
                    safe_put(cx + dx, cy + dy + 2 + offset_y, belly)
        # Eyes
        safe_put(cx - 4, cy - 2 + offset_y, eyes)
        safe_put(cx + 4, cy - 2 + offset_y, eyes)
        # Ears
        safe_put(cx - 6, cy - 6 + offset_y, accent)
        safe_put(cx + 6, cy - 6 + offset_y, accent)
        # Tail
        for i in range(6):
            safe_put(cx - 10 - i, cy + 2 + (i % 2), accent)

    elif animal == "fox":
        # Body
        for dy in range(-8, 10):
            for dx in range(-10, 10):
                if dx * dx / 50 + dy * dy / 36 < 1:
                    safe_put(cx + dx, cy + dy + offset_y, body)
        # Belly
        for dy in range(-2, 4):
            for dx in range(-5, 5):
                if dy * dy / 9 + dx * dx / 15 < 1:
                    safe_put(cx + dx, cy + dy + 4 + offset_y, belly)
        # Eyes
        safe_put(cx - 5, cy - 4 + offset_y, eyes)
        safe_put(cx + 5, cy - 4 + offset_y, eyes)
        # Nose
        safe_put(cx, cy + offset_y, (20, 20, 20))
        # Ears
        safe_put(cx - 7, cy - 8 + offset_y, accent)
        safe_put(cx + 7, cy - 8 + offset_y, accent)
        # Tail
        for i in range(8):
            safe_put(cx + 10 + i, cy + 2 - (i % 2) * 2 + offset_y, accent)

    elif animal == "crow":
        # Body
        for dy in range(-8, 8):
            for dx in range(-10, 10):
                if dx * dx / 50 + dy * dy / 25 < 1:
                    safe_put(cx + dx, cy + dy + offset_y, body)
        # Belly patch
        for dy in range(0, 4):
            for dx in range(-4, 4):
                safe_put(cx + dx, cy + dy + 2 + offset_y, belly)
        # Eyes
        safe_put(cx - 4, cy - 4 + offset_y, eyes)
        safe_put(cx + 4, cy - 4 + offset_y, eyes)
        # Beak
        for i in range(4):
            safe_put(cx + 8 + i, cy + offset_y, colors.get("beak", (50, 40, 30)))
            safe_put(cx + 8 + i, cy + 1 + offset_y, colors.get("beak", (50, 40, 30)))

    elif animal == "snake":
        # Wavy body
        for i in range(img_w - 8):
            y = cy + int((i + frame * 2) % 6) - 3
            safe_put(4 + i, y, body)
            safe_put(4 + i, y + 1, belly)
        # Head
        safe_put(4, cy - 1, body)
        safe_put(5, cy - 1, body)
        # Eyes
        safe_put(5, cy - 2, eyes)
        # Tongue
        if frame % 2 == 0:
            safe_put(img_w - 2, cy - 2, colors.get("tongue", (200, 50, 50)))

    elif animal == "deer":
        # Body
        for dy in range(-12, 12):
            for dx in range(-8, 8):
                if dx * dx / 36 + dy * dy / 60 < 1:
                    safe_put(cx + dx, cy + dy + offset_y, body)
        # Belly
        for dy in range(2, 8):
            for dx in range(-4, 4):
                safe_put(cx + dx, cy + dy + offset_y, belly)
        # Eyes
        safe_put(cx - 3, cy - 6 + offset_y, eyes)
        safe_put(cx + 3, cy - 6 + offset_y, eyes)
        # Nose
        safe_put(cx, cy + offset_y, colors.get("nose", (30, 20, 20)))
        # Antlers (male)
        if gender == "male":
            for i in range(6):
                safe_put(cx - 4, cy - 10 - i + offset_y, colors.get("antlers", (120, 90, 60)))
                safe_put(cx + 4, cy - 10 - i + offset_y, colors.get("antlers", (120, 90, 60)))

    elif animal == "frog":
        # Body (round)
        for dy in range(-8, 8):
            for dx in range(-8, 8):
                if dx * dx / 36 + dy * dy / 25 < 1:
                    safe_put(cx + dx, cy + dy + offset_y, body)
        # Belly
        for dy in range(-2, 4):
            for dx in range(-5, 5):
                safe_put(cx + dx, cy + dy + 2 + offset_y, belly)
        # Eyes (bulging)
        safe_put(cx - 4, cy - 5 + offset_y, (255, 255, 255))
        safe_put(cx + 4, cy - 5 + offset_y, (255, 255, 255))
        safe_put(cx - 4, cy - 4 + offset_y, eyes)
        safe_put(cx + 4, cy - 4 + offset_y, eyes)

    elif animal == "pigeon":
        # Body
        for dy in range(-8, 8):
            for dx in range(-8, 8):
                if dx * dx / 36 + dy * dy / 25 < 1:
                    safe_put(cx + dx, cy + dy + offset_y, body)
        # Belly
        for dy in range(0, 4):
            for dx in range(-4, 4):
                safe_put(cx + dx, cy + dy + 2 + offset_y, belly)
        # Neck
        for dy in range(-6, 0):
            for dx in range(-2, 2):
                safe_put(cx + dx, cy + dy + offset_y, colors.get("neck", (80, 100, 120)))
        # Eyes
        safe_put(cx - 3, cy - 4 + offset_y, eyes)
        safe_put(cx + 3, cy - 4 + offset_y, eyes)
        # Beak
        safe_put(cx + 6, cy - 2 + offset_y, colors.get("beak", (40, 35, 30)))

    elif animal == "raccoon":
        # Body
        for dy in range(-8, 8):
            for dx in range(-8, 8):
                if dx * dx / 36 + dy * dy / 25 < 1:
                    safe_put(cx + dx, cy + dy + offset_y, body)
        # Belly
        for dy in range(0, 4):
            for dx in range(-4, 4):
                safe_put(cx + dx, cy + dy + 2 + offset_y, belly)
        # Mask
        for dy in range(-2, 2):
            for dx in range(-5, 5):
                safe_put(cx + dx, cy + dy - 2 + offset_y, colors.get("mask", (20, 20, 20)))
        # Eyes
        safe_put(cx - 3, cy - 2 + offset_y, (255, 255, 255))
        safe_put(cx + 3, cy - 2 + offset_y, (255, 255, 255))
        safe_put(cx - 3, cy - 1 + offset_y, eyes)
        safe_put(cx + 3, cy - 1 + offset_y, eyes)

    elif animal == "squirrel":
        # Body
        for dy in range(-10, 8):
            for dx in range(-6, 6):
                if dx * dx / 16 + dy * dy / 36 < 1:
                    safe_put(cx + dx, cy + dy + offset_y, body)
        # Belly
        for dy in range(-2, 2):
            for dx in range(-3, 3):
                safe_put(cx + dx, cy + dy + 2 + offset_y, belly)
        # Eyes
        safe_put(cx - 2, cy - 4 + offset_y, eyes)
        safe_put(cx + 2, cy - 4 + offset_y, eyes)
        # Nose
        safe_put(cx, cy - 2 + offset_y, colors.get("nose", (20, 20, 20)))
        # Tail (fluffy)
        for i in range(8):
            for j in range(4):
                safe_put(cx + 6 + j, cy - 8 + i + (j % 2), colors.get("tail", (180, 120, 60)))

    elif animal == "goose":
        # Body
        for dy in range(-10, 10):
            for dx in range(-10, 10):
                if dx * dx / 50 + dy * dy / 40 < 1:
                    safe_put(cx + dx, cy + dy + offset_y, body)
        # Belly
        for dy in range(2, 8):
            for dx in range(-5, 5):
                safe_put(cx + dx, cy + dy + offset_y, belly)
        # Neck
        for dy in range(-10, -2):
            safe_put(cx, cy + dy + offset_y, body)
        # Eyes
        safe_put(cx - 3, cy - 8 + offset_y, eyes)
        safe_put(cx + 3, cy - 8 + offset_y, eyes)
        # Beak
        for i in range(4):
            safe_put(cx + 5 + i, cy - 6 + offset_y, colors.get("beak", (240, 140, 40)))
        # Knob (male)
        if gender == "male":
            safe_put(cx, cy - 11 + offset_y, colors.get("beak", (240, 140, 40)))

    # Condition effects
    if animation == "sick":
        # Green tint overlay
        for y in range(img_h):
            for x in range(img_w):
                p = img.getpixel((x, y))
                if p[3] > 0:
                    r = min(255, p[0] + 30)
                    g = min(255, p[1] + 20)
                    img.putpixel((x, y), (r, g, p[2], p[3]))

    elif animation == "bathe":
        # Water drops
        for i in range(frame + 1):
            safe_put(4 + i * 3, img_h - 2, (150, 200, 255))

    return img


def generate_sprites(output_dir):
    """Generate all sprites"""
    animals = list(ANIMALS.keys())
    genders = ["male", "female"]
    egg_colors = list(EGG_PALETTES.keys())
    animations = ["idle", "walk", "eat", "happy", "sad", "sleep", "sick", "bathe"]

    sprites_dir = os.path.join(output_dir, "sprites")
    os.makedirs(sprites_dir, exist_ok=True)

    total = 0

    for animal in animals:
        config = ANIMALS[animal]
        animal_colors = config["colors"]

        for gender in genders:
            gender_size = config["sizes"][gender]
            img_w, img_h = config["img_size"]

            for egg_color in egg_colors:
                animal_dir = os.path.join(sprites_dir, animal, gender, egg_color)
                os.makedirs(animal_dir, exist_ok=True)

                for anim in animations:
                    frame_count = 4 if anim in ["idle", "walk", "happy", "sick", "bathe"] else 2
                    if anim == "eat":
                        frame_count = 4

                    for frame in range(frame_count):
                        sprite = create_simple_sprite(
                            animal, gender, egg_color, anim, frame, img_w, img_h, animal_colors
                        )

                        filename = f"{anim}_{frame:02d}.png"
                        sprite.save(os.path.join(animal_dir, filename))
                        total += 1

    print(f"Generated {total} sprites in {sprites_dir}")
    return sprites_dir


if __name__ == "__main__":
    import sys

    output_dir = sys.argv[1] if len(sys.argv) > 1 else "wevito-godot"
    generate_sprites(output_dir)
