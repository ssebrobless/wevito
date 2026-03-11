extends Resource
class_name PetData

@export var name: String = "Wevito"
@export var animal_type: String = "rat"
@export var egg_color: String = "blue"
@export var gender: String = "male"
@export var stage: int = 0  # 0=egg, 1=baby, 2=teen, 3=adult

# Stats (0-100)
@export var hunger: float = 100.0
@export var hydration: float = 100.0
@export var happiness: float = 100.0
@export var energy: float = 100.0
@export var health: float = 100.0
@export var cleanliness: float = 100.0
@export var affection: float = 100.0
@export var grooming: float = 100.0
@export var fitness: float = 100.0

# Personality (-100 to 100)
@export var food_love: float = 0.0
@export var cuddle_need: float = 0.0
@export var pet_cleanliness: float = 0.0
@export var activity_level: float = 0.0
@export var cheerfulness: float = 0.0
@export var social_need: float = 0.0
@export var playfulness: float = 0.0
@export var stubbornness: float = 0.0

# State
@export var age_minutes: int = 0
@export var is_dead: bool = false
@export var is_sleeping: bool = false
@export var is_hatching: bool = true
@export var is_naming_pending: bool = false
@export var emotion: String = "hatching"

# Death info for In Memoriam
@export var death_sprite_path: String = ""
@export var age_at_death: int = 0

# Water bowl
@export var water_bowl_level: float = 100.0
@export var water_bowl_capacity: float = 100.0

# Conditions - stored as {condition_id: severity} where severity 1-3
@export var conditions: Dictionary = {}
@export var active_treatments: Array[Dictionary] = []

# Movement/wandering
@export var position: Vector2 = Vector2(160, 100)
@export var target_position: Vector2 = Vector2(160, 100)
@export var is_wandering: bool = false
@export var wander_timer: float = 0.0
@export var animation_current: String = "idle"

# Base stats for animals
const ANIMAL_STATS = {
	"rat": {"speed": 80.0, "wander_chance": 0.7},
	"crow": {"speed": 60.0, "wander_chance": 0.5},
	"fox": {"speed": 70.0, "wander_chance": 0.5},
	"snake": {"speed": 30.0, "wander_chance": 0.2},
	"deer": {"speed": 40.0, "wander_chance": 0.3},
	"frog": {"speed": 50.0, "wander_chance": 0.5},
	"pigeon": {"speed": 55.0, "wander_chance": 0.6},
	"raccoon": {"speed": 60.0, "wander_chance": 0.5},
	"squirrel": {"speed": 75.0, "wander_chance": 0.7},
	"goose": {"speed": 45.0, "wander_chance": 0.3}
}

func get_animal_base_stats() -> Dictionary:
	return ANIMAL_STATS.get(animal_type, {"speed": 50.0, "wander_chance": 0.5})

func get_movement_speed() -> float:
	var stats = get_animal_base_stats()
	var speed = stats["speed"]
	# Males move slightly faster
	if gender == "male":
		speed *= 1.1
	else:
		speed *= 0.9
	return speed

func get_wander_chance() -> float:
	var stats = get_animal_base_stats()
	var chance = stats["wander_chance"]
	# Males wander more
	if gender == "male":
		chance *= 1.15
	else:
		chance *= 0.85
	return clamp(chance, 0.1, 0.9)

func get_stage_name() -> String:
	match stage:
		0: return "Egg"
		1: return "Baby"
		2: return "Teen"
		3: return "Adult"
		4: return "Aging"
		_: return "Adult"

func get_stage_from_age(age_mins: int) -> int:
	if age_mins < 10:
		return 0  # Egg (first 10 minutes)
	elif age_mins < 60:
		return 1  # Baby (10-60 minutes)
	elif age_mins < 240:
		return 2  # Teen (1-4 hours)
	elif age_mins < 480:
		return 3  # Adult (4-8 hours)
	else:
		return 4  # Aging (8+ hours)

func update_stage():
	stage = get_stage_from_age(age_minutes)
