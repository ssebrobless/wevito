#!/usr/bin/env python3
"""Generate pixel art icons for Wevito UI"""

from PIL import Image
import os

# Output directory
OUTPUT_DIR = "C:/users/fishe/wevito-new/wevito-godot/sprites/icons"

# Icon definitions: (name, color, shape_type)
# Colors are RGB tuples
ICONS = [
    # Action buttons
    ("feed", (200, 100, 100), "meat"),
    ("pet", (150, 150, 200), "hand"),
    ("rest", (100, 100, 180), "sleep"),
    ("groom", (180, 180, 120), "brush"),
    ("bathe", (100, 180, 200), "water"),
    ("exercise", (120, 180, 120), "run"),
    ("medicine", (200, 100, 200), "pill"),
    # System buttons
    ("save", (150, 150, 150), "disk"),
    ("settings", (120, 120, 120), "gear"),
    ("ghost", (180, 180, 200), "ghost"),
    ("add", (100, 200, 100), "plus"),
    ("minimize", (80, 80, 80), "minus"),
    ("doctor", (255, 255, 255), "note"),
    # Food items
    ("food_plant", (100, 180, 100), "leaf"),
    ("food_meat", (180, 80, 80), "meat"),
    ("food_sweet", (255, 150, 200), "candy"),
    ("food_salty", (200, 200, 150), "salt"),
    ("water", (100, 150, 255), "droplet"),
    ("water_bowl", (150, 180, 220), "bowl"),
    ("memoriam", (255, 180, 200), "heart_wings"),
]


def create_icon(name, color, shape_type, size=16):
    """Create a simple pixel art icon"""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    pixels = img.load()

    # Fill based on shape type
    for y in range(size):
        for x in range(size):
            include = False

            if shape_type == "meat":
                # Meat shape (rough oval)
                cx, cy = size // 2, size // 2
                if ((x - cx) ** 2 / 25 + (y - cy) ** 2 / 16) < 1:
                    include = True

            elif shape_type == "leaf":
                # Leaf shape
                if y > 2 and y < size - 2 and x > 2 and x < size - 2:
                    if abs(x - size // 2) < (size - y) // 2:
                        include = True

            elif shape_type == "candy":
                # Candy/swirl shape
                cx, cy = size // 2, size // 2
                d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
                if d < size // 2 - 1:
                    include = True

            elif shape_type == "salt":
                # Salt shaker shape
                if x > 4 and x < size - 4 and y > 3 and y < size - 3:
                    include = True

            elif shape_type == "droplet":
                # Water droplet
                cx = size // 2
                if y < size // 2:
                    if abs(x - cx) < (size // 2 - y) // 2:
                        include = True
                else:
                    if abs(x - cx) < ((size - y) * 2):
                        include = True

            elif shape_type == "bowl":
                # Bowl shape
                cx = size // 2
                if y > size // 2 - 2:
                    if abs(x - cx) < (size // 2 - (y - size // 2)):
                        include = True

            elif shape_type == "pill":
                # Pill shape (capsule)
                cx, cy = size // 2, size // 2
                if y < size // 2:
                    if ((x - cx) ** 2 + (y - cy + 2) ** 2) < 16:
                        include = True
                else:
                    if ((x - cx) ** 2 + (y - cy - 2) ** 2) < 16:
                        include = True

            elif shape_type == "hand":
                # Hand/paw
                if y > size - 6:
                    if abs(x - size // 2) < 4:
                        include = True
                elif y > 2 and abs(x - size // 2) < 3:
                    include = True

            elif shape_type == "sleep":
                # Sleep/zzz
                if (x == 3 and y == 4) or (x == 5 and y == 3) or (x == 7 and y == 2):
                    include = True
                if y > size - 5 and x > 2:
                    include = True

            elif shape_type == "brush":
                # Brush
                if y < 4:
                    if x > 5 and x < 10:
                        include = True
                elif y > 5 and x > 3 and x < 12:
                    include = True

            elif shape_type == "water":
                # Water drop
                cx = size // 2
                if y < size // 2:
                    if abs(x - cx) < (size // 2 - y) // 2 + 1:
                        include = True
                else:
                    if abs(x - cx) < 3:
                        include = True

            elif shape_type == "run":
                # Running figure
                if y == size - 4:
                    if x > 3 and x < 12:
                        include = True
                if (x == 4 and y == 8) or (x == 11 and y == 8):
                    include = True
                if (x == 2 and y == 5) or (x == 13 and y == 6):
                    include = True

            elif shape_type == "disk":
                # Floppy disk
                if x > 2 and x < size - 2 and y > 2 and y < size - 2:
                    include = True
                if y < 5 and x > 4 and x < 11:
                    include = True

            elif shape_type == "gear":
                # Gear
                cx = size // 2
                d = ((x - cx) ** 2 + (y - cx) ** 2) ** 0.5
                if d > 3 and d < 6:
                    include = True
                if x == cx and y > 2 and y < size - 2:
                    include = True
                if y == cx and x > 2 and x < size - 2:
                    include = True

            elif shape_type == "ghost":
                # Ghost
                cx = size // 2
                if y < size - 4:
                    if abs(x - cx) < 5 - y // 3:
                        include = True
                else:
                    if abs(x - cx) < 4:
                        include = True
                # Eyes
                if y == 6 and (x == 5 or x == 10):
                    include = False  # Make transparent for eyes

            elif shape_type == "plus":
                # Plus
                if x == size // 2 or y == size // 2:
                    if x > 3 and x < size - 3 and y > 3 and y < size - 3:
                        include = True

            elif shape_type == "minus":
                # Minus
                if y == size // 2 and x > 3 and x < size - 3:
                    include = True

            elif shape_type == "note":
                # Doctor note / clipboard
                if x > 3 and x < size - 3 and y > 1 and y < size - 2:
                    include = True
                if x > 6 and x < 9 and y < 4:
                    include = True

            elif shape_type == "heart_wings":
                # Heart with wings
                cx = size // 2
                cy = size // 2
                # Heart shape
                dx = x - cx
                dy = y - cy + 1
                if dx * dx + (dy * dy) * 0.8 < 12:
                    include = True
                # Left wing
                if x == 2 and y == 6:
                    include = True
                if x == 3 and y == 5:
                    include = True
                # Right wing
                if x == size - 3 and y == 6:
                    include = True
                if x == size - 4 and y == 5:
                    include = True

            else:
                # Default filled square
                if x > 2 and x < size - 2 and y > 2 and y < size - 2:
                    include = True

            if include:
                pixels[x, y] = color + (255,)

    return img


def main():
    # Create output directory
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    for name, color, shape in ICONS:
        img = create_icon(name, color, shape)
        filepath = os.path.join(OUTPUT_DIR, f"{name}.png")
        img.save(filepath)
        print(f"Created: {filepath}")

    print(f"\nGenerated {len(ICONS)} icons in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
