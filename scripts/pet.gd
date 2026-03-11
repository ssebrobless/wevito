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
const SPRITE_SCALE := Vector2(3, 3)
const HATCH_DURATION_SEC := 3.0
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
		var frame_path = "res://sprites/egg/egg_%02d.png" % frame
		if ResourceLoader.exists(frame_path):
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

func move_to_home(home_x: float, hold_seconds: float = 0.0, resume_wandering_after_hold: bool = false):
	pause_wandering()
	_home_lock_timer = max(_home_lock_timer, hold_seconds)
	_resume_wandering_after_home_lock = resume_wandering_after_hold
	_target_position = Vector2(clamp(home_x, _bounds.position.x, _bounds.end.x), _floor_y)
	pet_data.target_position = _target_position
	pet_state = PetState.MOVING_TO_ENV
	current_animation = "walk"

func _load_texture(path: String) -> Texture2D:
	var lower_path = path.to_lower()
	var absolute_path = ProjectSettings.globalize_path(path)
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

func load_sprites():
	if pet_data == null:
		return
	
	var animal = pet_data.animal_type
	var gender = pet_data.gender
	var color = pet_data.egg_color
	var age = _animation_age_key()
	
	# Try primary color, then fallbacks
	var colors_to_try = [color, "blue", "yellow", "indigo", "green", "purple"]
	var base_path = ""
	
	for try_color in colors_to_try:
		var age_test_path = "res://sprites/" + animal + "/" + age + "/" + gender + "/" + try_color + "/idle_00.png"
		var legacy_test_path = "res://sprites/" + animal + "/" + gender + "/" + try_color + "/idle_00.png"
		if ResourceLoader.exists(age_test_path):
			base_path = "res://sprites/" + animal + "/" + age + "/" + gender + "/" + try_color + "/"
			if try_color != color:
				print("Wevito: Color '" + color + "' not found for " + age + ", using '" + try_color + "'")
			break
		elif ResourceLoader.exists(legacy_test_path):
			base_path = "res://sprites/" + animal + "/" + gender + "/" + try_color + "/"
			if try_color != color:
				print("Wevito: Color '" + color + "' not found for " + age + ", using '" + try_color + "'")
			break
	
	if base_path == "":
		print("Wevito ERROR: No sprites found for " + animal + "/" + age + "/" + gender)
		# Create a placeholder colored rectangle so pet is visible
		_create_placeholder_sprite()
		return
	
	# Load all animation frames - detect available frames per animation
	var animations = ["idle", "walk", "eat", "happy", "sad", "sleep", "sick", "bathe"]
	for anim in animations:
		_sprites[anim] = []
		# Try loading frames until we hit one that doesn't exist
		for frame in range(20):  # Max 20 frames safety limit
			var frame_path = base_path + anim + "_%02d.png" % frame
			if ResourceLoader.exists(frame_path):
				var tex = _load_texture(frame_path)
				if tex:
					_sprites[anim].append(tex)
			else:
				break  # No more frames
		
		if _sprites[anim].size() == 0:
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
	if pet_data == null or pet_data.is_dead:
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
	
	# Update animation timer
	animation_timer += delta
	if animation_timer >= animation_speed:
		animation_timer = 0
		animation_frame = (animation_frame + 1) % get_frame_count()
		update_sprite()

	if _home_lock_timer > 0.0:
		_home_lock_timer = max(0.0, _home_lock_timer - delta)
	
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
	
	# Update animation state
	update_animation_state()

func move_to_environment():
	# Get game_manager from parent (main_scene creates both pet and game_manager as children)
	var main_scene = get_parent()
	if main_scene and main_scene.has_method("get_pet_home_position_for_node"):
		_target_position = main_scene.get_pet_home_position_for_node(self)
		_target_position.y = _floor_y
		pet_data.target_position = _target_position
		pet_state = PetState.MOVING_TO_ENV
		current_animation = "walk"
		return

	var game_mgr = null
	if main_scene:
		game_mgr = main_scene.get("game_manager")
	if game_mgr and game_mgr.has_method("get_environment_position"):
		_target_position = game_mgr.get_environment_position(pet_data.animal_type)
		_target_position.y = _floor_y
		pet_data.target_position = _target_position
		pet_state = PetState.MOVING_TO_ENV
		current_animation = "walk"

func _update_movement_to_target(delta: float):
	var speed = 80.0
	var dx = _target_position.x - position.x
	var distance = abs(dx)
	
	if distance < 2.0:
		_complete_move_to_target()
	else:
		position.x += sign(dx) * min(distance, speed * delta)
		position.y = _floor_y
		if abs(_target_position.x - position.x) < 2.0:
			_complete_move_to_target()
	
	_set_horizontal_facing(dx)

func _complete_move_to_target():
	position.x = _target_position.x
	position.y = _floor_y
	pet_state = PetState.WANDERING
	_interaction_timer = 0.0
	if _home_lock_timer <= 0.0 and _resume_wandering_after_home_lock and not _wandering:
		_resume_wandering_after_home_lock = false
		_interaction_timer = 999.0
		start_wandering()

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
	
	if new_anim != current_animation:
		current_animation = new_anim
		animation_frame = 0

func update_sprite():
	if pet_data and pet_data.is_hatching and _egg_frames.size() > 0:
		var egg_idx = clamp(_hatch_stage_index, 0, _egg_frames.size() - 1)
		sprite.texture = _egg_frames[egg_idx]
		sprite.self_modulate = _egg_tint()
		return

	sprite.self_modulate = Color.WHITE
	var frames = _sprites.get(current_animation, [])
	if frames.size() > 0:
		var frame_idx = animation_frame % frames.size()
		if frame_idx < frames.size():
			sprite.texture = frames[frame_idx]

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

func perform_action(action: String):
	_interaction_timer = 0.0
	pause_wandering()
	pet_state = PetState.ACTING
	
	match action:
		"feed":
			current_animation = "eat"
		"pet":
			current_animation = "happy"
		"rest":
			current_animation = "sleep" if pet_data.is_sleeping else "idle"
		"bathe":
			current_animation = "bathe"
		"groom":
			current_animation = "happy"
		"exercise":
			current_animation = "walk"
	
	animation_frame = 0
	_wander_timer = 1.0
	
	# After action animation plays, move to environment
	await get_tree().create_timer(1.5).timeout
	if pet_state == PetState.ACTING:
		move_to_environment()
