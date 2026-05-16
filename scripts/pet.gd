extends Node2D
class_name Pet

const PetData = preload("res://scripts/pet_data.gd")

var pet_data: PetData
var sprite: Sprite2D
var click_area: Area2D
var current_animation: String = "idle"
var animation_frame: int = 0
var animation_timer: float = 0.0
var animation_speed: float = 0.25  # seconds per frame
var cursor_reactivity_enabled: bool = true
var cursor_reactivity_distance_px: float = 200.0
var _cursor_reactivity_timer: float = 0.0
const SPRITE_SCALE := Vector2(3, 3)
const HATCH_DURATION_SEC := 3.0
const PET_SPRITE_ROOTS := ["res://sprites_runtime", "res://sprites"]
const SHARED_SPRITE_ROOTS := ["res://sprites_shared_runtime", "res://sprites"]
const REQUIRED_ANIMATIONS := ["idle", "walk", "eat", "happy", "sad", "sleep", "sick", "bathe"]
const EXPANDED_OPTIONAL_ANIMATIONS := [
	"drink",
	"play_ball",
	"hold_ball",
	"carry_ball_walk",
	"carry_ball_run",
	"pickup_ball",
	"drop_ball",
	"ghost"
]
const ACTION_ANIMATION_FALLBACKS := {
	"feed": ["eat", "idle"],
	"drink": ["drink", "eat", "idle"],
	"pet": ["happy", "idle"],
	"rest": ["sleep", "idle"],
	"bathe": ["bathe", "happy", "idle"],
	"groom": ["happy", "idle"],
	"exercise": ["walk", "idle"],
	"play_ball": ["play_ball", "happy", "walk", "idle"],
	"fetch_ball": ["carry_ball_run", "carry_ball_walk", "play_ball", "walk", "idle"],
	"hold_ball": ["hold_ball", "happy", "idle"],
	"pickup_ball": ["pickup_ball", "play_ball", "happy", "idle"],
	"drop_ball": ["drop_ball", "happy", "idle"]
}
const STAGE_SCALE_MULTIPLIERS := {
	0: 1.0,
	1: 1.0,
	2: 1.0,
	3: 1.0,
	4: 1.0
}
const EGG_TINTS := {
	"red": Color(1.0, 0.42, 0.42),
	"orange": Color(1.0, 0.66, 0.3),
	"yellow": Color(1.0, 0.88, 0.4),
	"blue": Color(0.45, 0.75, 0.99),
	"indigo": Color(0.45, 0.56, 0.99),
	"violet": Color(0.85, 0.47, 0.95)
}

# State machine
enum PetState { WANDERING, MOVING_TO_ENV, ACTING }
enum FetchStage { NONE, MOVE_TO_BALL, PICKUP, HOLD, CARRY_WALK, CARRY_RUN, DROP, RETURN_IDLE }
const FETCH_STAGE_DURATIONS := {
	FetchStage.MOVE_TO_BALL: 2.4,
	FetchStage.PICKUP: 0.7,
	FetchStage.HOLD: 0.45,
	FetchStage.CARRY_WALK: 0.8,
	FetchStage.CARRY_RUN: 1.4,
	FetchStage.DROP: 0.7,
	FetchStage.RETURN_IDLE: 0.35
}
const FETCH_STAGE_ANIMATIONS := {
	FetchStage.MOVE_TO_BALL: ["walk", "idle"],
	FetchStage.PICKUP: ["pickup_ball", "play_ball", "happy", "idle"],
	FetchStage.HOLD: ["hold_ball", "happy", "idle"],
	FetchStage.CARRY_WALK: ["carry_ball_walk", "hold_ball", "walk", "idle"],
	FetchStage.CARRY_RUN: ["carry_ball_run", "carry_ball_walk", "hold_ball", "walk", "idle"],
	FetchStage.DROP: ["drop_ball", "happy", "idle"],
	FetchStage.RETURN_IDLE: ["happy", "idle"]
}
var pet_state: PetState = PetState.WANDERING
var _target_position: Vector2 = Vector2.ZERO

# Movement
var _wandering: bool = false
var _wander_timer: float = 0.0
var _interaction_timer: float = 0.0
var _bounds: Rect2 = Rect2(20, 240, 120, 80)  # Each pet has its own area
var _direction: Vector2 = Vector2.RIGHT
var _floor_y: float = 280.0
var _idle_jump_timer: float = 6.0
var _home_lock_timer: float = 0.0
var _resume_wandering_after_home_lock: bool = false
var _current_action_family: String = "home"
var fetch_stage: int = FetchStage.NONE
var _fetch_stage_timer: float = 0.0
var _fetch_ball_position: Vector2 = Vector2.ZERO
var _fetch_return_position: Vector2 = Vector2.ZERO

# Sprite storage
var _sprites: Dictionary = {}
var _egg_frames: Array[Texture2D] = []
var _hatch_elapsed: float = 0.0
var _hatch_stage_index: int = -1
var _last_hatching_state: bool = false

# signal animation_changed(anim: String)  # Unused - kept for future

func _ready():
	_idle_jump_timer = randf_range(6.0, 12.0)

	# Create sprite
	sprite = Sprite2D.new()
	sprite.position = Vector2.ZERO  # Local position, Pet node moves instead
	sprite.scale = SPRITE_SCALE  # Larger for readability and easier interaction
	z_index = 10  # Draw above background
	add_child(sprite)

	click_area = Area2D.new()
	var collision = CollisionShape2D.new()
	var shape = RectangleShape2D.new()
	shape.size = Vector2(90, 90)
	collision.shape = shape
	click_area.add_child(collision)
	click_area.input_event.connect(_on_click_area_input_event)
	add_child(click_area)

func _on_click_area_input_event(_viewport, event, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		current_animation = "happy"
		animation_frame = 0
		_play_happy_effect()

func _play_happy_effect():
	# Small jump effect using a tween
	if get_tree():
		var tween = create_tween()
		tween.tween_property(self, "position:y", position.y - 12, 0.15)
		tween.tween_property(self, "position:y", position.y, 0.15)

func _set_horizontal_facing(x_dir: float):
	if sprite == null:
		return
	if x_dir > 0.01:
		_direction = Vector2.RIGHT
		sprite.flip_h = false
	elif x_dir < -0.01:
		_direction = Vector2.LEFT
		sprite.flip_h = true

func _update_cursor_reactivity(delta: float):
	if not cursor_reactivity_enabled:
		return

	if _cursor_reactivity_timer > 0.0:
		_cursor_reactivity_timer = max(0.0, _cursor_reactivity_timer - delta)
		return

	var mouse_position = get_global_mouse_position()
	if position.distance_to(mouse_position) > cursor_reactivity_distance_px:
		return

	_set_horizontal_facing(mouse_position.x - position.x)
	_cursor_reactivity_timer = 10.0

func _egg_tint() -> Color:
	if pet_data == null:
		return Color.WHITE
	return EGG_TINTS.get(pet_data.egg_color, Color.WHITE)

func _animation_age_key() -> String:
	if pet_data == null:
		return "adult"
	match pet_data.stage:
		1:
			return "baby"
		2:
			return "teen"
		_:
			return "adult"

func _apply_visual_scale():
	if sprite == null or pet_data == null:
		return
	var multiplier = float(STAGE_SCALE_MULTIPLIERS.get(pet_data.stage, 1.0))
	sprite.scale = SPRITE_SCALE * multiplier

func setup(data: PetData):
	pet_data = data
	position = pet_data.position  # Move the Pet node, not the sprite
	position.y = _floor_y
	pet_data.position = position
	_hatch_elapsed = 0.0
	_hatch_stage_index = -1
	_last_hatching_state = pet_data.is_hatching
	load_egg_frames()
	load_sprites()
	_apply_visual_scale()

func load_egg_frames():
	_egg_frames.clear()
	for frame in range(5):
		var frame_path = _resolve_shared_sprite_path("egg/egg_%02d.png" % frame)
		if frame_path != "":
			var tex = _load_texture(frame_path)
			if tex:
				_egg_frames.append(tex)

func set_floor_y(new_floor_y: float):
	_floor_y = new_floor_y
	position.y = _floor_y
	if pet_data:
		pet_data.position = position
		if pet_data.target_position != Vector2.ZERO:
			pet_data.target_position.y = _floor_y

func set_wander_bounds(bounds: Rect2):
	_bounds = bounds
	if pet_data:
		var clamped_x = clamp(position.x, _bounds.position.x, _bounds.end.x)
		position.x = clamped_x
		position.y = _floor_y
		pet_data.position = position
		var target = pet_data.target_position
		if target == Vector2.ZERO:
			target = position
		target.x = clamp(target.x, _bounds.position.x, _bounds.end.x)
		target.y = _floor_y
		pet_data.target_position = target
		_target_position = target

func move_to_home(home_x: float, hold_seconds: float = 0.0, resume_wandering_after_hold: bool = false):
	pause_wandering()
	_home_lock_timer = max(_home_lock_timer, hold_seconds)
	_resume_wandering_after_home_lock = resume_wandering_after_hold
	_target_position = Vector2(clamp(home_x, _bounds.position.x, _bounds.end.x), _floor_y)
	pet_data.target_position = _target_position
	pet_state = PetState.MOVING_TO_ENV
	current_animation = "walk"

func _load_texture(path: String) -> Texture2D:
	if path == "":
		return null
	var lower_path = path.to_lower()
	var absolute_path = ProjectSettings.globalize_path(path)
	var external_path = _external_asset_path(path)
	if external_path != "" and FileAccess.file_exists(external_path):
		var external_image = Image.new()
		if external_image.load(external_path) == OK:
			return ImageTexture.create_from_image(external_image)
	if not OS.has_feature("editor"):
		var packed_resource = load(path)
		if packed_resource is Texture2D:
			return packed_resource
	if lower_path.ends_with(".png") or lower_path.ends_with(".jpg") or lower_path.ends_with(".jpeg") or lower_path.ends_with(".webp"):
		if not FileAccess.file_exists(absolute_path):
			return null
		var image = Image.new()
		if image.load(absolute_path) == OK:
			return ImageTexture.create_from_image(image)
		return null
	var resource = load(path)
	if resource is Texture2D:
		return resource
	if not FileAccess.file_exists(absolute_path):
		return null
	return null

func _resource_or_file_exists(path: String) -> bool:
	var external_path = _external_asset_path(path)
	return ResourceLoader.exists(path) or FileAccess.file_exists(ProjectSettings.globalize_path(path)) or (external_path != "" and FileAccess.file_exists(external_path))

func _external_asset_path(path: String) -> String:
	if OS.has_feature("editor") or not path.begins_with("res://"):
		return ""
	var relative_path = path.trim_prefix("res://")
	var executable_dir = OS.get_executable_path().get_base_dir()
	if executable_dir == "":
		return ""
	for root in [executable_dir.path_join("assets"), executable_dir]:
		var candidate = root.path_join(relative_path)
		if FileAccess.file_exists(candidate):
			return candidate
	return ""

func _resolve_shared_sprite_path(relative_path: String) -> String:
	for root in SHARED_SPRITE_ROOTS:
		var candidate = root + "/" + relative_path
		if _resource_or_file_exists(candidate):
			return candidate
	return ""

func load_sprites():
	if pet_data == null:
		return
	
	var animal = pet_data.animal_type
	var gender = pet_data.gender
	var color = pet_data.egg_color
	var age = _animation_age_key()
	
	var base_path = ""
	
	for root in PET_SPRITE_ROOTS:
		var age_test_path = root + "/" + animal + "/" + age + "/" + gender + "/" + color + "/idle_00.png"
		if _resource_or_file_exists(age_test_path):
			base_path = root + "/" + animal + "/" + age + "/" + gender + "/" + color + "/"
			break
	
	if base_path == "":
		print("Wevito ERROR: No sprites found for " + animal + "/" + age + "/" + gender + "/" + color)
		# Create a placeholder colored rectangle so pet is visible
		_create_placeholder_sprite()
		return
	
	var animations := []
	animations.append_array(REQUIRED_ANIMATIONS)
	animations.append_array(EXPANDED_OPTIONAL_ANIMATIONS)
	for anim in animations:
		_sprites[anim] = []
		# Try loading frames until we hit one that doesn't exist
		for frame in range(20):  # Max 20 frames safety limit
			var frame_path = base_path + anim + "_%02d.png" % frame
			if _resource_or_file_exists(frame_path):
				var tex = _load_texture(frame_path)
				if tex:
					_sprites[anim].append(tex)
			else:
				break  # No more frames
		
		if anim in REQUIRED_ANIMATIONS and _sprites[anim].size() == 0:
			print("Wevito WARNING: No frames loaded for animation: " + anim)
	
	# Set initial sprite
	update_sprite()

func _create_placeholder_sprite():
	var image = Image.create(16, 16, false, Image.FORMAT_RGBA8)
	image.fill(Color(1.0, 0.5, 0.5, 1.0))
	var tex = ImageTexture.create_from_image(image)
	sprite.texture = tex
	sprite.self_modulate = Color.WHITE

func _process(delta):
	if pet_data == null:
		return

	if pet_data.is_dead:
		_update_death_visual(delta)
		return

	if pet_data.is_hatching:
		_hatch_elapsed = min(HATCH_DURATION_SEC, _hatch_elapsed + delta)
		var progress = clamp(_hatch_elapsed / HATCH_DURATION_SEC, 0.0, 0.999)
		var egg_stage = min(_egg_frames.size() - 1, int(floor(progress * _egg_frames.size()))) if _egg_frames.size() > 0 else -1
		if egg_stage != _hatch_stage_index:
			_hatch_stage_index = egg_stage
			update_sprite()
	elif _last_hatching_state:
		_hatch_stage_index = -1
		update_sprite()
	_last_hatching_state = pet_data.is_hatching
	_apply_visual_scale()
	_update_cursor_reactivity(delta)
	
	# Update animation timer
	animation_timer += delta
	if animation_timer >= animation_speed:
		animation_timer = 0
		animation_frame = (animation_frame + 1) % get_frame_count()
		update_sprite()

	if _home_lock_timer > 0.0:
		_home_lock_timer = max(0.0, _home_lock_timer - delta)

	if fetch_stage != FetchStage.NONE:
		_update_fetch_sequence(delta)
		return
	
	# Update based on state
	match pet_state:
		PetState.WANDERING:
			if not pet_data.is_sleeping:
		update_wandering(delta)
				update_movement(delta)
			if not _wandering:
				_idle_jump_timer -= delta
				if _idle_jump_timer <= 0.0:
					current_animation = "happy"
					animation_frame = 0
					_play_happy_effect()
					_idle_jump_timer = randf_range(6.0, 12.0)
		PetState.MOVING_TO_ENV:
			_update_movement_to_target(delta)
		PetState.ACTING:
			pass  # Animation plays, wait for action to finish
	
	if pet_state != PetState.ACTING:
		update_animation_state()

func move_to_environment():
	# Get game_manager from parent (main_scene creates both pet and game_manager as children)
	var main_scene = get_parent()
	if main_scene and main_scene.has_method("get_pet_home_position_for_node"):
		_target_position = main_scene.get_pet_home_position_for_node(self, _current_action_family)
		pet_data.target_position = _target_position
		pet_state = PetState.MOVING_TO_ENV
		current_animation = "walk"
		return

	var game_mgr = null
	if main_scene:
		game_mgr = main_scene.get("game_manager")
	if game_mgr and game_mgr.has_method("get_environment_position"):
		_target_position = game_mgr.get_environment_position(pet_data.animal_type)
		pet_data.target_position = _target_position
		pet_state = PetState.MOVING_TO_ENV
		current_animation = "walk"

func _update_movement_to_target(delta: float):
	var speed = 80.0
	var delta_to_target = _target_position - position
	var distance = delta_to_target.length()
	
	if distance < 2.0:
		_complete_move_to_target()
	else:
		position += delta_to_target.normalized() * min(distance, speed * delta)
		if position.distance_to(_target_position) < 2.0:
			_complete_move_to_target()
	
	_set_horizontal_facing(delta_to_target.x)

func _complete_move_to_target():
	position = _target_position
	if pet_data:
		pet_data.position = position
	pet_state = PetState.WANDERING
	_interaction_timer = 0.0
	if _home_lock_timer <= 0.0 and _resume_wandering_after_home_lock and not _wandering:
		_resume_wandering_after_home_lock = false
		_interaction_timer = 999.0
		start_wandering()
	_current_action_family = "home"

func update_wandering(delta):
	if _home_lock_timer > 0.0:
		return

	if _resume_wandering_after_home_lock and not _wandering:
		_resume_wandering_after_home_lock = false
		_interaction_timer = 999.0
		start_wandering()
		return

	_interaction_timer += delta
	
	# Check to start wandering
	if not _wandering and _interaction_timer > 20.0:
		var chance = pet_data.get_wander_chance()
		if randf() < chance:
			start_wandering()
	
	# Update wander timer
	if _wandering:
		_wander_timer -= delta
		if _wander_timer <= 0:
			pause_wandering()

func start_wandering():
	_wandering = true
	_wander_timer = randf_range(2.0, 5.0)
	pet_data.is_wandering = true
	
	# Pick random position within bounds
	var new_x = randf_range(_bounds.position.x, _bounds.end.x)
	var new_y = _floor_y
	pet_data.target_position = Vector2(new_x, new_y)
	
	_set_horizontal_facing(new_x - position.x)

func pause_wandering():
	_wandering = false
	pet_data.is_wandering = false
	_wander_timer = randf_range(2.0, 4.0)
	_direction = Vector2.ZERO

func update_movement(delta):
	if not _wandering:
		return
	
	var speed = pet_data.get_movement_speed()
	var target = pet_data.target_position
	
	var move_dir = (target - position).normalized()
	var move_amount = move_dir * speed * delta
	
	var new_pos = position + move_amount
	
	# Clamp to bounds
	new_pos.x = clamp(new_pos.x, _bounds.position.x, _bounds.end.x)
	new_pos.y = _floor_y
	
	position = new_pos  # Move the Pet node, sprite stays at local (0,0)
	pet_data.position = new_pos
	
	_set_horizontal_facing(move_dir.x)
	
	# Check if reached target
	if position.distance_to(target) < 5.0:
		pause_wandering()

func update_animation_state():
	var new_anim = "idle"
	
	if pet_data.is_hatching:
		new_anim = "idle"
	elif pet_data.is_sleeping:
		new_anim = "sleep"
	elif _wandering:
		new_anim = "walk"
	elif pet_data.conditions.has("depression") or pet_data.happiness < 25:
		new_anim = "sad"
	elif pet_data.happiness > 80:
		new_anim = "happy"
	elif pet_data.conditions.size() > 0:
		new_anim = "sick"
	
	new_anim = _first_available_animation([new_anim, "idle"])
	if new_anim != current_animation:
		current_animation = new_anim
		animation_frame = 0

func update_sprite():
	if pet_data and pet_data.is_hatching and _egg_frames.size() > 0:
		var egg_idx = clamp(_hatch_stage_index, 0, _egg_frames.size() - 1)
		sprite.texture = _egg_frames[egg_idx]
		sprite.self_modulate = _egg_tint()
		return

	sprite.self_modulate = _lifecycle_tint()
	var frames = _sprites.get(current_animation, [])
	if frames.size() > 0:
		var frame_idx = animation_frame % frames.size()
		if frame_idx < frames.size():
			sprite.texture = frames[frame_idx]

func _lifecycle_tint() -> Color:
	if pet_data == null:
		return Color.WHITE
	if pet_data.is_ghost:
		return Color(0.72, 0.88, 1.0, 0.52)
	if pet_data.is_dead:
		return Color(0.62, 0.62, 0.68, 0.62)
	if pet_data.stage == 4:
		return Color(0.84, 0.84, 0.82, 1.0)
	return Color.WHITE

func _update_death_visual(delta: float):
	if pet_data == null:
		return
	pet_data.death_elapsed_sec += delta
	if pet_data.death_elapsed_sec < 2.5:
		current_animation = _first_available_animation(["sad", "idle"])
	else:
		pet_data.is_ghost = true
		current_animation = _first_available_animation(["ghost", "idle"])
	animation_timer += delta
	if animation_timer >= animation_speed:
		animation_timer = 0
		animation_frame = (animation_frame + 1) % get_frame_count()
	update_sprite()

func get_frame_count() -> int:
	if pet_data and pet_data.is_hatching and _egg_frames.size() > 0:
		return _egg_frames.size()
	var frames = _sprites.get(current_animation, [])
	return max(1, frames.size())

func get_current_sprite_path() -> String:
	var frames = _sprites.get(current_animation, [])
	if frames.size() > 0:
		var frame_idx = animation_frame % frames.size()
		if frame_idx < frames.size() and frames[frame_idx]:
			return frames[frame_idx].get_path()
	return ""

func request_auto_action(action: String) -> bool:
	if _current_action_family == action and (pet_state == PetState.ACTING or pet_state == PetState.MOVING_TO_ENV):
		return false
	perform_action(action)
	return true

func start_fetch_sequence(ball_position: Vector2, return_position: Vector2):
	_current_action_family = "fetch_ball"
	_fetch_ball_position = Vector2(ball_position.x, _floor_y)
	_fetch_return_position = Vector2(return_position.x, _floor_y)
	pause_wandering()
	pet_state = PetState.ACTING
	_set_fetch_stage(FetchStage.MOVE_TO_BALL)

func is_fetch_sequence_active() -> bool:
	return fetch_stage != FetchStage.NONE

func _set_fetch_stage(next_stage: int):
	fetch_stage = next_stage
	_fetch_stage_timer = 0.0
	animation_frame = 0
	if fetch_stage == FetchStage.NONE:
		_current_action_family = "home"
		_wandering = false
		_resume_wandering_after_home_lock = false
		_interaction_timer = 0.0
		_wander_timer = 2.0
		if pet_data:
			pet_data.is_wandering = false
		current_animation = _first_available_animation(["happy", "idle"])
		pet_state = PetState.WANDERING
		return
	current_animation = _first_available_animation(FETCH_STAGE_ANIMATIONS.get(fetch_stage, ["idle"]))

func _advance_fetch_stage():
	match fetch_stage:
		FetchStage.MOVE_TO_BALL:
			_set_fetch_stage(FetchStage.PICKUP)
		FetchStage.PICKUP:
			_set_fetch_stage(FetchStage.HOLD)
		FetchStage.HOLD:
			_set_fetch_stage(FetchStage.CARRY_WALK)
		FetchStage.CARRY_WALK:
			_set_fetch_stage(FetchStage.CARRY_RUN)
		FetchStage.CARRY_RUN:
			_set_fetch_stage(FetchStage.DROP)
		FetchStage.DROP:
			_set_fetch_stage(FetchStage.RETURN_IDLE)
		FetchStage.RETURN_IDLE:
			_set_fetch_stage(FetchStage.NONE)
		_:
			_set_fetch_stage(FetchStage.NONE)

func _update_fetch_sequence(delta: float):
	_fetch_stage_timer += delta
	var max_duration = float(FETCH_STAGE_DURATIONS.get(fetch_stage, 0.5))

	match fetch_stage:
		FetchStage.MOVE_TO_BALL:
			if _move_fetch_toward(_fetch_ball_position, delta, 92.0) or _fetch_stage_timer >= max_duration:
				_advance_fetch_stage()
		FetchStage.CARRY_WALK:
			var midpoint = Vector2((_fetch_ball_position.x + _fetch_return_position.x) * 0.5, _floor_y)
			if _move_fetch_toward(midpoint, delta, 72.0) or _fetch_stage_timer >= max_duration:
				_advance_fetch_stage()
		FetchStage.CARRY_RUN:
			if _move_fetch_toward(_fetch_return_position, delta, 112.0) or _fetch_stage_timer >= max_duration:
				_advance_fetch_stage()
		_:
			if _fetch_stage_timer >= max_duration:
				_advance_fetch_stage()

func _move_fetch_toward(target_position: Vector2, delta: float, speed: float) -> bool:
	var delta_to_target = target_position - position
	var distance = delta_to_target.length()
	if distance < 3.0:
		position = target_position
		if pet_data:
			pet_data.position = position
			pet_data.target_position = position
		return true
	position += delta_to_target.normalized() * min(distance, speed * delta)
	position.y = _floor_y
	if pet_data:
		pet_data.position = position
		pet_data.target_position = target_position
	_set_horizontal_facing(delta_to_target.x)
	return false

func perform_action(action: String):
	_current_action_family = action
	_interaction_timer = 0.0
	pause_wandering()
	pet_state = PetState.ACTING
	
	if action == "rest" and pet_data and not pet_data.is_sleeping:
		current_animation = _first_available_animation(["idle"])
	else:
		current_animation = _first_available_animation(ACTION_ANIMATION_FALLBACKS.get(action, [action, "idle"]))
	
	animation_frame = 0
	_wander_timer = 1.0
	
	# After action animation plays, move to environment
	await get_tree().create_timer(1.5).timeout
	if pet_state == PetState.ACTING:
		move_to_environment()

func _first_available_animation(candidates: Array) -> String:
	for anim in candidates:
		var anim_name = str(anim)
		var frames = _sprites.get(anim_name, [])
		if frames is Array and frames.size() > 0:
			return anim_name
	return "idle"
