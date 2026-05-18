extends Node
class_name GameManager

const Pet = preload("res://scripts/pet.gd")
const PetData = preload("res://scripts/pet_data.gd")

const MAX_PETS = 3
const ANIMAL_TYPES = ["rat", "crow", "fox", "snake", "deer", "frog", "pigeon", "raccoon", "squirrel", "goose"]
const STARTER_EGGS_PATH := "res://vnext/content/starter_eggs.json"
const FALLBACK_STARTER_EGGS := [
	{"color": "red", "label": "Red egg", "hex": "#D94A42", "species": "fox", "enabled": true, "disabledReason": ""},
	{"color": "orange", "label": "Orange egg", "hex": "#E98635", "species": "squirrel", "enabled": true, "disabledReason": ""},
	{"color": "yellow", "label": "Yellow egg", "hex": "#E8C84E", "species": "goose", "enabled": true, "disabledReason": ""},
	{"color": "blue", "label": "Blue egg", "hex": "#4C8FE8", "species": "frog", "enabled": true, "disabledReason": ""},
	{"color": "indigo", "label": "Indigo egg", "hex": "#4B5BC8", "species": "crow", "enabled": true, "disabledReason": ""},
	{"color": "violet", "label": "Violet egg", "hex": "#8C58D8", "species": "raccoon", "enabled": true, "disabledReason": ""}
]

# Animal environments and innate conditions
const ANIMAL_DATA = {
	"rat": {"environment": "sewers", "innate_condition": "respiratoryProblems"},
	"crow": {"environment": "nest", "innate_condition": "parasites"},
	"fox": {"environment": "burrow", "innate_condition": "dentalProblems"},
	"snake": {"environment": "rocks", "innate_condition": "sheddingIssues"},
	"deer": {"environment": "forest", "innate_condition": "jointStiffness"},
	"frog": {"environment": "log", "innate_condition": "skinInfections"},
	"pigeon": {"environment": "ledge", "innate_condition": "viralSusceptibility"},
	"raccoon": {"environment": "trash", "innate_condition": "parasites"},
	"squirrel": {"environment": "tree", "innate_condition": "dentalOvergrowth"},
	"goose": {"environment": "lake", "innate_condition": "footProblems"}
}

const ENVIRONMENTS = {
	"sewers": {"bg": Color(0.1, 0.12, 0.14), "mid": Color(0.18, 0.2, 0.21), "ground": Color(0.06, 0.07, 0.08)},
	"nest": {"bg": Color(0.53, 0.81, 0.92), "mid": Color(0.18, 0.35, 0.15), "ground": Color(0.13, 0.3, 0.1)},
	"burrow": {"bg": Color(0.42, 0.33, 0.27), "mid": Color(0.55, 0.45, 0.33), "ground": Color(0.29, 0.22, 0.16)},
	"rocks": {"bg": Color(0.24, 0.29, 0.18), "mid": Color(0.33, 0.42, 0.18), "ground": Color(0.18, 0.23, 0.14)},
	"forest": {"bg": Color(0.12, 0.3, 0.1), "mid": Color(0.13, 0.55, 0.13), "ground": Color(0.08, 0.19, 0.06)},
	"log": {"bg": Color(0.29, 0.22, 0.16), "mid": Color(0.4, 0.26, 0.13), "ground": Color(0.24, 0.16, 0.08)},
	"ledge": {"bg": Color(0.35, 0.42, 0.48), "mid": Color(0.44, 0.5, 0.56), "ground": Color(0.29, 0.35, 0.42)},
	"trash": {"bg": Color(0.1, 0.12, 0.14), "mid": Color(0.18, 0.2, 0.21), "ground": Color(0.06, 0.07, 0.08)},
	"tree": {"bg": Color(0.56, 0.93, 0.56), "mid": Color(0.13, 0.55, 0.13), "ground": Color(0.55, 0.27, 0.07)},
	"lake": {"bg": Color(0.27, 0.51, 0.71), "mid": Color(0.35, 0.61, 0.77), "ground": Color(0.56, 0.74, 0.56)}
}

# Target positions for each environment (where pet goes when performing actions)
const ENVIRONMENT_POSITIONS = {
	"sewers": Vector2(30, 290),
	"nest": Vector2(160, 250),
	"burrow": Vector2(280, 290),
	"rocks": Vector2(50, 280),
	"forest": Vector2(270, 280),
	"log": Vector2(150, 290),
	"ledge": Vector2(160, 240),
	"trash": Vector2(40, 290),
	"tree": Vector2(160, 220),
	"lake": Vector2(200, 290)
}

const HABITAT_LOADOUTS_PATH := "res://vnext/content/habitat_loadouts.json"
const HABITAT_MANIFEST_STAGE_SIZE := Vector2(400.0, 240.0)
const HABITAT_FOOD_ASSETS := ["snack_bowl", "seed_tray", "bug_treat"]
const HABITAT_DRINK_ASSETS := ["pond_dish", "shallow_water_dish", "water_bowl"]
const HABITAT_PLAY_ASSETS := ["ball", "bell_toy", "branch_perch", "leaf_pile", "rope_toy"]

var pets: Array[Pet] = []
var pet_datas: Array[PetData] = []
var active_pet_index: int = 0

# In Memoriam - list of deceased pets
var in_memoriam: Array[PetData] = []

# Auto-save timer
var auto_save_timer: float = 0.0

# Runtime action state trackers (per pet index)
var forage_state: Dictionary = {}
var workout_heat: Dictionary = {}
var habitat_loadouts: Dictionary = {}
var starter_eggs: Array = []
var critical_threshold_notified: Dictionary = {}

# Game settings
var tick_rate: float = 60.0  # 1 tick per second for testing
var tick_timer: float = 0.0

# Decay rates per tick
var DECAY_RATES = {
	"hunger": 0.2,
	"hydration": 0.25,
	"happiness": 0.15,
	"energy": 0.1,
	"health": 0.1,
	"cleanliness": 0.15,
	"affection": 0.1,
	"grooming": 0.15,
	"fitness": 0.1
}

# Stat damage thresholds
var CRITICAL_THRESHOLD = 20.0
var WARNING_THRESHOLD = 40.0

# Action balance tuning
const TUNE_SLEEP_MULT := 1.0
const TUNE_FORAGE_FIND_MULT := 1.0
const TUNE_WORKOUT_INJURY_MULT := 1.0

const SLEEP_ENERGY_REGEN := 5.0
const SLEEP_HEALTH_REGEN := 1.5
const SLEEP_HUNGER_DECAY := 0.5
const SLEEP_HYDRATION_DECAY := 0.5

const FORAGE_HUNGER_COST := 2.0
const FORAGE_ENERGY_COST := 2.5
const FORAGE_HYDRATION_COST := 1.5
const FORAGE_FITNESS_GAIN := 1.0
const FORAGE_BASE_FIND_CHANCE := 0.36
const FORAGE_LOW_HUNGER_FIND_CHANCE := 0.22
const FORAGE_CRITICAL_HUNGER_FIND_CHANCE := 0.1
const FORAGE_HUNGER_REWARD := 24.0
const FORAGE_HAPPINESS_REWARD := 5.0

signal stats_updated()
signal pet_state_changed(state: String)
signal pet_died(pet_data: PetData)
signal pet_added(pet_index: int)
signal active_pet_changed(pet_index: int)
signal naming_needed(pet_index: int)
signal pet_critical_threshold_crossed(pet_index: int, stat_name: String, stat_value: float)

func _ready():
	starter_eggs = _load_starter_eggs()
	_load_habitat_loadouts()

func get_starter_egg_options() -> Array:
	if starter_eggs.is_empty():
		starter_eggs = _load_starter_eggs()
	return starter_eggs.duplicate(true)

func get_enabled_starter_egg_colors() -> Array:
	var colors: Array = []
	for egg in get_starter_egg_options():
		if bool(egg.get("enabled", true)):
			colors.append(str(egg.get("color", "")))
	return colors

func _load_starter_eggs() -> Array:
	if not FileAccess.file_exists(STARTER_EGGS_PATH):
		return FALLBACK_STARTER_EGGS.duplicate(true)
	var file = FileAccess.open(STARTER_EGGS_PATH, FileAccess.READ)
	if file == null:
		return FALLBACK_STARTER_EGGS.duplicate(true)
	var parsed = JSON.parse_string(file.get_as_text())
	if parsed is Dictionary and parsed.has("eggs") and parsed["eggs"] is Array:
		return parsed["eggs"]
	return FALLBACK_STARTER_EGGS.duplicate(true)

func _resolve_starter_egg(egg_color: String) -> Dictionary:
	for egg in get_starter_egg_options():
		if str(egg.get("color", "")).to_lower() == egg_color.to_lower():
			return egg
	return {}

func _resolve_starter_egg_species(egg_color: String) -> String:
	var egg = _resolve_starter_egg(egg_color)
	if egg.has("species"):
		return str(egg["species"])
	return ANIMAL_TYPES.pick_random()

func _pick_enabled_starter_egg_color() -> String:
	var colors = get_enabled_starter_egg_colors()
	if colors.is_empty():
		return "red"
	return colors.pick_random()

func get_active_pet() -> Pet:
	if pets.size() > 0 and active_pet_index < pets.size():
		return pets[active_pet_index]
	return null

func get_active_pet_data() -> PetData:
	if pet_datas.size() > 0 and active_pet_index < pet_datas.size():
		return pet_datas[active_pet_index]
	return null

func get_pet_count() -> int:
	return pets.size()

func can_add_pet() -> bool:
	return pets.size() < MAX_PETS

func add_pet(pet_node: Pet) -> int:
	if not can_add_pet():
		return -1
	
	var new_index = pets.size()
	pets.append(pet_node)
	
	# Create new pet data with random enabled egg/gender; the egg now owns species.
	var pd = PetData.new()
	pd.name = "Wevito"
	pd.egg_color = _pick_enabled_starter_egg_color()
	pd.animal_type = _resolve_starter_egg_species(pd.egg_color)
	pd.gender = "male" if randf() > 0.5 else "female"
	pd.is_hatching = true
	pd.position = Vector2(80 + new_index * 100, 280)
	
	pet_datas.append(pd)
	pet_node.setup(pd)
	
	# Start hatching for this pet
	start_hatching(new_index, 3.0)
	
	pet_added.emit(new_index)
	stats_updated.emit()
	
	return new_index

func add_pet_with_color(pet_node: Pet, egg_color: String) -> int:
	if not can_add_pet():
		return -1
	
	var new_index = pets.size()
	pets.append(pet_node)
	
	var egg = _resolve_starter_egg(egg_color)
	if egg.is_empty() or not bool(egg.get("enabled", true)):
		pets.erase(pet_node)
		return -1

	# Create new pet data with selected color and deterministic hidden species.
	var pd = PetData.new()
	pd.name = "Wevito"
	pd.egg_color = egg_color
	pd.animal_type = str(egg.get("species", ANIMAL_TYPES.pick_random()))
	pd.gender = "male" if randf() > 0.5 else "female"
	pd.is_hatching = true
	pd.position = Vector2(80 + new_index * 100, 280)
	
	pet_datas.append(pd)
	pet_node.setup(pd)
	
	# Start hatching for this pet
	start_hatching(new_index, 3.0)
	
	pet_added.emit(new_index)
	stats_updated.emit()
	
	return new_index

func start_hatching(pet_index: int, duration: float):
	if pet_index < 0 or pet_index >= pet_datas.size():
		return
	pet_datas[pet_index].is_hatching = true
	await get_tree().create_timer(duration).timeout
	finish_hatching(pet_index)

func finish_hatching(pet_index: int):
	if pet_index < 0 or pet_index >= pet_datas.size():
		return
	
	var pd = pet_datas[pet_index]
	if not pd.is_hatching:
		return
	pd.is_hatching = false
	pd.is_sleeping = false
	pd.emotion = "happy"
	pd.hunger = 100.0
	pd.hydration = 100.0
	pd.happiness = 100.0
	pd.energy = 100.0
	pd.health = 100.0
	pd.cleanliness = 100.0
	pd.affection = 100.0
	pd.grooming = 100.0
	pd.fitness = 100.0
	pd.water_bowl_level = 100.0
	
	# Assign innate condition based on animal type (with severity)
	var innate_cond = get_animal_innate_condition(pd.animal_type)
	if innate_cond != "" and not pd.conditions.has(innate_cond):
		pd.conditions[innate_cond] = 1  # Mild severity
	
	# Assign random personality traits
	_assign_personality(pd)
	
	# Trigger naming popup
	pd.is_naming_pending = true
	naming_needed.emit(pet_index)
	
	stats_updated.emit()

func _assign_personality(pd: PetData):
	# Generate random personality between -50 and 50 with bias toward 0
	pd.food_love = randf_range(-50, 50)
	pd.cuddle_need = randf_range(-50, 50)
	pd.pet_cleanliness = randf_range(-50, 50)
	pd.activity_level = randf_range(-50, 50)
	pd.cheerfulness = randf_range(-50, 50)
	pd.social_need = randf_range(-50, 50)
	pd.playfulness = randf_range(-50, 50)
	pd.stubbornness = randf_range(-50, 50)

func set_active_pet(index: int):
	if index >= 0 and index < pets.size():
		active_pet_index = index
		active_pet_changed.emit(active_pet_index)
		stats_updated.emit()

func next_pet():
	if pets.size() > 0:
		active_pet_index = (active_pet_index + 1) % pets.size()
		active_pet_changed.emit(active_pet_index)
		stats_updated.emit()

func previous_pet():
	if pets.size() > 0:
		active_pet_index = (active_pet_index - 1 + pets.size()) % pets.size()
		active_pet_changed.emit(active_pet_index)
		stats_updated.emit()

func _process(delta):
	# Advance shared world tick, then process all pets together.
	tick_timer += delta
	while tick_timer >= tick_rate:
		tick_timer -= tick_rate
		for i in range(pets.size()):
			process_tick(i)

func process_tick(pet_index: int):
	if pet_index >= pet_datas.size():
		return
	
	var pd = pet_datas[pet_index]
	if pd == null or pd.is_dead or pd.is_hatching:
		return
	
	# Increment age
	pd.age_minutes += 1
	
	# Update life stage
	pd.update_stage()
	
	# Apply stat decay (if not sleeping)
	if not pd.is_sleeping:
		apply_decay(pet_index)
	else:
		_apply_sleep_regen(pet_index)

	_process_forage_tick(pet_index)
	_decay_workout_heat(pet_index)
	
	# Check for conditions
	check_conditions(pet_index)
	
	# Check for death
	check_death(pet_index)
	
	# Update emotion
	update_emotion(pet_index)
	_emit_threshold_notifications(pet_index)
	
	# Emit update signal
	stats_updated.emit()

func _emit_threshold_notifications(pet_index: int):
	if pet_index >= pet_datas.size():
		return
	var pd = pet_datas[pet_index]
	var criticals = {
		"hunger": pd.hunger,
		"hydration": pd.hydration,
		"energy": pd.energy,
		"health": pd.health
	}
	for stat_name in criticals.keys():
		var value = float(criticals[stat_name])
		var key = str(pet_index) + ":" + stat_name
		if value < CRITICAL_THRESHOLD:
			if not bool(critical_threshold_notified.get(key, false)):
				critical_threshold_notified[key] = true
				pet_critical_threshold_crossed.emit(pet_index, stat_name, value)
		else:
			critical_threshold_notified.erase(key)

func _apply_sleep_regen(pet_index: int):
	if pet_index >= pet_datas.size():
		return
	var pd = pet_datas[pet_index]
	pd.energy = min(100.0, pd.energy + (SLEEP_ENERGY_REGEN * TUNE_SLEEP_MULT))
	pd.health = min(100.0, pd.health + (SLEEP_HEALTH_REGEN * TUNE_SLEEP_MULT))
	pd.hunger = max(0.0, pd.hunger - (SLEEP_HUNGER_DECAY / max(0.5, TUNE_SLEEP_MULT)))
	pd.hydration = max(0.0, pd.hydration - (SLEEP_HYDRATION_DECAY / max(0.5, TUNE_SLEEP_MULT)))

func _process_forage_tick(pet_index: int):
	if not forage_state.has(pet_index):
		return
	if pet_index >= pet_datas.size():
		forage_state.erase(pet_index)
		return

	var pd = pet_datas[pet_index]
	var pet = pets[pet_index] if pet_index < pets.size() else null

	# Foraging counts as exercise and costs resources while searching.
	pd.hunger = max(0.0, pd.hunger - FORAGE_HUNGER_COST)
	pd.energy = max(0.0, pd.energy - FORAGE_ENERGY_COST)
	pd.hydration = max(0.0, pd.hydration - FORAGE_HYDRATION_COST)
	pd.fitness = min(100.0, pd.fitness + FORAGE_FITNESS_GAIN)

	if pet and pet.has_method("start_wandering"):
		pet.start_wandering()

	var data = forage_state[pet_index]
	data["ticks_left"] = int(data.get("ticks_left", 0)) - 1

	# Risky when very hungry: reduced find chance.
	var find_chance = FORAGE_BASE_FIND_CHANCE
	if pd.hunger < 30:
		find_chance = FORAGE_LOW_HUNGER_FIND_CHANCE
	if pd.hunger < 15:
		find_chance = FORAGE_CRITICAL_HUNGER_FIND_CHANCE

	find_chance = clamp(find_chance * TUNE_FORAGE_FIND_MULT, 0.02, 0.95)

	if randf() < find_chance:
		pd.hunger = min(100.0, pd.hunger + FORAGE_HUNGER_REWARD)
		pd.happiness = min(100.0, pd.happiness + FORAGE_HAPPINESS_REWARD)
		forage_state.erase(pet_index)
		pet_state_changed.emit("forage_success")
		return

	if data["ticks_left"] <= 0:
		forage_state.erase(pet_index)
		pet_state_changed.emit("forage_failed")
		return

	forage_state[pet_index] = data

func _decay_workout_heat(pet_index: int):
	if not workout_heat.has(pet_index):
		return
	workout_heat[pet_index] = max(0, int(workout_heat[pet_index]) - 1)

func apply_decay(pet_index: int):
	if pet_index >= pet_datas.size():
		return
	
	var pd = pet_datas[pet_index]
	var personality_mods = get_personality_modifiers(pet_index)
	var condition_mods = get_condition_modifiers(pet_index)
	
	for stat in DECAY_RATES.keys():
		var decay = DECAY_RATES[stat]
		
		# Apply personality/life stage modifiers
		match stat:
			"energy":
				decay *= (1.0 + personality_mods.get("energy_decay", 0.0))
			"cleanliness":
				decay *= (1.0 + personality_mods.get("cleanliness_decay", 0.0))
			"affection":
				decay *= (1.0 + personality_mods.get("affection_decay", 0.0))
			"happiness":
				decay *= (1.0 + personality_mods.get("happiness_decay", 0.0))
			"hunger":
				decay *= (1.0 + personality_mods.get("hunger_decay", 0.0))
			"fitness":
				decay *= (1.0 + personality_mods.get("fitness_decay", 0.0))
		
		# Apply condition modifiers
		decay *= condition_mods.get("all_decay", 1.0)
		
		# Apply decay
		var value = pd.get(stat)
		pd.set(stat, max(0.0, value - decay))
		
		# Check for health damage from low stats
		if value < CRITICAL_THRESHOLD:
			pd.health -= 0.1
	
	# Auto-drink: when hydration < 80%, chance to drink scales with need
	if pd.hydration < 80 and pd.water_bowl_level > 0 and not pd.is_sleeping:
		var thirst_urgency = (80.0 - pd.hydration) / 80.0  # 0 at 80%, 1 at 0%
		var drink_chance = 0.05 + (thirst_urgency * 0.75)  # 5% at 80%, 80% at 0%
		if randf() < drink_chance:
			_apply_auto_drink(pet_index)

func _apply_auto_drink(pet_index: int) -> bool:
	if pet_index >= pet_datas.size():
		return false
	var pd = pet_datas[pet_index]
	if pd == null or pd.is_dead or pd.is_hatching or pd.is_sleeping:
		return false
	if pd.hydration >= 80 or pd.water_bowl_level <= 0:
		return false

	pd.hydration = min(100.0, pd.hydration + 20)
	pd.water_bowl_level = max(0.0, pd.water_bowl_level - 5)
	if pet_index < pets.size() and pets[pet_index]:
		var pet = pets[pet_index]
		if pet.has_method("request_auto_action"):
			pet.request_auto_action("drink")
		else:
			pet.perform_action("drink")
	return true

func force_auto_drink_for_test(pet_index: int) -> bool:
	return _apply_auto_drink(pet_index)

func get_personality_modifiers(pet_index: int) -> Dictionary:
	if pet_index >= pet_datas.size():
		return {}
	
	var pd = pet_datas[pet_index]
	var mods = {
		"energy_decay": pd.activity_level / 500.0,
		"cleanliness_decay": pd.pet_cleanliness / 500.0,
		"affection_decay": pd.cuddle_need / 500.0,
		"happiness_decay": -pd.cheerfulness / 500.0,
		"hunger_decay": -pd.food_love / 500.0,
		"fitness_decay": pd.stubbornness / 500.0,
		"wander_multiplier": 1.0,
		"speed_multiplier": 1.0
	}
	
	# Life stage modifiers
	match pd.stage:
		1:  # Baby - developing preferences
			mods["wander_multiplier"] = 0.7
			mods["speed_multiplier"] = 0.8
		2:  # Teen - more active
			mods["wander_multiplier"] = 1.5
			mods["speed_multiplier"] = 1.2
			mods["hunger_decay"] += 0.3  # Extra hungry
			mods["happiness_decay"] += 0.3  # Extra need for entertainment
		3:  # Adult - set personality
			mods["wander_multiplier"] = 1.0
			mods["speed_multiplier"] = 1.0
		4:  # Aging - slower, more affectionate
			mods["wander_multiplier"] = 0.5
			mods["speed_multiplier"] = 0.8
			mods["energy_decay"] += 0.2  # Gets tired faster
			mods["affection_decay"] -= 0.3  # Needs more love
	
	return mods

func get_condition_modifiers(pet_index: int) -> Dictionary:
	if pet_index >= pet_datas.size():
		return {"all_decay": 1.0}
	
	var pd = pet_datas[pet_index]
	var mods = {"all_decay": 1.0}
	
	for condition in pd.conditions.keys():
		match condition:
			"depression":
				mods["all_decay"] *= 1.2
			"anxiety":
				mods["affection_decay"] = mods.get("affection_decay", 0.0) + 0.3
	
	return mods

func check_conditions(pet_index: int):
	if pet_index >= pet_datas.size():
		return
	
	var pd = pet_datas[pet_index]
	
	# Check for acquired conditions based on low stats (with severity)
	# Severity: 1=mild, 2=moderate, 3=severe
	if pd.hunger > 85 and not pd.conditions.has("obesity"):
		pd.conditions["obesity"] = 1 + int(pd.hunger > 95)
	elif pd.hunger < 25 and not pd.conditions.has("malnutrition"):
		pd.conditions["malnutrition"] = 1 + int(pd.hunger < 15)
	
	if pd.happiness < 20 and not pd.conditions.has("depression"):
		pd.conditions["depression"] = 1 + int(pd.happiness < 10)
	
	if pd.affection < 20 and not pd.conditions.has("anxiety"):
		pd.conditions["anxiety"] = 1 + int(pd.affection < 10)
	
	if pd.cleanliness < 20 and not pd.conditions.has("skinInfection"):
		pd.conditions["skinInfection"] = 1 + int(pd.cleanliness < 10)
	
	if pd.fitness < 20 and not pd.conditions.has("jointPain"):
		pd.conditions["jointPain"] = 1 + int(pd.fitness < 10)
	
	if pd.energy < 15 and not pd.conditions.has("exhaustion"):
		pd.conditions["exhaustion"] = 1 + int(pd.energy < 8)

func check_death(pet_index: int):
	if pet_index >= pet_datas.size():
		return
	
	var pd = pet_datas[pet_index]
	if pd == null or pd.is_dead:
		return
	
	# Death from health
	if pd.health <= 0:
		_mark_pet_dead(pet_index)
		return
	
	# Death from starvation
	if pd.hunger <= 0:
		pd.health = 0
		_mark_pet_dead(pet_index)
		return

	# Death from dehydration
	if pd.hydration <= 0:
		pd.health = 0
		_mark_pet_dead(pet_index)
		return

func _mark_pet_dead(pet_index: int):
	if pet_index >= pet_datas.size():
		return
	var pd = pet_datas[pet_index]
	if pd == null:
		return
	pd.is_dead = true
	pd.is_ghost = false
	pd.death_elapsed_sec = 0.0
	pd.age_at_death = pd.age_minutes
	pd.memorial_object_id = "memorial_object"
	pd.memorial_position = pd.position
	pd.memorial_expires_at = int(Time.get_unix_time_from_system()) + 86400
	# Store sprite info for In Memoriam.
	if pets.size() > pet_index and pets[pet_index] != null:
		var pet = pets[pet_index]
		pd.death_sprite_path = pet.get_current_sprite_path()
	_add_to_in_memoriam(pd)
	pet_died.emit(pd)

func _add_to_in_memoriam(pd: PetData):
	in_memoriam.append(pd)
	# Keep max 10
	while in_memoriam.size() > 10:
		in_memoriam.pop_front()

func update_emotion(pet_index: int):
	if pet_index >= pet_datas.size():
		return
	
	var pd = pet_datas[pet_index]
	
	if pd.is_dead:
		pd.emotion = "dead"
	elif pd.is_hatching:
		pd.emotion = "hatching"
	elif pd.is_sleeping:
		pd.emotion = "sleeping"
	elif pd.conditions.has("depression"):
		pd.emotion = "sad"
	elif pd.conditions.has("anxiety"):
		pd.emotion = "nervous"
	elif pd.conditions.has("exhaustion") or pd.energy < 30:
		pd.emotion = "exhausted"
	elif pd.hunger < 25:
		pd.emotion = "hungry"
	elif pd.cleanliness < 25:
		pd.emotion = "dirty"
	elif pd.affection < 25:
		pd.emotion = "lonely"
	elif pd.fitness < 25:
		pd.emotion = "tired"
	elif pd.happiness < 30:
		pd.emotion = "sad"
	elif pd.happiness > 75:
		pd.emotion = "happy"
	else:
		pd.emotion = "neutral"
	
	pet_state_changed.emit(pd.emotion)

func perform_action(action: String) -> Dictionary:
	var pd = get_active_pet_data()
	var pet = get_active_pet()
	var pet_index = active_pet_index
	
	if pd == null or pet == null:
		return {"accepted": false, "message": "No active pet."}
	
	if pd.is_dead or pd.is_hatching:
		return {"accepted": false, "message": "Action unavailable."}

	var result := {"accepted": true, "message": "Done."}

	match action:
		"feed_small":
			pd.hunger = min(100.0, pd.hunger + 18.0)
			pd.happiness = min(100.0, pd.happiness + 3.0)
			pet.perform_action("feed")
			result.message = "Small meal served. +Hunger, +Joy."
		"feed_full":
			pd.hunger = min(100.0, pd.hunger + 30.0)
			pd.happiness = min(100.0, pd.happiness + 2.0)
			pd.energy = max(0.0, pd.energy - 4.0)
			pd.cleanliness = max(0.0, pd.cleanliness - 2.0)
			pet.perform_action("feed")
			result.message = "Full meal served. +Hunger, -Energy."
		"feed_treat":
			pd.happiness = min(100.0, pd.happiness + 12.0)
			pd.hunger = min(100.0, pd.hunger + 8.0)
			pet.perform_action("feed")
			result.message = "Treat served. +Joy."
		"feed_hydrate":
			if pd.water_bowl_level <= 0:
				result.accepted = false
				result.message = "Refill the bowl first."
			else:
				pd.hydration = min(100.0, pd.hydration + 22.0)
				pd.water_bowl_level = max(0.0, pd.water_bowl_level - 10.0)
				pet.perform_action("drink")
				result.message = "Hydration restored. +Water."
		"feed_forage":
			var forage_ticks = randi_range(2, 5)
			forage_state[pet_index] = {"ticks_left": forage_ticks}
			pet.perform_action("exercise")
			result.message = "Foraging started. +Fit, no immediate hunger gain."
		"pet_pat":
			pd.affection = min(100.0, pd.affection + 12.0)
			pd.happiness = min(100.0, pd.happiness + 5.0)
			pd.energy = max(0.0, pd.energy - 1.0)
			pet.perform_action("pet")
			result.message = "Head pat complete. +Love, +Joy."
		"pet_cuddle":
			pd.affection = min(100.0, pd.affection + 18.0)
			pd.happiness = min(100.0, pd.happiness + 7.0)
			pd.energy = max(0.0, pd.energy - 4.0)
			pet.perform_action("pet")
			result.message = "Cuddle complete. +Love, +Joy."
		"pet_talk":
			pd.affection = min(100.0, pd.affection + 10.0)
			pd.happiness = min(100.0, pd.happiness + 4.0)
			if pd.conditions.has("anxiety") and randf() < 0.2:
				pd.conditions["anxiety"] = max(0, int(pd.conditions["anxiety"]) - 1)
				if pd.conditions["anxiety"] <= 0:
					pd.conditions.erase("anxiety")
			pet.perform_action("pet")
			result.message = "Comfort talk complete. +Love."
		"pet_play", "exercise_play", "fetch_ball":
			var refusal = 0.0
			if pd.affection < 20:
				refusal = 0.5
			elif pd.affection < 40:
				refusal = 0.2
			if randf() < refusal:
				result.accepted = false
				result.message = "Not in the mood to play."
			else:
				pd.fitness = min(100.0, pd.fitness + 8.0)
				pd.happiness = min(100.0, pd.happiness + 10.0)
				pd.affection = min(100.0, pd.affection + 7.0)
				pd.energy = max(0.0, pd.energy - 5.0)
				pd.hunger = max(0.0, pd.hunger - 3.0)
				pet.perform_action("fetch_ball" if action == "fetch_ball" else "play_ball")
				result.message = "Fetch complete. +Joy, +Fit, -Energy." if action == "fetch_ball" else "Play session complete. +Joy, +Fit, -Energy."
		"exercise_workout":
			pd.fitness = min(100.0, pd.fitness + 18.0)
			pd.health = min(100.0, pd.health + 3.0)
			pd.energy = max(0.0, pd.energy - 10.0)
			pd.hunger = max(0.0, pd.hunger - 7.0)
			pd.hydration = max(0.0, pd.hydration - 5.0)
			var heat = int(workout_heat.get(pet_index, 0)) + 2
			workout_heat[pet_index] = heat
			if heat >= 7 and randf() < clamp(0.45 * TUNE_WORKOUT_INJURY_MULT, 0.02, 0.98):
				pd.conditions["injury"] = max(int(pd.conditions.get("injury", 0)), 1)
				pd.health = max(0.0, pd.health - 3.0)
				result.message = "Overworked. +Fit, injury risk increased."
			elif heat >= 5 and randf() < clamp(0.22 * TUNE_WORKOUT_INJURY_MULT, 0.02, 0.98):
				pd.conditions["injury"] = max(int(pd.conditions.get("injury", 0)), 1)
				pd.health = max(0.0, pd.health - 2.0)
				result.message = "Overworked. +Fit, injury risk increased."
			else:
				result.message = "Workout complete. +Fit, -Energy."
			pet.perform_action("exercise")
		"rest_toggle":
			pd.is_sleeping = not pd.is_sleeping
			pet.perform_action("rest")
			result.message = "Now sleeping. +Energy over time." if pd.is_sleeping else "Now awake."
		"groom_haircut":
			pd.grooming = min(100.0, pd.grooming + 18.0)
			pd.cleanliness = min(100.0, pd.cleanliness + 5.0)
			pet.perform_action("groom")
			result.message = "Hair cut complete. +Groom, +Clean."
		"groom_dental":
			pd.grooming = min(100.0, pd.grooming + 10.0)
			pd.health = min(100.0, pd.health + 6.0)
			for cond in ["dentalProblems", "dentalOvergrowth"]:
				if pd.conditions.has(cond):
					pd.conditions[cond] = max(0, int(pd.conditions[cond]) - 1)
					if pd.conditions[cond] <= 0:
						pd.conditions.erase(cond)
			pet.perform_action("groom")
			result.message = "Dental check complete. +Health."
		"groom_bathing":
			pd.cleanliness = min(100.0, pd.cleanliness + 24.0)
			pd.happiness = min(100.0, pd.happiness + 5.0)
			pet.perform_action("bathe")
			result.message = "Bath complete. +Clean, +Joy."
		"feed", "pet", "rest", "groom", "bathe", "exercise":
			# Compatibility for old calls.
			pet.perform_action(action)
			result.message = "Action complete."
		_:
			result.accepted = false
			result.message = "Unknown action."

	stats_updated.emit()
	return result

func perform_fetch_sequence(ball_position: Vector2) -> Dictionary:
	var pd = get_active_pet_data()
	var pet = get_active_pet()

	if pd == null or pet == null:
		return {"accepted": false, "message": "No active pet."}
	if pd.is_dead or pd.is_hatching:
		return {"accepted": false, "message": "Action unavailable."}

	var refusal = 0.0
	if pd.affection < 20:
		refusal = 0.5
	elif pd.affection < 40:
		refusal = 0.2
	if randf() < refusal:
		return {"accepted": false, "message": "Not in the mood to play."}

	pd.fitness = min(100.0, pd.fitness + 8.0)
	pd.happiness = min(100.0, pd.happiness + 10.0)
	pd.affection = min(100.0, pd.affection + 7.0)
	pd.energy = max(0.0, pd.energy - 5.0)
	pd.hunger = max(0.0, pd.hunger - 3.0)
	if pet.has_method("start_fetch_sequence"):
		pet.start_fetch_sequence(ball_position, pet.position)
	else:
		pet.perform_action("fetch_ball")

	stats_updated.emit()
	return {"accepted": true, "message": "Fetch started. Watch the pickup, carry, and drop sequence."}

func get_pet_environment(animal_type: String) -> Dictionary:
	return ENVIRONMENTS.get(ANIMAL_DATA.get(animal_type, {}).get("environment", "sewers"), ENVIRONMENTS["sewers"])

func get_environment_position(animal_type: String) -> Vector2:
	var env = ANIMAL_DATA.get(animal_type, {}).get("environment", "sewers")
	return ENVIRONMENT_POSITIONS.get(env, ENVIRONMENT_POSITIONS["sewers"])

func _load_habitat_loadouts():
	habitat_loadouts.clear()
	if not FileAccess.file_exists(HABITAT_LOADOUTS_PATH):
		push_warning("Habitat loadout manifest missing: " + HABITAT_LOADOUTS_PATH)
		return

	var file = FileAccess.open(HABITAT_LOADOUTS_PATH, FileAccess.READ)
	if file == null:
		push_warning("Unable to open habitat loadout manifest: " + HABITAT_LOADOUTS_PATH)
		return

	var raw = file.get_as_text()
	file.close()
	var parsed = JSON.parse_string(raw)
	if not (parsed is Array):
		push_warning("Habitat loadout manifest must be a JSON array: " + HABITAT_LOADOUTS_PATH)
		return

	for entry in parsed:
		if not (entry is Dictionary):
			continue
		var species_id = str(entry.get("speciesId", "")).strip_edges()
		if species_id == "":
			continue
		habitat_loadouts[species_id] = entry

func get_habitat_loadout(animal_type: String) -> Dictionary:
	if habitat_loadouts.is_empty():
		_load_habitat_loadouts()
	return habitat_loadouts.get(animal_type, {})

func _find_habitat_slot(animal_type: String, slot_id: String) -> Dictionary:
	var loadout = get_habitat_loadout(animal_type)
	var slots = loadout.get("slots", [])
	if not (slots is Array):
		return {}
	for slot in slots:
		if slot is Dictionary and str(slot.get("slotId", "")) == slot_id:
			return slot
	return {}

func _find_habitat_slot_for_action(animal_type: String, action_family: String) -> Dictionary:
	var primary = _find_habitat_slot(animal_type, "primary")
	var interaction = _find_habitat_slot(animal_type, "interaction")
	var normalized = normalize_action_family(action_family)
	var interaction_asset = str(interaction.get("assetId", ""))

	if normalized == "feed" and HABITAT_FOOD_ASSETS.has(interaction_asset):
		return interaction
	if normalized == "drink" and HABITAT_DRINK_ASSETS.has(interaction_asset):
		return interaction
	if normalized == "play" and HABITAT_PLAY_ASSETS.has(interaction_asset):
		return interaction
	return primary if not primary.is_empty() else interaction

func _slot_anchor(slot: Dictionary) -> Vector2:
	var rect = slot.get("defaultRect", {})
	if not (rect is Dictionary):
		return Vector2(0.5, 0.82)

	var left = float(rect.get("left", 0.0))
	var top = float(rect.get("top", 0.0))
	var width = float(rect.get("width", 0.0))
	var height = float(rect.get("height", 0.0))
	var manifest_width = max(1.0, HABITAT_MANIFEST_STAGE_SIZE.x)
	var manifest_height = max(1.0, HABITAT_MANIFEST_STAGE_SIZE.y)
	return Vector2(
		clamp((left + (width * 0.5)) / manifest_width, 0.05, 0.95),
		clamp((top + height) / manifest_height, 0.15, 0.95)
	)

func get_habitat_interaction_data(animal_type: String) -> Dictionary:
	var loadout = get_habitat_loadout(animal_type)
	if loadout.is_empty():
		return {}
	var primary = _find_habitat_slot(animal_type, "primary")
	var interaction = _find_habitat_slot(animal_type, "interaction")
	return {
		"primary_object": str(primary.get("assetId", "")),
		"interaction": str(interaction.get("assetId", primary.get("assetId", "stand"))),
		"loadout": loadout
	}

func get_habitat_anchor(animal_type: String, action_family: String = "home") -> Vector2:
	var slot = _find_habitat_slot_for_action(animal_type, action_family)
	if slot.is_empty():
		return Vector2(0.5, 0.82)
	return _slot_anchor(slot)

func get_habitat_interaction_name(animal_type: String) -> String:
	var interaction = _find_habitat_slot(animal_type, "interaction")
	if not interaction.is_empty():
		return str(interaction.get("assetId", "stand"))
	var primary = _find_habitat_slot(animal_type, "primary")
	return str(primary.get("assetId", "stand"))

func normalize_action_family(action_family: String) -> String:
	if action_family == "drink" or action_family == "feed_hydrate":
		return "drink"
	if action_family.begins_with("feed_") or action_family == "feed":
		return "feed"
	if action_family == "rest" or action_family == "rest_toggle":
		return "rest"
	if action_family == "play_ball" or action_family == "fetch_ball" or action_family.begins_with("pet_") or action_family.begins_with("exercise_"):
		return "play"
	return "home"

func get_animal_innate_condition(animal_type: String) -> String:
	return ANIMAL_DATA.get(animal_type, {}).get("innate_condition", "")

func get_stat_color(value: float) -> Color:
	if value > 60:
		return Color(0.29, 0.61, 0.29)  # Green
	elif value > 30:
		return Color(0.96, 0.62, 0.0)  # Orange
	else:
		return Color(0.88, 0.19, 0.19)  # Red

const MEDICINE_CONDITIONS = {
	"woundClean": ["skinInfection", "poorCoat"],
	"antibiotics": ["skinInfection", "respiratoryProblems", "parasites"],
	"jointSupport": ["jointPain", "jointStiffness", "footProblems", "injury"],
	"immuneBoost": ["viralSusceptibility", "skinInfections"],
	"dentalCare": ["dentalProblems", "dentalOvergrowth"],
	"moodStabilizer": ["depression", "anxiety"],
	"energyTonic": ["exhaustion", "malnutrition"],
	"appetiteStimulant": ["malnutrition", "obesity"],
	"detoxHerbs": ["medicationToxicity"],
	"vitaminSupplements": ["poorCoat", "viralSusceptibility"]
}

func check_food_acceptance(pet_index: int, food_type: String) -> bool:
	if pet_index >= pet_datas.size():
		return false
	
	var pd = pet_datas[pet_index]
	
	# Too full
	if pd.hunger < 20:
		return false
	
	# Personality-based rejection chance
	var rejection_chance = 0.0
	if pd.stubbornness > 30:
		rejection_chance += 0.2
	if pd.food_love < -30:
		rejection_chance += 0.2
	
	# Food-specific preferences
	if food_type == "sweet" and pd.cheerfulness < -20:
		rejection_chance -= 0.1  # Actually likes sweets when sad
	elif food_type == "sweet" and pd.happiness > 80:
		rejection_chance += 0.15  # Too happy to need treats
	
	if randf() < rejection_chance:
		return false
	
	return true

func check_water_acceptance(pet_index: int) -> bool:
	if pet_index >= pet_datas.size():
		return false
	
	var pd = pet_datas[pet_index]
	
	# Thirsty
	if pd.hydration < 80:
		return true
	
	# 50% chance otherwise
	return randf() < 0.5

func check_medicine_acceptance(pet_index: int, medicine_type: String) -> Dictionary:
	# Returns {accepted: bool, correct: bool, message: String}
	if pet_index >= pet_datas.size():
		return {"accepted": false, "correct": false, "message": "No active pet."}
	
	var pd = pet_datas[pet_index]
	var target_conditions = MEDICINE_CONDITIONS.get(medicine_type, [])
	
	# Check if pet has any condition this medicine treats
	for cond in pd.conditions.keys():
		if cond in target_conditions:
			return {"accepted": true, "correct": true, "message": "Medicine applied. Condition improved."}
	
	# No matching condition
	return {"accepted": false, "correct": false, "message": "Wrong medicine. Health reduced."}

func apply_food(pet_index: int, food_type: String) -> bool:
	if pet_index >= pet_datas.size():
		return false
	
	if not check_food_acceptance(pet_index, food_type):
		return false
	
	var pd = pet_datas[pet_index]
	var food_stats = {
		"plant": {"stat": "hunger", "value": 20},
		"meat": {"stat": "hunger", "value": 25},
		"sweet": {"stat": "happiness", "value": 15},
		"salty": {"stat": "energy", "value": 10}
	}
	
	var food_data = food_stats.get(food_type, {})
	var stat = food_data.get("stat", "hunger")
	var value = food_data.get("value", 10)
	
	pd.set(stat, min(100.0, pd.get(stat) + value))
	if pet_index < pets.size() and pets[pet_index]:
		pets[pet_index].perform_action("feed")
	return true

func apply_water(pet_index: int) -> bool:
	if pet_index >= pet_datas.size():
		return false
	
	if not check_water_acceptance(pet_index):
		return false
	
	var pd = pet_datas[pet_index]
	pd.hydration = min(100.0, pd.hydration + 25)
	pd.water_bowl_level = max(0.0, pd.water_bowl_level - 10)
	if pet_index < pets.size() and pets[pet_index]:
		pets[pet_index].perform_action("drink")
	return true

func refill_water_bowl(pet_index: int) -> bool:
	if pet_index >= pet_datas.size():
		return false
	var pd = pet_datas[pet_index]
	pd.water_bowl_level = 100.0
	return true

func apply_medicine(pet_index: int, medicine_type: String) -> Dictionary:
	var result = check_medicine_acceptance(pet_index, medicine_type)
	
	if not result["accepted"]:
		# Wrong medicine - reduce health
		var pd = pet_datas[pet_index]
		pd.health = max(0.0, pd.health - 10)
		return result
	
	# Correct medicine - reduce condition severity
	var pd = pet_datas[pet_index]
	var target_conditions = MEDICINE_CONDITIONS.get(medicine_type, [])
	
	for cond in pd.conditions.keys():
		if cond in target_conditions:
			var current_severity = pd.conditions[cond]
			pd.conditions[cond] = current_severity - 1
			if pd.conditions[cond] <= 0:
				pd.conditions.erase(cond)
	
	return result
