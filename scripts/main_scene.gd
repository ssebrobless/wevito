extends Node2D

var game_manager: GameManager
var ui_visible: bool = true
var window_positioned: bool = false
var egg_selection_active: bool = false
var doctors_note_visible: bool = false

var stats_panel: VBoxContainer
var actions_bar: HBoxContainer
var title_label: Label
var pet_name_label: Label
var pet_gender_label: Label
var pet_age_label: Label
var background: ColorRect
var bg_mid: ColorRect
var bg_ground: ColorRect
var environment_stages: Array[Dictionary] = []
var nav_arrows: HBoxContainer
var add_pet_button: Button
var doctor_button: Button
var minimize_button: Button
var settings_button: Button
var ghost_button: Button
var memoriam_button: Button
var pet_indicators: HBoxContainer
var egg_selection_overlay: Control
var action_menu_overlay: Control
var action_tab_overlay: Control
var naming_overlay: Control
var death_overlay: Control
var memoriam_overlay: Control
var settings_overlay: Control
var doctors_note_overlay: Control
var feeding_panel_overlay: Control
var sound_player: AudioStreamPlayer
var ghost_overlay: ColorRect
var sound_manager: SoundManager
var held_item_sprite: Sprite2D
var held_item: Dictionary = {"type": "", "name": "", "icon": ""}


# Game settings
var settings = {
	"bell": true,
	"tick_rate": 60.0,
	"auto_save": true,
	"auto_save_interval": 3,
	"click_through": false,
	"ghost_mode": false,
	"experimental_monitor_roam": false,
	"sound_effects": true,
	"main_menu_text_color": "#9cbd0f",
	"detail_text_color": "#d8e0d8",
	"status_box_palette": "classic"
}

const STATUS_BOX_PALETTES = {
	"classic": {
		"high": "#4caf50",
		"mid": "#f0b23a",
		"low": "#d93a3a"
	},
	"cool": {
		"high": "#43c6db",
		"mid": "#4d8ef7",
		"low": "#5b4ce6"
	},
	"warm": {
		"high": "#f4cf57",
		"mid": "#ec8f3a",
		"low": "#d4483d"
	},
	"mono": {
		"high": "#e0e0e0",
		"mid": "#9e9e9e",
		"low": "#555555"
	}
}

const EGG_COLORS = {
	"red": Color(1.0, 0.42, 0.42),
	"orange": Color(1.0, 0.66, 0.3),
	"yellow": Color(1.0, 0.88, 0.4),
	"blue": Color(0.45, 0.75, 0.99),
	"indigo": Color(0.45, 0.56, 0.99),
	"violet": Color(0.85, 0.47, 0.95)
}

var COLOR_BG = Color(0.04, 0.04, 0.08, 0.95)
var COLOR_UI_BG = Color(0.1, 0.1, 0.18, 0.9)
var COLOR_BORDER = Color(0.23, 0.23, 0.35, 1.0)
var COLOR_TEXT = Color(0.61, 0.74, 0.06, 1.0)
var COLOR_TEXT_DIM = Color(0.42, 0.54, 0.42, 1.0)
const COLOR_SURFACE := Color(0.06, 0.08, 0.1, 1.0)
const COLOR_SURFACE_ALT := Color(0.1, 0.1, 0.18, 0.9)
const COLOR_BACKDROP := Color(0.01, 0.01, 0.02, 1.0)
const COLOR_OVERLAY_PANEL := Color(0.02, 0.02, 0.04, 0.98)
const UI_FADE_IN_SEC := 0.14
const UI_FADE_OUT_SEC := 0.12
const TAB_SWAP_SEC := 0.12
const TOAST_VISIBLE_SEC := 0.9
const TOAST_FADE_SEC := 0.2
const TOAST_REPEAT_SUPPRESS_MS := 500

var auto_save_timer: float = 0.0
var last_open_action_tab: String = ""  # Track last open tab for focus restoration
var action_tab_open: bool = false  # Is an action tab currently open
var current_action_tab: String = ""  # Which action tab is open
var action_tab_recall_pending_hold: bool = false
var startup_focus_guard: bool = true
var monitor_roam_active: bool = false
var feedback_label: Label
var feedback_tween: Tween
var feedback_last_text: String = ""
var feedback_last_at_ms: int = 0

const WINDOW_FOCUSED_SIZE := Vector2i(320, 420)
const WINDOW_UNFOCUSED_SIZE := Vector2i(320, 240)
const WINDOW_MARGIN := Vector2i(20, 20)
const TOP_ZONE_PCT := 0.12
const HUD_ZONE_PCT := 0.44
const GAP_ZONE_PCT := 0.04
const PET_ZONE_PCT := 0.40

const HUD_PADDING_X := 10.0
const HUD_INNER_GAP := 6.0
const HUD_ACTIONS_GAP := 10.0
const HUD_ACTIONS_HEIGHT := 22.0
const HUD_ACTION_BUTTON_GAP := 2.0
const IDENTITY_ROW_Y_OFFSET := 6.0
const IDENTITY_NAME_RATIO := 0.44
const IDENTITY_GENDER_WIDTH := 22.0
const IDENTITY_INNER_GAP := 4.0
const PET_FLOOR_INSET := 18.0
const STAGE_WIDTH_RATIO := 0.78
const STAGE_MIN_WIDTH := 228.0
const STAGE_MAX_WIDTH := 292.0
const STAGE_TOP_INSET := 6.0
const MONITOR_ROAM_MARGIN_X := 28.0
const MONITOR_ROAM_MARGIN_Y := 38.0

func _color_from_setting(key: String, fallback: Color) -> Color:
	var val = settings.get(key, "")
	if val is String and str(val) != "":
		return Color.from_string(val, fallback)
	return fallback

func _color_to_setting(c: Color) -> String:
	return c.to_html(false)

func _truncate_with_ellipsis(text: String, max_chars: int) -> String:
	if max_chars <= 0:
		return ""
	if text.length() <= max_chars:
		return text
	if max_chars == 1:
		return "..."
	return text.substr(0, max_chars - 1) + "..."

func _main_text_color() -> Color:
	return _color_from_setting("main_menu_text_color", COLOR_TEXT)

func _detail_text_color() -> Color:
	return _color_from_setting("detail_text_color", Color(0.85, 0.88, 0.85))

func _status_box_color(value: float) -> Color:
	var palette_key = settings.get("status_box_palette", "classic")
	var palette = STATUS_BOX_PALETTES.get(palette_key, STATUS_BOX_PALETTES["classic"])
	var low = Color.from_string(palette["low"], Color.RED)
	var mid = Color.from_string(palette["mid"], Color.ORANGE)
	var high = Color.from_string(palette["high"], Color.GREEN)
	var t = clamp(value / 100.0, 0.0, 1.0)
	if t < 0.5:
		return low.lerp(mid, t * 2.0)
	return mid.lerp(high, (t - 0.5) * 2.0)

func _contrast_text_color(bg: Color) -> Color:
	var lum = 0.2126 * bg.r + 0.7152 * bg.g + 0.0722 * bg.b
	return Color.BLACK if lum > 0.56 else Color.WHITE

func _apply_main_menu_text_theme():
	var c = _main_text_color()
	var d = c.darkened(0.2)
	if title_label:
		title_label.add_theme_color_override("font_color", c)
	if pet_name_label:
		pet_name_label.add_theme_color_override("font_color", c)
	if pet_age_label:
		pet_age_label.add_theme_color_override("font_color", d)
	if pet_gender_label:
		pet_gender_label.add_theme_color_override("font_color", d)
	if actions_bar:
		for child in actions_bar.get_children():
			if child is Button:
				child.add_theme_color_override("font_color", c)
	if stats_panel:
		for child in stats_panel.get_children():
			if child is HBoxContainer:
				for row_child in child.get_children():
					if row_child is Label and str(row_child.name).ends_with("_label") and not str(row_child.name).ends_with("_value_label"):
						row_child.add_theme_color_override("font_color", c)

func _apply_detail_text_theme(root: Node):
	if root == null:
		return
	var c = _detail_text_color()
	if root is Label:
		(root as Label).add_theme_color_override("font_color", c)
	elif root is Button:
		(root as Button).add_theme_color_override("font_color", c)
	for child in root.get_children():
		_apply_detail_text_theme(child)

func _ready():
	position_window()
	
	# Create audio player
	sound_player = AudioStreamPlayer.new()
	add_child(sound_player)
	
	# Create sound manager
	sound_manager = SoundManager.new()
	add_child(sound_manager)
	sound_manager.set_enabled(settings.get("sound_effects", true))
	
	# Create environment stage layers (one slot initially; grows with pet count)
	_ensure_environment_stage_nodes(1)
	
	game_manager = GameManager.new()
	add_child(game_manager)
	
	game_manager.stats_updated.connect(_on_stats_updated)
	game_manager.pet_died.connect(_on_pet_died)
	game_manager.naming_needed.connect(_on_naming_needed)
	game_manager.pet_state_changed.connect(_on_pet_state_changed)
	game_manager.active_pet_changed.connect(_on_active_pet_changed)
	game_manager.pet_added.connect(_on_pet_added)
	
	create_ui()
	_apply_window_mode_layout(true)
	
	# Enable UI clicks by default
	_set_ui_clickable(true)
	
	# Add first pet with egg selection
	show_egg_selection()
	
	_on_stats_updated()
	update_environment_background()
	call_deferred("_finalize_startup_focus")

func _finalize_startup_focus():
	_apply_window_mode_layout(true)
	_set_environment_stages_visible(true)
	_set_ui_clickable(true)
	_apply_mouse_passthrough_for_mode(true)
	await get_tree().create_timer(1.5).timeout
	startup_focus_guard = false

func _process(delta):
	# Pet nodes are processed automatically by Godot since they're in the scene tree
	if not ui_visible:
		# Enforce unfocused presentation in case any UI controls were created after focus-out.
		_set_all_ui_controls_visible(false)
		_set_environment_stages_visible(true)
	
	# Update held item sprite to follow mouse
	if held_item_sprite and held_item.type != "":
		held_item_sprite.position = get_global_mouse_position()
	
	# Auto-save
	if settings.get("auto_save", true):
		auto_save_timer += delta
		if auto_save_timer >= settings.get("auto_save_interval", 60):
			auto_save_timer = 0
			_do_auto_save()

func _do_auto_save():
	# Auto-save to single file
	var save_data = {
		"pets": [],
		"in_memoriam": [],
		"timestamp": Time.get_unix_time_from_system(),
		"auto": true
	}
	
	for pet_data in game_manager.pet_datas:
		if pet_data:
			save_data["pets"].append({
				"name": pet_data.name,
				"animal_type": pet_data.animal_type,
				"egg_color": pet_data.egg_color,
				"gender": pet_data.gender,
				"age_minutes": pet_data.age_minutes,
				"hunger": pet_data.hunger,
				"hydration": pet_data.hydration,
				"happiness": pet_data.happiness,
				"energy": pet_data.energy,
				"health": pet_data.health,
				"cleanliness": pet_data.cleanliness,
				"affection": pet_data.affection,
				"grooming": pet_data.grooming,
				"fitness": pet_data.fitness,
				"conditions": pet_data.conditions,
				"water_bowl_level": pet_data.water_bowl_level,
				"is_dead": pet_data.is_dead,
				"is_sleeping": pet_data.is_sleeping,
				"is_hatching": pet_data.is_hatching,
				"emotion": pet_data.emotion,
				"position": {"x": pet_data.position.x, "y": pet_data.position.y},
				"personality": {
					"food_love": pet_data.food_love,
					"cuddle_need": pet_data.cuddle_need,
					"pet_cleanliness": pet_data.pet_cleanliness,
					"activity_level": pet_data.activity_level,
					"cheerfulness": pet_data.cheerfulness,
					"social_need": pet_data.social_need,
					"playfulness": pet_data.playfulness,
					"stubbornness": pet_data.stubbornness
				}
			})
	
	# Save in memoriam
	for memoriam_data in game_manager.in_memoriam:
		if memoriam_data:
			save_data["in_memoriam"].append({
				"name": memoriam_data.name,
				"animal_type": memoriam_data.animal_type,
				"gender": memoriam_data.gender,
				"age_at_death": memoriam_data.age_at_death,
				"death_sprite_path": memoriam_data.death_sprite_path
			})
	
	# Save settings
	save_data["settings"] = settings
	
	var save_path = "user://save_slot.json"
	var file = FileAccess.open(save_path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(save_data))
		file.close()

func close_all_overlays():
	if egg_selection_overlay:
		egg_selection_overlay.queue_free()
		egg_selection_overlay = null
	if action_menu_overlay:
		action_menu_overlay.queue_free()
		action_menu_overlay = null
	if action_tab_overlay:
		action_tab_overlay.queue_free()
		action_tab_overlay = null
		action_tab_open = false
		current_action_tab = ""
		action_tab_recall_pending_hold = false
		if stats_panel:
			stats_panel.visible = ui_visible
	if naming_overlay:
		naming_overlay.queue_free()
		naming_overlay = null
	if death_overlay:
		death_overlay.queue_free()
		death_overlay = null
	if memoriam_overlay:
		memoriam_overlay.queue_free()
		memoriam_overlay = null
	if settings_overlay:
		settings_overlay.queue_free()
		settings_overlay = null
	if doctors_note_overlay:
		doctors_note_overlay.queue_free()
		doctors_note_overlay = null
		doctors_note_visible = false
	if feeding_panel_overlay:
		feeding_panel_overlay.queue_free()
		feeding_panel_overlay = null
	egg_selection_active = false

func _fade_in_control(node: CanvasItem, duration_sec: float = UI_FADE_IN_SEC):
	if node == null:
		return
	node.modulate.a = 0.0
	var tween = create_tween()
	tween.tween_property(node, "modulate:a", 1.0, max(0.01, duration_sec))

func _fade_out_and_free(node: CanvasItem, duration_sec: float = UI_FADE_OUT_SEC):
	if node == null:
		return
	if node is Control:
		(node as Control).mouse_filter = Control.MOUSE_FILTER_IGNORE
	var tween = create_tween()
	tween.tween_property(node, "modulate:a", 0.0, max(0.01, duration_sec))
	tween.tween_callback(func():
		if is_instance_valid(node):
			node.queue_free()
	)

func _recenter_modal_card(overlay: Control):
	if overlay == null:
		return
	var card = overlay.get_node_or_null("modal_card") as Panel
	if card == null:
		return
	var win_size = get_window().size
	card.position = Vector2(
		max(8.0, (float(win_size.x) - card.size.x) * 0.5),
		max(8.0, (float(win_size.y) - card.size.y) * 0.5)
	)

func _create_priority_modal(card_size: Vector2, backdrop_alpha: float = 0.6) -> Dictionary:
	var overlay = Control.new()
	overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay.mouse_filter = Control.MOUSE_FILTER_STOP
	overlay.z_index = 500
	add_child(overlay)

	var overlay_bg = ColorRect.new()
	overlay_bg.color = Color(COLOR_BACKDROP.r, COLOR_BACKDROP.g, COLOR_BACKDROP.b, backdrop_alpha)
	overlay_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay.add_child(overlay_bg)

	var card = Panel.new()
	card.name = "modal_card"
	card.size = card_size
	var win_size = get_window().size
	card.position = Vector2(
		max(8.0, (float(win_size.x) - card_size.x) * 0.5),
		max(8.0, (float(win_size.y) - card_size.y) * 0.5)
	)
	var card_style = StyleBoxFlat.new()
	card_style.bg_color = COLOR_SURFACE
	card_style.border_color = Color(0.2, 0.24, 0.3, 1.0)
	card_style.set_border_width_all(1)
	card_style.set_corner_radius_all(8)
	card.add_theme_stylebox_override("panel", card_style)
	overlay.add_child(card)
	_fade_in_control(overlay, UI_FADE_IN_SEC)

	return {"overlay": overlay, "card": card}

func show_egg_selection():
	if not game_manager.can_add_pet():
		return
	
	close_all_overlays()
	egg_selection_active = true
	
	var modal = _create_priority_modal(Vector2(272, 260), 0.6)
	egg_selection_overlay = modal["overlay"]
	var card = modal["card"] as Panel
	
	# Title
	var title = Label.new()
	title.text = "Choose an Egg"
	title.add_theme_font_size_override("font_size", 16)
	title.add_theme_color_override("font_color", _detail_text_color())
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(0, 12)
	title.size = Vector2(272, 28)
	card.add_child(title)
	
	# Egg container
	var egg_container = HBoxContainer.new()
	egg_container.position = Vector2(14, 70)
	egg_container.add_theme_constant_override("separation", 10)
	egg_container.alignment = BoxContainer.ALIGNMENT_CENTER
	card.add_child(egg_container)
	
	# Create egg buttons
	var egg_colors = ["red", "orange", "yellow", "blue", "indigo", "violet"]
	for egg_color in egg_colors:
		var egg_btn = Button.new()
		egg_btn.custom_minimum_size = Vector2(36, 44)
		egg_btn.pressed.connect(_on_egg_selected.bind(egg_color))
		
		# Create egg appearance
		var egg_style = StyleBoxFlat.new()
		egg_style.bg_color = EGG_COLORS[egg_color]
		egg_style.set_corner_radius_all(20)
		egg_style.border_width_left = 2
		egg_style.border_width_right = 2
		egg_style.border_width_top = 2
		egg_style.border_width_bottom = 2
		egg_style.border_color = EGG_COLORS[egg_color].darkened(0.3)
		egg_btn.add_theme_stylebox_override("normal", egg_style)
		
		var hover_style = egg_style.duplicate()
		hover_style.border_color = _detail_text_color()
		egg_btn.add_theme_stylebox_override("hover", hover_style)
		
		egg_container.add_child(egg_btn)

	var hint = Label.new()
	hint.text = "Pick a color to hatch your next pet"
	hint.add_theme_font_size_override("font_size", 10)
	hint.add_theme_color_override("font_color", _detail_text_color())
	hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hint.position = Vector2(8, 180)
	hint.size = Vector2(256, 24)
	card.add_child(hint)

func _on_egg_selected(egg_color: String):
	if egg_selection_overlay:
		_fade_out_and_free(egg_selection_overlay, UI_FADE_OUT_SEC)
		egg_selection_overlay = null
	egg_selection_active = false
	
	# Add pet with selected egg color
	add_new_pet_with_color(egg_color)
	
	# Play hatch sound with a delay (hatch takes 3 seconds)
	await get_tree().create_timer(2.5).timeout
	var pd = game_manager.get_active_pet_data()
	if pd and sound_manager:
		sound_manager.play_hatch_sound(pd.gender)

func add_new_pet_with_color(selected_color: String):
	if not game_manager.can_add_pet():
		return
	
	var pet = Pet.new()
	var pet_index = game_manager.get_pet_count()
	pet.position = Vector2(80 + pet_index * 100, 280)
	add_child(pet)
	
	# Set the egg color before adding
	game_manager.add_pet_with_color(pet, selected_color)
	_set_pet_floor(float(get_window().size.y) - PET_FLOOR_INSET)

func add_new_pet():
	# This now shows egg selection
	show_egg_selection()

func position_window():
	if window_positioned:
		return
	window_positioned = true
	
	await get_tree().create_timer(0.1).timeout
	
	_pin_window_bottom_right()

func _pin_window_bottom_right():
	var screen = DisplayServer.window_get_current_screen()
	var usable_rect = DisplayServer.screen_get_usable_rect(screen)
	var screen_size = usable_rect.size
	var window_size = get_window().get_size()
	var margin = WINDOW_MARGIN
	get_window().position = Vector2i(
		int(usable_rect.position.x + screen_size.x - window_size.x - margin.x),
		int(usable_rect.position.y + screen_size.y - window_size.y - margin.y)
	)

func _monitor_roam_requested() -> bool:
	# Monitor-wide roaming is currently disabled due compositor/input instability
	# on some Windows setups (black box/invisible pet/fullscreen UI lock).
	return false

func _attempt_monitor_roam_layout() -> bool:
	var screen = DisplayServer.window_get_current_screen()
	var screen_size = DisplayServer.screen_get_size(screen)
	get_window().size = Vector2i(screen_size)
	get_window().position = Vector2i.ZERO
	var actual = get_window().size
	return actual.x >= int(screen_size.x * 0.9) and actual.y >= int(screen_size.y * 0.9)

func _apply_window_mode_layout(focused: bool):
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	if focused:
		get_window().size = WINDOW_FOCUSED_SIZE
		_pin_window_bottom_right()
		monitor_roam_active = false
	else:
		# Stable unfocused companion mode: compact bottom-right window.
		monitor_roam_active = false
		# Keep full companion visuals visible while UI is hidden.
		get_window().size = WINDOW_FOCUSED_SIZE
		_pin_window_bottom_right()
	_apply_responsive_ui_layout(get_window().size.y)
	_apply_environment_layout(get_window().size.y)
	_apply_pet_bounds_for_mode(focused)

func _apply_responsive_ui_layout(window_height: int):
	var w = float(get_window().size.x)
	var h = float(window_height)
	var top_zone_h = h * TOP_ZONE_PCT
	var hud_top = top_zone_h
	var hud_h = h * HUD_ZONE_PCT
	var hud_bottom = hud_top + hud_h
	var content_w = max(120.0, w - (HUD_PADDING_X * 2.0))

	var icon_size = Vector2(28, 24)
	var icon_gap = 4.0
	var top_row_y = 6.0
	var second_row_y = 30.0
	var right_x = w - HUD_PADDING_X - icon_size.x
	var right_row3_start = right_x - ((icon_size.x + icon_gap) * 2.0)
	var center_left = HUD_PADDING_X + icon_size.x + 8.0
	var center_right = right_row3_start - 8.0
	var center_w = max(96.0, center_right - center_left)
	var center_mid = center_left + (center_w * 0.5)
	var nav_y = second_row_y + 1.0
	var indicators_x = center_mid + 16.0
	var identity_y = hud_top + IDENTITY_ROW_Y_OFFSET
	var name_w = max(96.0, content_w * IDENTITY_NAME_RATIO)
	var age_x = HUD_PADDING_X + name_w + IDENTITY_GENDER_WIDTH + (IDENTITY_INNER_GAP * 2.0)
	var age_w = max(72.0, content_w - (name_w + IDENTITY_GENDER_WIDTH + (IDENTITY_INNER_GAP * 2.0)))

	if doctor_button:
		doctor_button.position = Vector2(HUD_PADDING_X, top_row_y)
	if minimize_button:
		minimize_button.position = Vector2(HUD_PADDING_X, second_row_y)
	if add_pet_button:
		add_pet_button.position = Vector2(right_x, top_row_y)
	if ghost_button:
		ghost_button.position = Vector2(right_row3_start, second_row_y)
	if memoriam_button:
		memoriam_button.position = Vector2(right_row3_start + icon_size.x + icon_gap, second_row_y)
	if settings_button:
		settings_button.position = Vector2(right_x, second_row_y)

	if title_label:
		title_label.position = Vector2(center_left, 8)
		title_label.size = Vector2(center_w, 16)

	if pet_indicators:
		pet_indicators.position = Vector2(indicators_x, nav_y + 2.0)
		pet_indicators.size = Vector2(max(40.0, center_right - indicators_x), 14)

	if pet_name_label:
		pet_name_label.position = Vector2(HUD_PADDING_X, identity_y)
		pet_name_label.size = Vector2(name_w, 14)

	if pet_gender_label:
		pet_gender_label.position = Vector2(HUD_PADDING_X + name_w + IDENTITY_INNER_GAP, identity_y)
		pet_gender_label.size = Vector2(IDENTITY_GENDER_WIDTH, 14)
		pet_gender_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER

	if pet_age_label:
		pet_age_label.position = Vector2(age_x, identity_y)
		pet_age_label.size = Vector2(age_w, 14)

	if stats_panel:
		var stats_top = hud_top + 26
		var actions_h = HUD_ACTIONS_HEIGHT
		var actions_top = hud_bottom - actions_h
		var stats_h = max(48.0, actions_top - stats_top - HUD_ACTIONS_GAP)
		stats_panel.position = Vector2(HUD_PADDING_X, stats_top)
		stats_panel.size = Vector2(content_w, stats_h)

	if actions_bar:
		actions_bar.position = Vector2(HUD_PADDING_X, hud_bottom - HUD_ACTIONS_HEIGHT)
		actions_bar.size = Vector2(content_w, HUD_ACTIONS_HEIGHT)
		_layout_action_buttons()

	if nav_arrows:
		nav_arrows.position = Vector2(center_mid - 30.0, nav_y)

	if action_tab_overlay and action_tab_open and stats_panel:
		action_tab_overlay.position = stats_panel.position
		action_tab_overlay.size = stats_panel.size

func _layout_action_buttons():
	if actions_bar == null:
		return

	var buttons: Array[Button] = []
	for child in actions_bar.get_children():
		if child is Button:
			buttons.append(child)

	if buttons.is_empty():
		return

	var total_gap = HUD_ACTION_BUTTON_GAP * float(max(0, buttons.size() - 1))
	var usable_w = max(60.0, actions_bar.size.x - total_gap)
	var btn_w = floor(usable_w / float(buttons.size()))

	for btn in buttons:
		btn.custom_minimum_size = Vector2(btn_w, 20)

func _ensure_environment_stage_nodes(required_count: int):
	var target = max(1, required_count)
	while environment_stages.size() < target:
		var stage_bg = ColorRect.new()
		stage_bg.color = COLOR_BG
		stage_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
		stage_bg.z_index = -2
		add_child(stage_bg)

		var stage_mid = ColorRect.new()
		stage_mid.color = Color(0.18, 0.2, 0.21)
		stage_mid.mouse_filter = Control.MOUSE_FILTER_IGNORE
		stage_mid.z_index = -2
		add_child(stage_mid)

		var stage_ground = ColorRect.new()
		stage_ground.color = Color(0.06, 0.07, 0.08)
		stage_ground.mouse_filter = Control.MOUSE_FILTER_IGNORE
		stage_ground.z_index = -2
		add_child(stage_ground)

		environment_stages.append({"bg": stage_bg, "mid": stage_mid, "ground": stage_ground})

	while environment_stages.size() > target:
		var stage = environment_stages.pop_back()
		for key in ["ground", "mid", "bg"]:
			var node = stage.get(key)
			if node:
				node.queue_free()

	if environment_stages.size() > 0:
		background = environment_stages[0]["bg"]
		bg_mid = environment_stages[0]["mid"]
		bg_ground = environment_stages[0]["ground"]

func _get_environment_slot_rects(window_width: float, window_height: float, count: int) -> Array[Rect2]:
	var slots: Array[Rect2] = []
	var c = max(1, count)
	var pet_top = window_height * (TOP_ZONE_PCT + HUD_ZONE_PCT + GAP_ZONE_PCT)
	var stage_top = pet_top + STAGE_TOP_INSET
	var stage_h = max(24.0, window_height - stage_top)

	var stage_gap = 8.0
	var stage_w = clamp(window_width * STAGE_WIDTH_RATIO, STAGE_MIN_WIDTH, STAGE_MAX_WIDTH)
	var total_w = (stage_w * c) + (stage_gap * max(0, c - 1))
	if total_w > (window_width - (HUD_PADDING_X * 2.0)):
		stage_w = max(56.0, ((window_width - (HUD_PADDING_X * 2.0)) - (stage_gap * max(0, c - 1))) / c)
		total_w = (stage_w * c) + (stage_gap * max(0, c - 1))
	var start_x = (window_width - total_w) * 0.5

	for i in range(c):
		slots.append(Rect2(Vector2(start_x + (i * (stage_w + stage_gap)), stage_top), Vector2(stage_w, stage_h)))
	return slots

func _apply_pet_bounds_for_mode(focused: bool):
	if game_manager == null:
		return
	if focused or not monitor_roam_active:
		var slots = _get_environment_slot_rects(float(get_window().size.x), float(get_window().size.y), game_manager.get_pet_count())
		for i in range(game_manager.pets.size()):
			var pet = game_manager.pets[i]
			if pet == null:
				continue
			if i < slots.size() and pet.has_method("set_wander_bounds"):
				var s = slots[i]
				pet.set_wander_bounds(Rect2(s.position.x + 8.0, s.position.y, max(12.0, s.size.x - 16.0), s.size.y))
	else:
		var w = float(get_window().size.x)
		var h = float(get_window().size.y)
		for pet in game_manager.pets:
			if pet and pet.has_method("set_wander_bounds"):
				pet.set_wander_bounds(Rect2(MONITOR_ROAM_MARGIN_X, 0.0, max(24.0, w - (MONITOR_ROAM_MARGIN_X * 2.0)), max(24.0, h - MONITOR_ROAM_MARGIN_Y)))

func _set_environment_stages_visible(should_show: bool):
	for stage in environment_stages:
		for key in ["bg", "mid", "ground"]:
			var node = stage.get(key)
			if node:
				node.visible = should_show

func _is_environment_stage_control(ctrl: Control) -> bool:
	for stage in environment_stages:
		for key in ["bg", "mid", "ground"]:
			if stage.get(key) == ctrl:
				return true
	return false

func _set_all_ui_controls_visible(should_show: bool):
	for child in get_children():
		if child is Control:
			var ctrl = child as Control
			if _is_environment_stage_control(ctrl):
				continue
			ctrl.visible = should_show

func _recall_all_pets_home(hold_seconds: float = 0.0):
	if game_manager == null:
		return
	var slots = _get_environment_slot_rects(float(get_window().size.x), float(get_window().size.y), game_manager.get_pet_count())
	for i in range(game_manager.pets.size()):
		var pet = game_manager.pets[i]
		if pet == null:
			continue
		if i < slots.size() and pet.has_method("move_to_home"):
			var center_x = slots[i].position.x + (slots[i].size.x * 0.5)
			pet.move_to_home(center_x, hold_seconds)

func _start_monitor_roam_all_pets():
	if game_manager == null:
		return
	for pet in game_manager.pets:
		if pet and pet.has_method("start_wandering"):
			pet.start_wandering()

func _apply_environment_layout(window_height: int):
	var w = float(get_window().size.x)
	var h = float(window_height)
	var count = max(1, game_manager.get_pet_count()) if game_manager else 1
	_ensure_environment_stage_nodes(count)
	var slots = _get_environment_slot_rects(w, h, count)

	for i in range(environment_stages.size()):
		var stage = environment_stages[i]
		var slot = slots[i] if i < slots.size() else Rect2(Vector2.ZERO, Vector2.ZERO)
		var x = slot.position.x
		var stage_top = slot.position.y
		var stage_w = slot.size.x
		var stage_h = slot.size.y
		var ground_top = stage_top + (stage_h * 0.48)
		var mid_top = stage_top + (stage_h * 0.2)

		var stage_bg = stage["bg"] as ColorRect
		var stage_mid = stage["mid"] as ColorRect
		var stage_ground = stage["ground"] as ColorRect

		if stage_bg:
			stage_bg.position = Vector2(x, stage_top)
			stage_bg.size = Vector2(stage_w, stage_h)
		if stage_mid:
			stage_mid.position = Vector2(x, mid_top)
			stage_mid.size = Vector2(stage_w, max(0.0, ground_top - mid_top))
		if stage_ground:
			stage_ground.position = Vector2(x, ground_top)
			stage_ground.size = Vector2(stage_w, max(0.0, h - ground_top))

	var floor_y = h - PET_FLOOR_INSET
	_set_pet_floor(floor_y)

func _set_pet_floor(floor_y: float):
	if game_manager == null:
		return
	for pet in game_manager.pets:
		if pet and pet.has_method("set_floor_y"):
			pet.set_floor_y(floor_y)

func _apply_mouse_passthrough_for_mode(focused: bool):
	if focused:
		get_window().mouse_passthrough_polygon = PackedVector2Array()
		return

	# In stable compact unfocused mode, keep window clickable so focus can return.
	if not monitor_roam_active:
		get_window().mouse_passthrough_polygon = PackedVector2Array()
		return

	# Respect setting for monitor roam mode.
	if not settings.get("click_through", false):
		get_window().mouse_passthrough_polygon = PackedVector2Array()
		return

	if game_manager.pets.is_empty():
		get_window().mouse_passthrough_polygon = PackedVector2Array()
		return

	var min_x = 99999.0
	var max_x = -99999.0
	var min_y = 99999.0
	var max_y = -99999.0
	for pet in game_manager.pets:
		if pet == null:
			continue
		min_x = min(min_x, pet.position.x - 36.0)
		max_x = max(max_x, pet.position.x + 36.0)
		min_y = min(min_y, pet.position.y - 48.0)
		max_y = max(max_y, pet.position.y + 20.0)

	min_x = clamp(min_x, 0.0, float(get_window().size.x))
	max_x = clamp(max_x, 0.0, float(get_window().size.x))
	min_y = clamp(min_y, 0.0, float(get_window().size.y))
	max_y = clamp(max_y, 0.0, float(get_window().size.y))

	if max_x <= min_x or max_y <= min_y:
		get_window().mouse_passthrough_polygon = PackedVector2Array()
		return

	get_window().mouse_passthrough_polygon = PackedVector2Array([
		Vector2(min_x, min_y),
		Vector2(max_x, min_y),
		Vector2(max_x, max_y),
		Vector2(min_x, max_y),
	])

func _set_ui_clickable(clickable: bool):
	# Set mouse filter on UI elements to enable/disable clicks
	# MOUSE_FILTER_STOP = clicks pass through to underlying elements
	# MOUSE_FILTER_IGNORE = clicks are ignored
	# MOUSE_FILTER_PASS = clicks pass to parent
	var filter = Control.MOUSE_FILTER_STOP if clickable else Control.MOUSE_FILTER_IGNORE
	
	# Overlays
	if stats_panel:
		stats_panel.mouse_filter = filter
	if actions_bar:
		actions_bar.mouse_filter = filter
	if action_tab_overlay:
		action_tab_overlay.mouse_filter = filter
	if action_menu_overlay:
		action_menu_overlay.mouse_filter = filter
	if settings_overlay:
		settings_overlay.mouse_filter = filter
	if memoriam_overlay:
		memoriam_overlay.mouse_filter = filter
	if death_overlay:
		death_overlay.mouse_filter = filter
	if naming_overlay:
		naming_overlay.mouse_filter = filter
	if egg_selection_overlay:
		egg_selection_overlay.mouse_filter = filter
	
	# Top UI elements
	if title_label:
		title_label.mouse_filter = filter
	if pet_name_label:
		pet_name_label.mouse_filter = filter
	if pet_gender_label:
		pet_gender_label.mouse_filter = filter
	if pet_age_label:
		pet_age_label.mouse_filter = filter
	if nav_arrows:
		nav_arrows.mouse_filter = filter
	if add_pet_button:
		add_pet_button.mouse_filter = filter
	if doctor_button:
		doctor_button.mouse_filter = filter
	if minimize_button:
		minimize_button.mouse_filter = filter
	if settings_button:
		settings_button.mouse_filter = filter
	if ghost_button:
		ghost_button.mouse_filter = filter
	if memoriam_button:
		memoriam_button.mouse_filter = filter
	if pet_indicators:
		pet_indicators.mouse_filter = filter
	
	# Set all children of actions_bar to ignore clicks when not focused
	if actions_bar:
		_set_container_clicks(actions_bar, clickable)

func _set_container_clicks(container: Control, clickable: bool):
	if container == null:
		return
	var filter = Control.MOUSE_FILTER_STOP if clickable else Control.MOUSE_FILTER_IGNORE
	container.mouse_filter = filter
	for child in container.get_children():
		if child is Control:
			child.mouse_filter = filter

func _notification(what):
	if what == NOTIFICATION_WM_WINDOW_FOCUS_IN:
		_handle_focus_change(true)
	elif what == NOTIFICATION_WM_WINDOW_FOCUS_OUT:
		if startup_focus_guard:
			return
		_handle_focus_change(false)
	elif what == NOTIFICATION_WM_SIZE_CHANGED:
		_apply_responsive_ui_layout(get_window().size.y)
		_apply_environment_layout(get_window().size.y)
		_apply_pet_bounds_for_mode(ui_visible)
		_recenter_modal_card(egg_selection_overlay)
		_recenter_modal_card(naming_overlay)
		_recenter_modal_card(death_overlay)
		_recenter_modal_card(settings_overlay)

func _handle_focus_change(focused: bool):
	# Save current tab state when losing focus
	if not focused and action_tab_open:
		last_open_action_tab = current_action_tab
	if not focused and action_tab_overlay:
		action_tab_overlay.visible = false
	
	if focused != ui_visible:
		ui_visible = focused
		
		if stats_panel:
			stats_panel.visible = focused
		if actions_bar:
			actions_bar.visible = focused
		if title_label:
			title_label.visible = focused
		if pet_name_label:
			pet_name_label.visible = focused
		if pet_gender_label:
			pet_gender_label.visible = focused
		if pet_age_label:
			pet_age_label.visible = focused
		if nav_arrows:
			nav_arrows.visible = focused
		if add_pet_button:
			add_pet_button.visible = focused
		if doctor_button:
			doctor_button.visible = focused
		if minimize_button:
			minimize_button.visible = focused
		if settings_button:
			settings_button.visible = focused
		if ghost_button:
			ghost_button.visible = focused
		if memoriam_button:
			memoriam_button.visible = focused
		if pet_indicators:
			pet_indicators.visible = focused
		if action_tab_overlay:
			action_tab_overlay.visible = focused
		if action_menu_overlay:
			action_menu_overlay.visible = focused
		if settings_overlay:
			settings_overlay.visible = focused
		if memoriam_overlay:
			memoriam_overlay.visible = focused
		if doctors_note_overlay:
			doctors_note_overlay.visible = focused
		if egg_selection_overlay:
			egg_selection_overlay.visible = focused
		if naming_overlay:
			naming_overlay.visible = focused
		if death_overlay:
			death_overlay.visible = focused
		if feeding_panel_overlay:
			feeding_panel_overlay.visible = focused
		if ghost_overlay:
			ghost_overlay.visible = focused
		
		# Restore previous action tab when focus returns (unless a priority modal is open)
		if focused and last_open_action_tab != "" and egg_selection_overlay == null and naming_overlay == null and death_overlay == null:
			_show_action_tab(last_open_action_tab)

	# Keep all non-environment UI controls in sync even if focus events duplicate.
	_set_all_ui_controls_visible(focused)
	
	_apply_window_mode_layout(focused)
	_set_environment_stages_visible(focused or not monitor_roam_active)
	if focused:
		_recall_all_pets_home()
	else:
		if monitor_roam_active:
			_start_monitor_roam_all_pets()
		else:
			_recall_all_pets_home(0.0)
	
	if not focused:
		_set_ui_clickable(false)
	else:
		_set_ui_clickable(true)

	_apply_mouse_passthrough_for_mode(focused)

func create_ui():
	# Title
	title_label = Label.new()
	title_label.text = "WEVITO"
	title_label.add_theme_font_size_override("font_size", 12)
	title_label.add_theme_color_override("font_color", COLOR_TEXT)
	title_label.position = Vector2(46, 8)
	title_label.size = Vector2(168, 16)
	title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(title_label)
	
	# Pet navigation arrows
	nav_arrows = HBoxContainer.new()
	nav_arrows.position = Vector2(100, 31)
	nav_arrows.add_theme_constant_override("separation", 8)
	add_child(nav_arrows)
	
	var prev_btn = Button.new()
	prev_btn.text = "<"
	prev_btn.custom_minimum_size = Vector2(20, 16)
	prev_btn.pressed.connect(_on_prev_pet)
	nav_arrows.add_child(prev_btn)
	
	var next_btn = Button.new()
	next_btn.text = ">"
	next_btn.custom_minimum_size = Vector2(20, 16)
	next_btn.pressed.connect(_on_next_pet)
	nav_arrows.add_child(next_btn)
	
	# Pet indicators
	pet_indicators = HBoxContainer.new()
	pet_indicators.position = Vector2(146, 33)
	pet_indicators.size = Vector2(68, 14)
	pet_indicators.clip_contents = true
	pet_indicators.alignment = BoxContainer.ALIGNMENT_CENTER
	pet_indicators.add_theme_constant_override("separation", 4)
	add_child(pet_indicators)
	update_pet_indicators()
	
	# Top button bar - use icon buttons
	# Minimize button (hide to tray) - left side
	minimize_button = create_icon_button("minimize", _minimize_window, "", "Hide to tray (Ctrl+Shift+W to show)")
	add_child(minimize_button)
	minimize_button.position = Vector2(10, 30)
	
	# Doctor's Note button
	doctor_button = create_icon_button("doctor", _toggle_doctors_note, "", "Doctor's Note")
	add_child(doctor_button)
	doctor_button.position = Vector2(10, 6)
	
	# Add pet button - right side area
	add_pet_button = create_icon_button("add", _on_add_pet, "", "Add new pet")
	add_pet_button.position = Vector2(282, 6)
	add_child(add_pet_button)
	
	# Settings button
	settings_button = create_icon_button("settings", _show_settings_menu, "", "Settings")
	settings_button.position = Vector2(282, 30)
	add_child(settings_button)
	
	# Ghost mode button (moved to make room)
	ghost_button = create_icon_button("ghost", _toggle_ghost_mode, "", "Ghost mode")
	ghost_button.position = Vector2(218, 30)
	add_child(ghost_button)
	
	# In Memoriam button
	memoriam_button = create_icon_button("memoriam", _show_in_memoriam, "", "In Memoriam")
	memoriam_button.position = Vector2(250, 30)
	add_child(memoriam_button)
	
	# Pet name
	pet_name_label = Label.new()
	pet_name_label.add_theme_font_size_override("font_size", 10)
	pet_name_label.add_theme_color_override("font_color", _main_text_color())
	pet_name_label.position = Vector2(10, 48)
	pet_name_label.size = Vector2(146, 14)
	pet_name_label.clip_text = true
	pet_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	add_child(pet_name_label)

	# Pet gender marker (separate from name)
	pet_gender_label = Label.new()
	pet_gender_label.add_theme_font_size_override("font_size", 9)
	pet_gender_label.add_theme_color_override("font_color", _main_text_color().darkened(0.2))
	pet_gender_label.position = Vector2(160, 48)
	pet_gender_label.size = Vector2(20, 14)
	pet_gender_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(pet_gender_label)

	# Pet age
	pet_age_label = Label.new()
	pet_age_label.add_theme_font_size_override("font_size", 10)
	pet_age_label.add_theme_color_override("font_color", _main_text_color().darkened(0.2))
	pet_age_label.position = Vector2(186, 48)
	pet_age_label.size = Vector2(130, 14)
	pet_age_label.clip_text = true
	pet_age_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	add_child(pet_age_label)
	
	# Stats panel (upper UI area, above pet/environment)
	stats_panel = VBoxContainer.new()
	stats_panel.position = Vector2(10, 70)
	stats_panel.size = Vector2(300, 126)
	stats_panel.clip_contents = true
	stats_panel.add_theme_constant_override("separation", 2)
	add_child(stats_panel)
	
	var stats = [
		["HUNGER", "hunger"],
		["WATER", "hydration"],
		["JOY", "happiness"],
		["ENERGY", "energy"],
		["HEALTH", "health"],
		["CLEAN", "cleanliness"],
		["LOVE", "affection"],
		["GROOM", "grooming"],
		["FIT", "fitness"]
	]
	
	for stat_info in stats:
		var stat_container = create_stat_bar(stat_info[0], stat_info[1])
		stats_panel.add_child(stat_container)
	
	# Actions bar
	actions_bar = HBoxContainer.new()
	actions_bar.position = Vector2(10, 210)
	actions_bar.size = Vector2(300, 22)
	actions_bar.clip_contents = true
	actions_bar.alignment = BoxContainer.ALIGNMENT_CENTER
	actions_bar.add_theme_constant_override("separation", int(HUD_ACTION_BUTTON_GAP))
	add_child(actions_bar)
	
	var actions = [
		["Feed", "feed"],
		["Pet", "pet"],
		["Rest", "rest"],
		["Groom", "groom"],
		["Ex", "exercise"],
		["Med", "medicine"]
	]
	
	for action_info in actions:
		var btn = Button.new()
		btn.text = action_info[0]
		btn.custom_minimum_size = Vector2(40, 20)
		btn.pressed.connect(_on_action_pressed.bind(action_info[1]))
		btn.add_theme_font_size_override("font_size", 8)
		btn.add_theme_color_override("font_color", _main_text_color())
		btn.add_theme_stylebox_override("normal", create_button_style_normal())
		btn.add_theme_stylebox_override("hover", create_button_style_hover())
		btn.add_theme_stylebox_override("pressed", create_button_style_pressed())
		actions_bar.add_child(btn)

	_layout_action_buttons()

	_apply_main_menu_text_theme()

func update_pet_indicators():
	# Clear existing
	for child in pet_indicators.get_children():
		child.queue_free()
	
	# Add indicator for each pet
	for i in range(game_manager.get_pet_count()):
		var indicator = Label.new()
		var is_active = (i == game_manager.active_pet_index)
		indicator.text = str(i + 1)
		indicator.add_theme_font_size_override("font_size", 10)
		if is_active:
			indicator.add_theme_color_override("font_color", _main_text_color())
		else:
			indicator.add_theme_color_override("font_color", _main_text_color().darkened(0.35))
		pet_indicators.add_child(indicator)

func create_stat_bar(label_text, stat_name):
	var container = HBoxContainer.new()
	container.custom_minimum_size = Vector2(280, 12)
	container.add_theme_constant_override("separation", 8)
	container.name = stat_name + "_row"

	var label = Label.new()
	label.name = stat_name + "_label"
	label.text = label_text
	label.custom_minimum_size = Vector2(96, 12)
	label.add_theme_font_size_override("font_size", 9)
	label.add_theme_color_override("font_color", _main_text_color())
	container.add_child(label)

	var box_container = Control.new()
	box_container.custom_minimum_size = Vector2(36, 12)
	container.add_child(box_container)

	var value_box = ColorRect.new()
	value_box.name = stat_name + "_value_box"
	value_box.color = _status_box_color(50)
	value_box.set_anchors_preset(Control.PRESET_FULL_RECT)
	box_container.add_child(value_box)

	var value_label = Label.new()
	value_label.name = stat_name + "_value_label"
	value_label.text = "50%"
	value_label.set_anchors_preset(Control.PRESET_FULL_RECT)
	value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	value_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	value_label.add_theme_font_size_override("font_size", 8)
	value_label.add_theme_color_override("font_color", _contrast_text_color(value_box.color))
	box_container.add_child(value_label)
	
	return container

func load_icon_texture(icon_name: String) -> Texture2D:
	var path = "res://sprites/icons/" + icon_name + ".png"
	if ResourceLoader.exists(path):
		return load(path)
	return null

func create_icon_button(icon_name: String, action_callback: Callable, action_arg: String = "", tooltip: String = "") -> Button:
	var btn = Button.new()
	btn.custom_minimum_size = Vector2(28, 24)
	
	var tex = load_icon_texture(icon_name)
	if tex:
		btn.icon = tex
		btn.icon_alignment = HORIZONTAL_ALIGNMENT_CENTER
		# btn.icon_max_width = 20
	
	if tooltip != "":
		btn.tooltip_text = tooltip
	
	if action_arg != "":
		btn.pressed.connect(action_callback.bind(action_arg))
	else:
		btn.pressed.connect(action_callback)
	
	btn.add_theme_stylebox_override("normal", create_button_style_normal())
	btn.add_theme_stylebox_override("hover", create_button_style_hover())
	btn.add_theme_stylebox_override("pressed", create_button_style_pressed())
	
	return btn

func create_action_button(label_text, action_name):
	var btn = Button.new()
	btn.text = label_text
	btn.custom_minimum_size = Vector2(40, 24)
	btn.pressed.connect(_on_action_pressed.bind(action_name))
	
	btn.add_theme_font_size_override("font_size", 16)
	btn.add_theme_color_override("font_color", COLOR_TEXT)
	btn.add_theme_color_override("font_hover_color", COLOR_TEXT)
	btn.add_theme_color_override("font_pressed_color", COLOR_TEXT)
	btn.add_theme_stylebox_override("normal", create_button_style_normal())
	btn.add_theme_stylebox_override("hover", create_button_style_hover())
	btn.add_theme_stylebox_override("pressed", create_button_style_pressed())
	
	return btn

func create_button_style_normal():
	var style = StyleBoxFlat.new()
	style.bg_color = Color(COLOR_SURFACE.r, COLOR_SURFACE.g, COLOR_SURFACE.b, 0.9)
	style.border_color = COLOR_BORDER
	style.set_border_width_all(1)
	style.set_corner_radius_all(2)
	return style

func create_button_style_hover():
	var style = create_button_style_normal()
	style.border_color = COLOR_TEXT
	style.bg_color = Color(0.16, 0.16, 0.29, 0.9)
	return style

func create_button_style_pressed():
	var style = create_button_style_normal()
	style.bg_color = COLOR_TEXT
	return style

func _on_stats_updated():
	var pd = game_manager.get_active_pet_data()
	if pd == null:
		if pet_name_label:
			pet_name_label.text = "No Pet"
			pet_name_label.tooltip_text = ""
		if pet_gender_label:
			pet_gender_label.text = ""
		if pet_age_label:
			pet_age_label.text = ""
		return
	
	var gender_symbol = "M" if pd.gender == "male" else "F"
	if pet_name_label:
		pet_name_label.text = _truncate_with_ellipsis(pd.name, 14)
		pet_name_label.tooltip_text = pd.name
	if pet_gender_label:
		pet_gender_label.text = "[" + gender_symbol + "]"
	
	var age_mins = pd.age_minutes
	var age_text = str(age_mins) + "m"
	if age_mins >= 60:
		age_text = str(age_mins / 60) + "h"
	if age_mins >= 60 * 24:
		age_text = str(age_mins / (60 * 24)) + "d"
	
	var stage_text = pd.get_stage_name()
	pet_age_label.text = stage_text + " | " + age_text
	
	update_stat_bar("hunger", pd.hunger)
	update_stat_bar("hydration", pd.hydration)
	update_stat_bar("happiness", pd.happiness)
	update_stat_bar("energy", pd.energy)
	update_stat_bar("health", pd.health)
	update_stat_bar("cleanliness", pd.cleanliness)
	update_stat_bar("affection", pd.affection)
	update_stat_bar("grooming", pd.grooming)
	update_stat_bar("fitness", pd.fitness)
	if action_tab_open:
		_populate_action_focus_stats(current_action_tab)
	
	update_pet_indicators()
	update_add_button()

func update_stat_bar(stat_name, value):
	var value_box = stats_panel.find_child(stat_name + "_value_box", true, false)
	if value_box:
		value_box.color = _status_box_color(value)

	var value_label = stats_panel.find_child(stat_name + "_value_label", true, false)
	if value_label:
		value_label.text = str(int(value)) + "%"
		if value_box:
			value_label.add_theme_color_override("font_color", _contrast_text_color(value_box.color))
	
	var label = stats_panel.find_child(stat_name + "_label", true, false)
	if label:
		var stat_label = stat_name.to_upper()
		if stat_label == "HAPPINESS":
			stat_label = "JOY"
		elif stat_label == "AFFECTION":
			stat_label = "LOVE"
		elif stat_label == "CLEANLINESS":
			stat_label = "CLEAN"
		elif stat_label == "GROOMING":
			stat_label = "GROOM"
		elif stat_label == "FITNESS":
			stat_label = "FIT"
		label.text = stat_label
		label.add_theme_color_override("font_color", _main_text_color())

func update_add_button():
	add_pet_button.disabled = not game_manager.can_add_pet()
	add_pet_button.visible = game_manager.can_add_pet()

func _play_action_sound(action_name: String):
	if sound_manager == null:
		return
	
	var pd = game_manager.get_active_pet_data()
	if pd == null:
		return
	
	var animal = pd.animal_type
	var gender = pd.gender
	
	match action_name:
		"feed":
			sound_manager.play_sound(animal, "feed", gender)
		"pet":
			sound_manager.play_sound(animal, "pet", gender)
		"rest":
			sound_manager.play_sound(animal, "rest", gender)
		"groom":
			sound_manager.play_sound(animal, "groom", gender)
		"bathe":
			sound_manager.play_sound(animal, "bathe", gender)
		"exercise":
			sound_manager.play_sound(animal, "exercise", gender)

func _on_action_pressed(action_name):
	# Show action tab instead of directly performing action
	_show_action_tab(action_name)

func _show_action_tab(action_name: String):
	close_all_overlays()
	action_tab_recall_pending_hold = true
	_recall_all_pets_home(0.0)
	if stats_panel:
		stats_panel.visible = false
	
	# Track current tab
	action_tab_open = true
	current_action_tab = action_name
	
	action_tab_overlay = Control.new()
	action_tab_overlay.position = stats_panel.position if stats_panel else Vector2(10, 52)
	action_tab_overlay.size = stats_panel.size if stats_panel else Vector2(300, 160)
	action_tab_overlay.clip_contents = true
	add_child(action_tab_overlay)
	_fade_in_control(action_tab_overlay, TAB_SWAP_SEC)
	
	# Panel background (replaces stats area only)
	var menu_bg = ColorRect.new()
	menu_bg.color = COLOR_OVERLAY_PANEL
	menu_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	action_tab_overlay.add_child(menu_bg)
	
	# Back button
	var back_btn = Button.new()
	back_btn.text = "< Back"
	back_btn.custom_minimum_size = Vector2(62, 22)
	back_btn.position = Vector2(8, 6)
	back_btn.add_theme_color_override("font_color", _detail_text_color())
	back_btn.pressed.connect(_close_action_tab)
	action_tab_overlay.add_child(back_btn)
	
	# Title showing action name
	var title = Label.new()
	title.name = "action_title"
	title.text = action_name.to_upper()
	title.add_theme_font_size_override("font_size", 12)
	title.add_theme_color_override("font_color", _detail_text_color())
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(0, 8)
	title.size = Vector2(action_tab_overlay.size.x, 22)
	action_tab_overlay.add_child(title)

	# Action-focused stat bars
	var focus_stats = VBoxContainer.new()
	focus_stats.name = "focus_stats"
	focus_stats.position = Vector2(8, 30)
	focus_stats.size = Vector2(action_tab_overlay.size.x - 16, 42)
	focus_stats.clip_contents = true
	focus_stats.add_theme_constant_override("separation", 4)
	action_tab_overlay.add_child(focus_stats)
	_populate_action_focus_stats(action_name)

	# Content container
	var content = VBoxContainer.new()
	content.name = "action_content"
	content.position = Vector2(8, 78)
	content.size = Vector2(action_tab_overlay.size.x - 16, max(32.0, action_tab_overlay.size.y - 86))
	content.clip_contents = true
	content.add_theme_constant_override("separation", 6)
	action_tab_overlay.add_child(content)
	
	# Content based on action type
	match action_name:
		"feed":
			_create_feed_tab_content(content)
		"medicine":
			_create_medicine_tab_content(content)
		"rest":
			_create_rest_tab_content(content)
		"groom":
			_create_groom_tab_content(content)
		"exercise":
			_create_exercise_tab_content(content)
		"pet":
			_create_pet_tab_content(content)

	_apply_detail_text_theme(action_tab_overlay)

func _close_action_tab():
	if action_tab_overlay:
		_fade_out_and_free(action_tab_overlay, TAB_SWAP_SEC)
		action_tab_overlay = null
	if stats_panel:
		stats_panel.visible = ui_visible
	action_tab_open = false
	current_action_tab = ""
	last_open_action_tab = ""
	if action_tab_recall_pending_hold:
		_recall_all_pets_home(2.0)
	action_tab_recall_pending_hold = false

func _populate_action_focus_stats(action_name: String):
	if action_tab_overlay == null:
		return
	var pd = game_manager.get_active_pet_data()
	if pd == null:
		return
	var focus_stats = action_tab_overlay.get_node_or_null("focus_stats") as VBoxContainer
	if focus_stats == null:
		return
	for child in focus_stats.get_children():
		child.queue_free()

	match action_name:
		"feed":
			focus_stats.add_child(_create_compact_stat_bar("HUNGER", pd.hunger))
		"pet":
			focus_stats.add_child(_create_compact_stat_bar("JOY", pd.happiness))
			focus_stats.add_child(_create_compact_stat_bar("LOVE", pd.affection))
		"rest":
			focus_stats.add_child(_create_compact_stat_bar("ENERGY", pd.energy))
			focus_stats.add_child(_create_compact_stat_bar("HEALTH", pd.health))
		"groom":
			focus_stats.add_child(_create_compact_stat_bar("GROOM", pd.grooming))
		"bathe":
			focus_stats.add_child(_create_compact_stat_bar("CLEAN", pd.cleanliness))
		"exercise":
			focus_stats.add_child(_create_compact_stat_bar("FIT", pd.fitness))
			focus_stats.add_child(_create_compact_stat_bar("ENERGY", pd.energy))
		"medicine":
			focus_stats.add_child(_create_compact_stat_bar("HEALTH", pd.health))

func _create_compact_stat_bar(label_text: String, value: float) -> Control:
	var bar_width = (action_tab_overlay.size.x - 16) if action_tab_overlay else 284.0
	var row = Control.new()
	row.custom_minimum_size = Vector2(bar_width, 18)

	var bg = ColorRect.new()
	bg.color = COLOR_SURFACE_ALT
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	row.add_child(bg)

	var fill = ColorRect.new()
	fill.color = game_manager.get_stat_color(value)
	fill.position = Vector2(1, 1)
	fill.custom_minimum_size = Vector2((value / 100.0) * (bar_width - 2.0), 16)
	row.add_child(fill)

	var label = Label.new()
	label.text = label_text + " " + str(int(value)) + "%"
	label.add_theme_font_size_override("font_size", 8)
	label.add_theme_color_override("font_color", _contrast_text_color(fill.color))
	label.position = Vector2(4, 1)
	row.add_child(label)

	return row

func _fit_row_buttons(row: HBoxContainer, row_width: float, min_height: float = 24.0):
	if row == null:
		return
	var buttons: Array[Button] = []
	for child in row.get_children():
		if child is Button:
			buttons.append(child)
	if buttons.is_empty():
		return
	var separation = 6.0
	var total_gap = separation * float(max(0, buttons.size() - 1))
	var usable_w = max(80.0, row_width - total_gap)
	var btn_w = floor(usable_w / float(buttons.size()))
	for btn in buttons:
		btn.custom_minimum_size = Vector2(btn_w, min_height)

func _append_tab_hint(content: VBoxContainer, text: String):
	if content == null:
		return
	var hint = Label.new()
	hint.text = text
	hint.add_theme_font_size_override("font_size", 9)
	hint.add_theme_color_override("font_color", _detail_text_color())
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	content.add_child(hint)

func _create_feed_tab_content(content: VBoxContainer):
	var row_w = content.size.x
	var options = HBoxContainer.new()
	options.add_theme_constant_override("separation", 6)
	content.add_child(options)

	for item in [["Small Meal", "feed_small"], ["Full Meal", "feed_full"], ["Treat", "feed_treat"]]:
		var btn = Button.new()
		btn.text = item[0]
		btn.custom_minimum_size = Vector2(84, 24)
		btn.pressed.connect(_do_action_from_tab.bind(item[1]))
		options.add_child(btn)
	_fit_row_buttons(options, row_w, 24)

	var options2 = HBoxContainer.new()
	options2.add_theme_constant_override("separation", 6)
	content.add_child(options2)

	for item in [["Hydrate", "feed_hydrate"], ["Forage", "feed_forage"]]:
		var btn2 = Button.new()
		btn2.text = item[0]
		btn2.custom_minimum_size = Vector2(90, 24)
		btn2.pressed.connect(_do_action_from_tab.bind(item[1]))
		options2.add_child(btn2)
	_fit_row_buttons(options2, row_w, 24)

	var utility = HBoxContainer.new()
	utility.add_theme_constant_override("separation", 8)
	content.add_child(utility)

	var refill_btn = Button.new()
	refill_btn.text = "Refill Bowl"
	refill_btn.custom_minimum_size = Vector2(86, 24)
	refill_btn.pressed.connect(_on_refill_water)
	utility.add_child(refill_btn)

	var pd = game_manager.get_active_pet_data()
	var water_text = Label.new()
	water_text.add_theme_font_size_override("font_size", 9)
	water_text.add_theme_color_override("font_color", _detail_text_color())
	water_text.text = "Water " + str(int(pd.water_bowl_level)) + "%" if pd else "Water --"
	utility.add_child(water_text)
	_append_tab_hint(content, "Forage is risky: no hunger gain until food is found.")

func _on_refill_water():
	var pd = game_manager.get_active_pet_data()
	if pd:
		game_manager.refill_water_bowl(game_manager.active_pet_index)
		_on_stats_updated()

func _create_medicine_tab_content(content: VBoxContainer):
	var hint = Label.new()
	hint.text = "Select medicine to hold, then click pet"
	hint.add_theme_font_size_override("font_size", 9)
	hint.add_theme_color_override("font_color", _detail_text_color())
	content.add_child(hint)

	var open_btn = Button.new()
	open_btn.text = "Open Medicine List"
	open_btn.custom_minimum_size = Vector2(200, 30)
	open_btn.add_theme_color_override("font_color", _detail_text_color())
	open_btn.pressed.connect(_show_medicine_collect)
	content.add_child(open_btn)
	_append_tab_hint(content, "Hold a treatment, then click your pet to apply it.")

func _create_rest_tab_content(content: VBoxContainer):
	var pd = game_manager.get_active_pet_data()
	var toggle = Button.new()
	toggle.text = "Wake Up" if pd and pd.is_sleeping else "Sleep"
	toggle.custom_minimum_size = Vector2(180, 30)
	toggle.pressed.connect(_do_action_from_tab.bind("rest_toggle"))
	content.add_child(toggle)
	_append_tab_hint(content, "Sleeping restores energy and health over time.")

func _create_groom_tab_content(content: VBoxContainer):
	var row_w = content.size.x
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation", 6)
	content.add_child(row)
	for item in [["Hair Cut", "groom_haircut"], ["Dental Check", "groom_dental"], ["Bathing", "groom_bathing"]]:
		var btn = Button.new()
		btn.text = item[0]
		btn.custom_minimum_size = Vector2(86, 24)
		btn.pressed.connect(_do_action_from_tab.bind(item[1]))
		row.add_child(btn)
	_fit_row_buttons(row, row_w, 24)
	_append_tab_hint(content, "Grooming improves coat quality and overall comfort.")

func _create_exercise_tab_content(content: VBoxContainer):
	var row_w = content.size.x
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation", 8)
	content.add_child(row)
	for item in [["Play", "exercise_play"], ["Workout", "exercise_workout"]]:
		var btn = Button.new()
		btn.text = item[0]
		btn.custom_minimum_size = Vector2(110, 28)
		btn.pressed.connect(_do_action_from_tab.bind(item[1]))
		row.add_child(btn)
	_fit_row_buttons(row, row_w, 28)
	_append_tab_hint(content, "Workout builds fitness faster but can cause injury if overused.")

func _create_pet_tab_content(content: VBoxContainer):
	var row_w = content.size.x
	var options = [["Head Pat", "pet_pat"], ["Cuddle", "pet_cuddle"], ["Play", "pet_play"], ["Talk", "pet_talk"]]
	for opt in options:
		var btn = Button.new()
		btn.text = opt[0]
		btn.custom_minimum_size = Vector2(row_w, 24)
		btn.add_theme_color_override("font_color", _detail_text_color())
		btn.pressed.connect(_do_action_from_tab.bind(opt[1]))
		content.add_child(btn)
	_append_tab_hint(content, "Play chances improve as affection rises.")

func _do_action_from_tab(action_name: String):
	var result = game_manager.perform_action(action_name)
	_play_action_sound(_base_action_name(action_name))
	if result is Dictionary and result.get("message", "") != "":
		_show_feedback_message(result.get("message", ""))
	_on_stats_updated()
	if action_name == "rest_toggle":
		_show_action_tab("rest")

func _base_action_name(action_name: String) -> String:
	if action_name.begins_with("feed_"):
		return "feed"
	if action_name.begins_with("pet_"):
		return "pet"
	if action_name.begins_with("groom_"):
		return "groom"
	if action_name.begins_with("exercise_"):
		return "exercise"
	if action_name.begins_with("rest_"):
		return "rest"
	return action_name

func show_action_menu(action_type: String):
	# Legacy entry point; route to the unified action-tab UX.
	_show_action_tab(action_type)

func add_food_options(container: VBoxContainer):
	var pd = game_manager.get_active_pet_data()
	var animal = pd.animal_type if pd else "rat"
	
	var foods = {
		"rat": ["Cheese", "Seeds", "Vegetables", "Fruit"],
		"crow": ["Nuts", "Seeds", "Berries", "Insects"],
		"fox": ["Meat", "Fish", "Berries", "Eggs"],
		"snake": ["Mouse", "Fish", "Eggs", "Insects"],
		"deer": ["Grass", "Leaves", "Berries", "Mushrooms"],
		"frog": ["Flies", "Worms", "Beetles", "Small Fish"],
		"pigeon": ["Seeds", "Grain", "Bread", "Fruit"],
		"raccoon": ["Fish", "Fruit", " Nuts", "Eggs"],
		"squirrel": ["Nuts", "Seeds", "Fruit", "Vegetables"],
		"goose": ["Grass", "Grain", "Seeds", "Vegetables"]
	}
	
	var food_list = foods.get(animal, foods["rat"])
	for food in food_list:
		var btn = Button.new()
		btn.text = food
		btn.custom_minimum_size = Vector2(240, 25)
		btn.pressed.connect(_on_food_selected.bind(food))
		container.add_child(btn)

func add_pet_options(container: VBoxContainer):
	var options = ["Head Pat", "Belly Rub", "Scratch Behind Ears", "Hold", "Play"]
	for opt in options:
		var btn = Button.new()
		btn.text = opt
		btn.custom_minimum_size = Vector2(240, 25)
		btn.pressed.connect(_close_action_menu)
		container.add_child(btn)

func add_rest_options(container: VBoxContainer):
	var options = ["Short Nap (1h)", "Long Sleep (3h)", "Deep Sleep (6h)"]
	for opt in options:
		var btn = Button.new()
		btn.text = opt
		btn.custom_minimum_size = Vector2(240, 25)
		btn.pressed.connect(_close_action_menu)
		container.add_child(btn)

func add_groom_options(container: VBoxContainer):
	var options = ["Brush Fur", "Clean Ears", "Trim Nails", "Full Groom"]
	for opt in options:
		var btn = Button.new()
		btn.text = opt
		btn.custom_minimum_size = Vector2(240, 25)
		btn.pressed.connect(_close_action_menu)
		container.add_child(btn)

func add_bathe_options(container: VBoxContainer):
	var options = ["Quick Rinse", "Full Bath", "Sponge Bath", "Shower"]
	for opt in options:
		var btn = Button.new()
		btn.text = opt
		btn.custom_minimum_size = Vector2(240, 25)
		btn.pressed.connect(_close_action_menu)
		container.add_child(btn)

func add_exercise_options(container: VBoxContainer):
	var options = ["Walk Around", "Play Fetch", "Swimming", "Training"]
	for opt in options:
		var btn = Button.new()
		btn.text = opt
		btn.custom_minimum_size = Vector2(240, 25)
		btn.pressed.connect(_close_action_menu)
		container.add_child(btn)

func _on_food_selected(_food: String):
	_close_action_menu()
	# Food gives different benefits
	game_manager.perform_action("feed")

func _close_action_menu():
	if action_menu_overlay:
		action_menu_overlay.queue_free()
		action_menu_overlay = null

# Medical Guidebook Data
const CONDITIONS_DATA = {
	# Innate
	"respiratoryProblems": {"name": "Respiratory Problems", "category": "Innate", "symptoms": "Heavy breathing, Sneezing, Less active", "treatments": "Antibiotics, Rest in clean air", "prevention": "Keep environment clean"},
	"sheddingIssues": {"name": "Shedding Issues", "category": "Innate", "symptoms": "Incomplete shedding, Itching", "treatments": "Wound Cleaning, Humidity", "prevention": "Maintain humidity"},
	"dentalProblems": {"name": "Dental Problems", "category": "Innate", "symptoms": "Difficulty eating, Drooling", "treatments": "Dental Care, Soft foods", "prevention": "Chew items"},
	"parasites": {"name": "Parasites", "category": "Innate", "symptoms": "Weight loss, Lethargy", "treatments": "Antibiotics, Cleaning", "prevention": "Clean area"},
	"jointStiffness": {"name": "Joint Stiffness", "category": "Innate", "symptoms": "Stiff movements, Reluctance", "treatments": "Joint Support, Exercise", "prevention": "Fitness"},
	"skinInfections": {"name": "Skin Infections", "category": "Innate", "symptoms": "Redness, Scratching", "treatments": "Antibiotics, Immune Boost", "prevention": "Allergens"},
	"viralSusceptibility": {"name": "Viral Susceptibility", "category": "Innate", "symptoms": "Gets sick easily, Slow recovery", "treatments": "Immune Booster, Vitamins", "prevention": "Low stress"},
	"dentalOvergrowth": {"name": "Dental Overgrowth", "category": "Innate", "symptoms": "Difficulty eating, Overgrown teeth", "treatments": "Dental Care, Chew toys", "prevention": "Chew items"},
	"footProblems": {"name": "Foot Problems", "category": "Innate", "symptoms": "Limping, Foot sensitivity", "treatments": "Joint Support, Soft bedding", "prevention": "Soft surfaces"},
	# Acquired
	"obesity": {"name": "Obesity", "category": "Acquired", "symptoms": "Rounder, Slower, Gets tired", "treatments": "Appetite Stimulant, Exercise", "prevention": "Monitor hunger"},
	"malnutrition": {"name": "Malnutrition", "category": "Acquired", "symptoms": "Thin, Ribs visible, Weakness", "treatments": "Appetite Stimulant, Energy Tonic", "prevention": "Consistent feeding"},
	"depression": {"name": "Depression", "category": "Acquired", "symptoms": "Droopy, Less animation, Uninterested", "treatments": "Mood Stabilizer, Play therapy", "prevention": "Keep happy"},
	"anxiety": {"name": "Anxiety", "category": "Acquired", "symptoms": "Trembling, Jumpy, Nervous", "treatments": "Mood Stabilizer, Comfort", "prevention": "Regular attention"},
	"skinInfection": {"name": "Skin Infection", "category": "Acquired", "symptoms": "Red skin, Scratching", "treatments": "Antibiotics, Bathing", "prevention": "Cleanliness"},
	"jointPain": {"name": "Joint Pain", "category": "Acquired", "symptoms": "Limping, Stiff movements", "treatments": "Joint Support, Gentle exercise", "prevention": "Fitness"},
	"exhaustion": {"name": "Exhaustion", "category": "Acquired", "symptoms": "Heavy breathing, Slumped, Tired", "treatments": "Energy Tonic, Rest", "prevention": "Balance activity"},
	"poorCoat": {"name": "Poor Coat", "category": "Acquired", "symptoms": "Ruffled, Dull appearance", "treatments": "Wound Cleaning, Vitamins", "prevention": "Regular grooming"},
	# Chronic
	"medicationToxicity": {"name": "Medication Toxicity", "category": "Chronic", "symptoms": "Pale, Lethargic, Weak", "treatments": "Detox Herbs, Rest", "prevention": "Don't over-treat"},
	"medicinePoisoning": {"name": "Medicine Poisoning", "category": "Chronic", "symptoms": "Rapid breathing, Unhealthy color", "treatments": "Detox Herbs, Supportive care", "prevention": "Correct diagnosis"},
	"treatmentScarring": {"name": "Treatment Scarring", "category": "Chronic", "symptoms": "Reduced healing", "treatments": "Vitamins, Wellness care", "prevention": "Prevent illness"}
}

const MEDICINES_DATA = {
	"woundClean": {"name": "Wound Cleaning", "treats": "Skin Infection, Poor Coat"},
	"antibiotics": {"name": "Antibiotics", "treats": "Skin Infection, Respiratory, Parasites"},
	"jointSupport": {"name": "Joint Support", "treats": "Joint Pain, Joint Stiffness, Foot Problems"},
	"immuneBoost": {"name": "Immune Booster", "treats": "Viral Susceptibility, Skin Infections"},
	"dentalCare": {"name": "Dental Care", "treats": "Dental Problems, Dental Overgrowth"},
	"moodStabilizer": {"name": "Mood Stabilizer", "treats": "Depression, Anxiety"},
	"energyTonic": {"name": "Energy Tonic", "treats": "Exhaustion, Malnutrition"},
	"appetiteStimulant": {"name": "Appetite Stimulant", "treats": "Malnutrition, Obesity"},
	"detoxHerbs": {"name": "Detox Herbs", "treats": "Medication Toxicity"},
	"vitaminSupplements": {"name": "Vitamin Supplements", "treats": "Poor Coat, Viral Susceptibility"}
}

var guidebook_page: int = 0

func show_medicine_menu():
	close_all_overlays()
	
	guidebook_page = 0
	action_menu_overlay = Control.new()
	action_menu_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(action_menu_overlay)
	
	# Background - solid (not transparent)
	var menu_bg = ColorRect.new()
	menu_bg.color = Color(COLOR_OVERLAY_PANEL.r, COLOR_OVERLAY_PANEL.g, COLOR_OVERLAY_PANEL.b, 1.0)
	menu_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	action_menu_overlay.add_child(menu_bg)
	
	# Title
	var title = Label.new()
	title.text = "Medical Guidebook"
	title.add_theme_font_size_override("font_size", 14)
	title.add_theme_color_override("font_color", COLOR_TEXT)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(0, 30)
	title.size = Vector2(320, 25)
	action_menu_overlay.add_child(title)
	
	# Navigation buttons
	var nav_container = HBoxContainer.new()
	nav_container.position = Vector2(10, 55)
	nav_container.add_theme_constant_override("separation", 10)
	action_menu_overlay.add_child(nav_container)
	
	var conditions_btn = Button.new()
	conditions_btn.text = "Conditions"
	conditions_btn.custom_minimum_size = Vector2(80, 20)
	conditions_btn.pressed.connect(_show_guidebook_conditions)
	nav_container.add_child(conditions_btn)
	
	var medicines_btn = Button.new()
	medicines_btn.text = "Medicines"
	medicines_btn.custom_minimum_size = Vector2(80, 20)
	medicines_btn.pressed.connect(_show_guidebook_medicines)
	nav_container.add_child(medicines_btn)
	
	var collect_btn = Button.new()
	collect_btn.text = "Collect"
	collect_btn.custom_minimum_size = Vector2(80, 20)
	collect_btn.pressed.connect(_show_medicine_collect)
	nav_container.add_child(collect_btn)
	
	# Content container
	var scroll = ScrollContainer.new()
	scroll.position = Vector2(10, 85)
	scroll.size = Vector2(300, 280)
	scroll.name = "guidebook_scroll"
	action_menu_overlay.add_child(scroll)
	
	var content = VBoxContainer.new()
	content.add_theme_constant_override("separation", 5)
	content.name = "guidebook_content"
	scroll.add_child(content)
	
	# Show initial page
	_show_guidebook_conditions()
	
	# Close button
	var close_btn = Button.new()
	close_btn.text = "Close"
	close_btn.custom_minimum_size = Vector2(80, 22)
	close_btn.position = Vector2(120, 375)
	close_btn.pressed.connect(_close_action_menu)
	action_menu_overlay.add_child(close_btn)

func _show_guidebook_conditions():
	guidebook_page = 0
	_update_guidebook_content()

func _show_guidebook_medicines():
	guidebook_page = 1
	_update_guidebook_content()

func _update_guidebook_content():
	var scroll = action_menu_overlay.find_child("guidebook_scroll", true, false)
	if not scroll:
		return
	
	var content = scroll.find_child("guidebook_content", true, false)
	if not content:
		return
	
	# Clear existing
	for child in content.get_children():
		child.queue_free()
	
	if guidebook_page == 0:
		# Show conditions
		for cond_id in CONDITIONS_DATA.keys():
			var cond = CONDITIONS_DATA[cond_id]
			
			var cond_panel = VBoxContainer.new()
			cond_panel.custom_minimum_size = Vector2(280, 60)
			content.add_child(cond_panel)
			
			var name_label = Label.new()
			name_label.text = cond["name"] + " [" + cond["category"] + "]"
			name_label.add_theme_font_size_override("font_size", 9)
			name_label.add_theme_color_override("font_color", COLOR_TEXT)
			cond_panel.add_child(name_label)
			
			var symp_label = Label.new()
			symp_label.text = "Symptoms: " + cond["symptoms"]
			symp_label.add_theme_font_size_override("font_size", 7)
			symp_label.add_theme_color_override("font_color", COLOR_TEXT_DIM)
			symp_label.autowrap_mode = TextServer.AUTOWRAP_WORD
			symp_label.custom_minimum_size = Vector2(275, 20)
			cond_panel.add_child(symp_label)
			
			var treat_label = Label.new()
			treat_label.text = "Treat: " + cond["treatments"]
			treat_label.add_theme_font_size_override("font_size", 7)
			treat_label.add_theme_color_override("font_color", Color(0.4, 0.8, 0.4))
			treat_label.autowrap_mode = TextServer.AUTOWRAP_WORD
			treat_label.custom_minimum_size = Vector2(275, 20)
			cond_panel.add_child(treat_label)
	
	elif guidebook_page == 1:
		# Show medicines info
		for med_id in MEDICINES_DATA.keys():
			var med = MEDICINES_DATA[med_id]
			
			var med_panel = VBoxContainer.new()
			med_panel.custom_minimum_size = Vector2(280, 35)
			content.add_child(med_panel)
			
			var name_label = Label.new()
			name_label.text = med["name"]
			name_label.add_theme_font_size_override("font_size", 9)
			name_label.add_theme_color_override("font_color", COLOR_TEXT)
			med_panel.add_child(name_label)
			
			var treat_label = Label.new()
			treat_label.text = "Treats: " + med["treats"]
			treat_label.add_theme_font_size_override("font_size", 7)
			treat_label.add_theme_color_override("font_color", Color(0.4, 0.8, 0.4))
			treat_label.autowrap_mode = TextServer.AUTOWRAP_WORD
			treat_label.custom_minimum_size = Vector2(275, 20)
			med_panel.add_child(treat_label)
	
	elif guidebook_page == 2:
		# Show medicine collection
		var collect_label = Label.new()
		collect_label.text = "Click medicine to collect (max 1 each)"
		collect_label.add_theme_font_size_override("font_size", 8)
		collect_label.add_theme_color_override("font_color", COLOR_TEXT_DIM)
		content.add_child(collect_label)
		
		for med_id in MEDICINE_INFO.keys():
			var med_info = MEDICINE_INFO[med_id]
			var count = medicine_inventory.get(med_id, 0)
			
			var med_row = HBoxContainer.new()
			med_row.custom_minimum_size = Vector2(280, 30)
			med_row.add_theme_constant_override("separation", 10)
			content.add_child(med_row)
			
			# Collect button
			var collect_btn = Button.new()
			collect_btn.text = "Get" if count < 1 else "Have"
			collect_btn.custom_minimum_size = Vector2(40, 25)
			collect_btn.disabled = count >= 1
			collect_btn.pressed.connect(_collect_medicine.bind(med_id))
			med_row.add_child(collect_btn)
			
			# Use button (if has medicine)
			var use_btn = Button.new()
			use_btn.text = "Use" if count > 0 else "--"
			use_btn.custom_minimum_size = Vector2(40, 25)
			use_btn.disabled = count < 1
			use_btn.pressed.connect(_on_hold_medicine.bind(med_id))
			med_row.add_child(use_btn)
			
			# Name label
			var name_lbl = Label.new()
			name_lbl.text = med_info["name"] + " (" + str(count) + ")"
			name_lbl.add_theme_font_size_override("font_size", 9)
			name_lbl.add_theme_color_override("font_color", COLOR_TEXT)
			name_lbl.custom_minimum_size = Vector2(150, 20)
			med_row.add_child(name_lbl)

func _show_medicine_collect():
	guidebook_page = 2
	_update_guidebook_content()

func _collect_medicine(med_id: String):
	if medicine_inventory.get(med_id, 0) < 1:
		medicine_inventory[med_id] = 1
		_update_guidebook_content()
		print("Collected: " + med_id)
	else:
		print("Already have: " + med_id)

func _on_hold_medicine(med_id: String):
	# Pick up medicine from inventory to hold
	if medicine_inventory.get(med_id, 0) > 0:
		held_item = {"type": "medicine", "name": med_id, "icon": "medicine"}
		_close_action_menu()
		_create_held_item_sprite()
		print("Holding medicine: " + med_id)
	else:
		print("No medicine available: " + med_id)

func _on_prev_pet():
	game_manager.previous_pet()

func _on_next_pet():
	game_manager.next_pet()

func _on_add_pet():
	add_new_pet()

func update_environment_background():
	var count = max(1, game_manager.get_pet_count()) if game_manager else 1
	_ensure_environment_stage_nodes(count)
	for i in range(environment_stages.size()):
		var stage = environment_stages[i]
		var slot_pet_data = game_manager.pet_datas[i] if game_manager and i < game_manager.pet_datas.size() else null
		var env = game_manager.get_pet_environment(slot_pet_data.animal_type) if slot_pet_data else {
			"bg": COLOR_BG,
			"mid": Color(0.18, 0.2, 0.21),
			"ground": Color(0.06, 0.07, 0.08)
		}

		var stage_bg = stage.get("bg") as ColorRect
		var stage_mid = stage.get("mid") as ColorRect
		var stage_ground = stage.get("ground") as ColorRect
		if stage_bg:
			stage_bg.color = env.get("bg", COLOR_BG)
		if stage_mid:
			stage_mid.color = env.get("mid", Color(0.18, 0.2, 0.21))
		if stage_ground:
			stage_ground.color = env.get("ground", Color(0.06, 0.07, 0.08))

	_apply_environment_layout(get_window().size.y)

func _on_active_pet_changed(_index):
	update_environment_background()
	_on_stats_updated()

func _on_pet_added(_index):
	update_environment_background()
	_on_stats_updated()

func _on_pet_state_changed(_state):
	pass

func _on_pet_died(pet_data: PetData):
	# Remove pet from game
	var pet_index = game_manager.pet_datas.find(pet_data)
	if pet_index >= 0:
		if game_manager.pets.size() > pet_index:
			var pet = game_manager.pets[pet_index]
			if pet:
				pet.queue_free()
		game_manager.pets.remove_at(pet_index)
		game_manager.pet_datas.remove_at(pet_index)
	
	# Adjust active pet index if needed
	if game_manager.active_pet_index >= game_manager.pets.size():
		game_manager.active_pet_index = max(0, game_manager.pets.size() - 1)
	
	# Show death popup
	_show_death_popup(pet_data)
	
	update_environment_background()
	_on_stats_updated()

func _show_death_popup(pet_data: PetData):
	if pet_data == null:
		return
	
	close_all_overlays()
	
	var modal = _create_priority_modal(Vector2(268, 210), 0.6)
	death_overlay = modal["overlay"]
	var card = modal["card"] as Panel
	
	# Death message
	var title = Label.new()
	title.text = "I'm sorry, '" + pet_data.name + "' has passed away..."
	title.add_theme_font_size_override("font_size", 13)
	title.add_theme_color_override("font_color", _detail_text_color())
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(12, 20)
	title.size = Vector2(244, 56)
	title.autowrap_mode = TextServer.AUTOWRAP_WORD
	card.add_child(title)
	
	# Age info
	var age_text = "Age: " + str(pet_data.age_at_death) + " minutes"
	var age_label = Label.new()
	age_label.text = age_text
	age_label.add_theme_font_size_override("font_size", 10)
	age_label.add_theme_color_override("font_color", _detail_text_color())
	age_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	age_label.position = Vector2(12, 92)
	age_label.size = Vector2(244, 22)
	card.add_child(age_label)
	
	# OK button
	var ok_btn = Button.new()
	ok_btn.text = "OK"
	ok_btn.custom_minimum_size = Vector2(80, 30)
	ok_btn.add_theme_color_override("font_color", _detail_text_color())
	ok_btn.position = Vector2(94, 150)
	ok_btn.pressed.connect(_close_death_popup)
	card.add_child(ok_btn)

func _close_death_popup():
	if death_overlay:
		_fade_out_and_free(death_overlay, UI_FADE_OUT_SEC)
		death_overlay = null
	_on_stats_updated()

func _on_naming_needed(pet_index: int):
	_show_naming_popup(pet_index)

func _show_naming_popup(pet_index: int):
	close_all_overlays()
	
	var modal = _create_priority_modal(Vector2(272, 228), 0.6)
	naming_overlay = modal["overlay"]
	var card = modal["card"] as Panel
	
	# Title
	var title = Label.new()
	title.text = "Your pet has hatched!"
	title.add_theme_font_size_override("font_size", 16)
	title.add_theme_color_override("font_color", _detail_text_color())
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(0, 18)
	title.size = Vector2(272, 30)
	card.add_child(title)
	
	# Subtitle
	var subtitle = Label.new()
	subtitle.text = "What will you name them?"
	subtitle.add_theme_font_size_override("font_size", 12)
	subtitle.add_theme_color_override("font_color", _detail_text_color())
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtitle.position = Vector2(0, 50)
	subtitle.size = Vector2(272, 25)
	card.add_child(subtitle)
	
	# Name input
	var name_input = LineEdit.new()
	name_input.position = Vector2(36, 94)
	name_input.custom_minimum_size = Vector2(200, 30)
	name_input.placeholder_text = "Enter name..."
	name_input.max_length = 20
	card.add_child(name_input)
	
	# Confirm button
	var confirm_btn = Button.new()
	confirm_btn.text = "Confirm"
	confirm_btn.custom_minimum_size = Vector2(100, 30)
	confirm_btn.add_theme_color_override("font_color", _detail_text_color())
	confirm_btn.position = Vector2(86, 150)
	confirm_btn.pressed.connect(_on_name_confirmed.bind(pet_index, name_input))
	card.add_child(confirm_btn)

func _on_name_confirmed(pet_index: int, name_input: LineEdit):
	var pet_name = name_input.text.strip_edges()
	if pet_name == "":
		pet_name = "Wevito"
	
	if pet_index >= 0 and pet_index < game_manager.pet_datas.size():
		var pd = game_manager.pet_datas[pet_index]
		pd.name = pet_name
		pd.is_naming_pending = false
	
	if naming_overlay:
		_fade_out_and_free(naming_overlay, UI_FADE_OUT_SEC)
		naming_overlay = null
	
	_on_stats_updated()

func _add_slider_row(parent: VBoxContainer, label_text: String, start_value: float, setting_key: String, channel: String):
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation", 8)
	parent.add_child(row)

	var label = Label.new()
	label.text = label_text
	label.custom_minimum_size = Vector2(36, 20)
	label.add_theme_font_size_override("font_size", 10)
	label.add_theme_color_override("font_color", _detail_text_color())
	row.add_child(label)

	var slider = HSlider.new()
	slider.min_value = 0
	slider.max_value = 255
	slider.step = 1
	slider.value = start_value
	slider.custom_minimum_size = Vector2(170, 18)
	slider.value_changed.connect(_on_color_slider_changed.bind(setting_key, channel))
	row.add_child(slider)

func _add_color_picker(parent: VBoxContainer, title_text: String, setting_key: String):
	var block = VBoxContainer.new()
	block.add_theme_constant_override("separation", 4)
	parent.add_child(block)

	var title = Label.new()
	title.text = title_text
	title.add_theme_font_size_override("font_size", 10)
	title.add_theme_color_override("font_color", _detail_text_color())
	block.add_child(title)

	var c = _color_from_setting(setting_key, Color.WHITE)
	_add_slider_row(block, "R", c.r * 255.0, setting_key, "r")
	_add_slider_row(block, "G", c.g * 255.0, setting_key, "g")
	_add_slider_row(block, "B", c.b * 255.0, setting_key, "b")

func _on_color_slider_changed(value: float, setting_key: String, channel: String):
	_on_color_slider(setting_key, channel, value)

func _on_color_slider(setting_key: String, channel: String, value: float):
	var c = _color_from_setting(setting_key, Color.WHITE)
	if channel == "r":
		c.r = value / 255.0
	elif channel == "g":
		c.g = value / 255.0
	elif channel == "b":
		c.b = value / 255.0
	settings[setting_key] = _color_to_setting(c)
	_apply_main_menu_text_theme()
	_on_stats_updated()
	if settings_overlay:
		_apply_detail_text_theme(settings_overlay)
	if action_tab_overlay:
		_apply_detail_text_theme(action_tab_overlay)

func _add_palette_selector(parent: VBoxContainer):
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation", 6)
	parent.add_child(row)

	var label = Label.new()
	label.text = "Status Box Palette:"
	label.custom_minimum_size = Vector2(120, 20)
	label.add_theme_font_size_override("font_size", 10)
	label.add_theme_color_override("font_color", _detail_text_color())
	row.add_child(label)

	for key in ["classic", "cool", "warm", "mono"]:
		var btn = Button.new()
		btn.text = key.capitalize()
		btn.custom_minimum_size = Vector2(58, 20)
		btn.add_theme_color_override("font_color", _detail_text_color())
		btn.pressed.connect(_set_status_palette.bind(key))
		row.add_child(btn)

func _set_status_palette(key: String):
	settings["status_box_palette"] = key
	_on_stats_updated()

func _show_settings_menu():
	close_all_overlays()
	
	var modal = _create_priority_modal(Vector2(280, 352), 0.58)
	settings_overlay = modal["overlay"]
	var card = modal["card"] as Panel
	card.name = "settings_card"
	
	var title = Label.new()
	title.text = "Settings"
	title.add_theme_font_size_override("font_size", 14)
	title.add_theme_color_override("font_color", _detail_text_color())
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(0, 8)
	title.size = Vector2(280, 25)
	card.add_child(title)
	
	var scroll = ScrollContainer.new()
	scroll.position = Vector2(10, 38)
	scroll.size = Vector2(260, 270)
	card.add_child(scroll)

	var options_container = VBoxContainer.new()
	options_container.custom_minimum_size = Vector2(246, 520)
	options_container.add_theme_constant_override("separation", 12)
	scroll.add_child(options_container)
	
	# Bell toggle
	var bell_row = HBoxContainer.new()
	bell_row.add_theme_constant_override("separation", 10)
	options_container.add_child(bell_row)
	
	var bell_label = Label.new()
	bell_label.text = "Bell Sounds:"
	bell_label.add_theme_font_size_override("font_size", 10)
	bell_label.add_theme_color_override("font_color", _detail_text_color())
	bell_label.custom_minimum_size = Vector2(120, 20)
	bell_row.add_child(bell_label)
	
	var bell_btn = Button.new()
	bell_btn.text = "ON" if settings["bell"] else "OFF"
	bell_btn.custom_minimum_size = Vector2(60, 20)
	bell_btn.pressed.connect(_toggle_bell.bind(bell_btn))
	bell_row.add_child(bell_btn)
	
	# Auto-save toggle
	var auto_save_row = HBoxContainer.new()
	auto_save_row.add_theme_constant_override("separation", 10)
	options_container.add_child(auto_save_row)
	
	var auto_save_label = Label.new()
	auto_save_label.text = "Auto-Save:"
	auto_save_label.add_theme_font_size_override("font_size", 10)
	auto_save_label.add_theme_color_override("font_color", _detail_text_color())
	auto_save_label.custom_minimum_size = Vector2(120, 20)
	auto_save_row.add_child(auto_save_label)
	
	var auto_save_btn = Button.new()
	auto_save_btn.text = "ON" if settings["auto_save"] else "OFF"
	auto_save_btn.custom_minimum_size = Vector2(60, 20)
	auto_save_btn.pressed.connect(_toggle_auto_save.bind(auto_save_btn))
	auto_save_row.add_child(auto_save_btn)
	
	# Click-through toggle
	var click_through_row = HBoxContainer.new()
	click_through_row.add_theme_constant_override("separation", 10)
	options_container.add_child(click_through_row)
	
	var click_through_label = Label.new()
	click_through_label.text = "Click-Through:"
	click_through_label.add_theme_font_size_override("font_size", 10)
	click_through_label.add_theme_color_override("font_color", _detail_text_color())
	click_through_label.custom_minimum_size = Vector2(120, 20)
	click_through_row.add_child(click_through_label)
	
	var click_through_btn = Button.new()
	click_through_btn.text = "ON" if settings["click_through"] else "OFF"
	click_through_btn.custom_minimum_size = Vector2(60, 20)
	click_through_btn.pressed.connect(_toggle_click_through.bind(click_through_btn))
	click_through_row.add_child(click_through_btn)
	
	# Sound effects toggle
	var sound_row = HBoxContainer.new()
	sound_row.add_theme_constant_override("separation", 10)
	options_container.add_child(sound_row)
	
	var sound_label = Label.new()
	sound_label.text = "Sound FX:"
	sound_label.add_theme_font_size_override("font_size", 10)
	sound_label.add_theme_color_override("font_color", _detail_text_color())
	sound_label.custom_minimum_size = Vector2(120, 20)
	sound_row.add_child(sound_label)
	
	var sound_btn = Button.new()
	sound_btn.text = "ON" if settings["sound_effects"] else "OFF"
	sound_btn.custom_minimum_size = Vector2(60, 20)
	sound_btn.pressed.connect(_toggle_sound.bind(sound_btn))
	sound_row.add_child(sound_btn)
	
	# Game speed info
	var speed_row = HBoxContainer.new()
	speed_row.add_theme_constant_override("separation", 10)
	options_container.add_child(speed_row)
	
	var speed_label = Label.new()
	speed_label.text = "Game Speed:"
	speed_label.add_theme_font_size_override("font_size", 10)
	speed_label.add_theme_color_override("font_color", _detail_text_color())
	speed_label.custom_minimum_size = Vector2(120, 20)
	speed_row.add_child(speed_label)
	
	var speed_value_label = Label.new()
	speed_value_label.text = "1 tick/sec"
	speed_value_label.add_theme_font_size_override("font_size", 10)
	speed_value_label.add_theme_color_override("font_color", _detail_text_color())
	speed_row.add_child(speed_value_label)

	_add_color_picker(options_container, "Main Menu Text Color", "main_menu_text_color")
	_add_color_picker(options_container, "Detail Text Color", "detail_text_color")
	_add_palette_selector(options_container)
	
	# Reset save
	var reset_btn = Button.new()
	reset_btn.text = "Reset Save"
	reset_btn.custom_minimum_size = Vector2(180, 25)
	reset_btn.pressed.connect(_reset_save)
	options_container.add_child(reset_btn)
	
	# Close button
	var close_btn = Button.new()
	close_btn.text = "Close"
	close_btn.custom_minimum_size = Vector2(100, 24)
	close_btn.position = Vector2(90, 318)
	close_btn.add_theme_color_override("font_color", _detail_text_color())
	close_btn.pressed.connect(_close_settings_menu)
	card.add_child(close_btn)

	_apply_detail_text_theme(settings_overlay)

func _close_settings_menu():
	if settings_overlay:
		_fade_out_and_free(settings_overlay, UI_FADE_OUT_SEC)
		settings_overlay = null

func _toggle_bell(btn: Button):
	settings["bell"] = not settings["bell"]
	btn.text = "ON" if settings["bell"] else "OFF"

func _toggle_auto_save(btn: Button):
	settings["auto_save"] = not settings["auto_save"]
	btn.text = "ON" if settings["auto_save"] else "OFF"

func _toggle_click_through(btn: Button):
	settings["click_through"] = not settings["click_through"]
	btn.text = "ON" if settings["click_through"] else "OFF"
	_apply_click_through_mode()

func _apply_click_through_mode():
	_set_ui_clickable(ui_visible)
	_apply_mouse_passthrough_for_mode(ui_visible)
	_pin_window_bottom_right()

func _minimize_window():
	get_window().visible = false

func _input(event):
	if event.is_action_pressed("show-window"):
		_show_from_tray()
	
	# Handle click to feed/medicate pet
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if held_item.type != "":
			_handle_held_item_click(event.position)

func _show_from_tray():
	get_window().visible = true
	get_window().mode = Window.MODE_WINDOWED
	position_window()
	
	if settings.get("click_through", false):
		settings["click_through"] = false
		_apply_click_through_mode()
		print("Click-through disabled: window manually shown")

func _toggle_ghost_mode():
	settings["ghost_mode"] = not settings["ghost_mode"]
	_apply_ghost_mode()
	if settings["ghost_mode"] and sound_manager:
		sound_manager.play_ghost_sound()

func _toggle_doctors_note():
	close_all_overlays()
	doctors_note_visible = not doctors_note_visible
	if doctors_note_visible:
		_show_doctors_note()
	else:
		if doctors_note_overlay:
			doctors_note_overlay.queue_free()
			doctors_note_overlay = null

func _show_doctors_note():
	doctors_note_overlay = Control.new()
	doctors_note_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(doctors_note_overlay)
	
	# Solid background
	var menu_bg = ColorRect.new()
	menu_bg.color = Color(COLOR_OVERLAY_PANEL.r, COLOR_OVERLAY_PANEL.g, COLOR_OVERLAY_PANEL.b, 1.0)
	menu_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	doctors_note_overlay.add_child(menu_bg)
	
	# Title
	var title = Label.new()
	title.text = "Doctor's Note"
	title.add_theme_font_size_override("font_size", 14)
	title.add_theme_color_override("font_color", COLOR_TEXT)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(0, 20)
	title.size = Vector2(320, 25)
	doctors_note_overlay.add_child(title)
	
	# Content
	var content = VBoxContainer.new()
	content.position = Vector2(20, 55)
	content.size = Vector2(280, 300)
	content.add_theme_constant_override("separation", 8)
	doctors_note_overlay.add_child(content)
	
	# Check if any pets are sick
	var has_sick_pet = false
	var pet_datas = game_manager.pet_datas
	for pd in pet_datas:
		if pd and pd.conditions.size() > 0 and not pd.is_dead:
			has_sick_pet = true
			# Show pet info
			var pet_label = Label.new()
			pet_label.text = pd.name + " (" + pd.animal_type + ")"
			pet_label.add_theme_font_size_override("font_size", 11)
			pet_label.add_theme_color_override("font_color", COLOR_TEXT)
			content.add_child(pet_label)
			
			for cond_id in pd.conditions.keys():
				var severity = pd.conditions[cond_id]
				var severity_text = "Mild"
				if severity == 2:
					severity_text = "Moderate"
				elif severity >= 3:
					severity_text = "Severe"
				var cond_label = Label.new()
				cond_label.text = "- " + cond_id + " (" + severity_text + ")"
				cond_label.add_theme_font_size_override("font_size", 9)
				cond_label.add_theme_color_override("font_color", COLOR_TEXT_DIM)
				content.add_child(cond_label)
	
	if not has_sick_pet:
		var healthy_label = Label.new()
		healthy_label.text = "Someone remembered to eat their Apples! 🍎"
		healthy_label.add_theme_font_size_override("font_size", 11)
		healthy_label.add_theme_color_override("font_color", COLOR_TEXT)
		healthy_label.autowrap_mode = TextServer.AUTOWRAP_WORD
		healthy_label.custom_minimum_size = Vector2(260, 60)
		content.add_child(healthy_label)
	
	# Close button
	var close_btn = Button.new()
	close_btn.text = "Close"
	close_btn.custom_minimum_size = Vector2(80, 25)
	close_btn.position = Vector2(120, 380)
	close_btn.pressed.connect(_toggle_doctors_note)
	doctors_note_overlay.add_child(close_btn)

func _show_in_memoriam():
	close_all_overlays()
	
	memoriam_overlay = Control.new()
	memoriam_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(memoriam_overlay)
	
	# Solid background
	var menu_bg = ColorRect.new()
	menu_bg.color = Color(COLOR_OVERLAY_PANEL.r, COLOR_OVERLAY_PANEL.g, COLOR_OVERLAY_PANEL.b, 1.0)
	menu_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	memoriam_overlay.add_child(menu_bg)
	
	# Title
	var title = Label.new()
	title.text = "In Memoriam"
	title.add_theme_font_size_override("font_size", 16)
	title.add_theme_color_override("font_color", COLOR_TEXT)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(0, 20)
	title.size = Vector2(320, 30)
	memoriam_overlay.add_child(title)
	
	# Content
	var scroll = ScrollContainer.new()
	scroll.position = Vector2(10, 60)
	scroll.size = Vector2(300, 300)
	memoriam_overlay.add_child(scroll)
	
	var content = VBoxContainer.new()
	content.add_theme_constant_override("separation", 10)
	scroll.add_child(content)
	
	# Show deceased pets
	var memoriam_list = game_manager.in_memoriam
	if memoriam_list.size() == 0:
		var empty_label = Label.new()
		empty_label.text = "No pets have passed away yet."
		empty_label.add_theme_font_size_override("font_size", 11)
		empty_label.add_theme_color_override("font_color", COLOR_TEXT_DIM)
		content.add_child(empty_label)
	else:
		for mem_pd in memoriam_list:
			# Create horizontal container for sprite + info
			var pet_row = HBoxContainer.new()
			pet_row.custom_minimum_size = Vector2(280, 40)
			pet_row.add_theme_constant_override("separation", 10)
			content.add_child(pet_row)
			
			# Sprite thumbnail container
			var sprite_container = Control.new()
			sprite_container.custom_minimum_size = Vector2(32, 32)
			pet_row.add_child(sprite_container)
			
			# Load sprite
			var sprite = Sprite2D.new()
			var sprite_loaded = false
			
			if mem_pd.death_sprite_path != "" and ResourceLoader.exists(mem_pd.death_sprite_path):
				var tex = load(mem_pd.death_sprite_path)
				if tex:
					sprite.texture = tex
					sprite_loaded = true
			
			if not sprite_loaded:
				# Fallback: load animal type placeholder
				var fallback_tex = load_icon_texture(mem_pd.animal_type)
				if fallback_tex:
					sprite.texture = fallback_tex
			
			sprite.scale = Vector2(0.5, 0.5)
			sprite_container.add_child(sprite)
			
			# Info column
			var info_col = VBoxContainer.new()
			pet_row.add_child(info_col)
			
			# Name and info
			var name_label = Label.new()
			name_label.text = mem_pd.name + " (" + mem_pd.animal_type + " " + mem_pd.gender[0].to_upper() + ")"
			name_label.add_theme_font_size_override("font_size", 11)
			name_label.add_theme_color_override("font_color", COLOR_TEXT)
			info_col.add_child(name_label)
			
			var age_label = Label.new()
			age_label.text = "Lived: " + str(mem_pd.age_at_death) + " minutes"
			age_label.add_theme_font_size_override("font_size", 9)
			age_label.add_theme_color_override("font_color", COLOR_TEXT_DIM)
			info_col.add_child(age_label)
	
	# Close button
	var close_btn = Button.new()
	close_btn.text = "Close"
	close_btn.custom_minimum_size = Vector2(80, 25)
	close_btn.position = Vector2(120, 380)
	close_btn.pressed.connect(_close_in_memoriam)
	memoriam_overlay.add_child(close_btn)

func _close_in_memoriam():
	if memoriam_overlay:
		memoriam_overlay.queue_free()
		memoriam_overlay = null

# Food types definition
const FOOD_TYPES = {
	"plant": {"name": "Plant", "icon": "food_plant", "stat": "hunger", "value": 20},
	"meat": {"name": "Meat", "icon": "food_meat", "stat": "hunger", "value": 25},
	"sweet": {"name": "Treat", "icon": "food_sweet", "stat": "happiness", "value": 15},
	"salty": {"name": "Salty", "icon": "food_salty", "stat": "energy", "value": 10}
}

# Medicine inventory - player can hold 1 of each medicine type
var medicine_inventory: Dictionary = {
	"woundClean": 0,
	"antibiotics": 0,
	"jointSupport": 0,
	"immuneBoost": 0,
	"dentalCare": 0,
	"moodStabilizer": 0,
	"energyTonic": 0,
	"appetiteStimulant": 0,
	"detoxHerbs": 0,
	"vitaminSupplements": 0
}

const MEDICINE_INFO = {
	"woundClean": {"name": "Wound Cleaning", "icon": "medicine"},
	"antibiotics": {"name": "Antibiotics", "icon": "medicine"},
	"jointSupport": {"name": "Joint Support", "icon": "medicine"},
	"immuneBoost": {"name": "Immune Booster", "icon": "medicine"},
	"dentalCare": {"name": "Dental Care", "icon": "medicine"},
	"moodStabilizer": {"name": "Mood Stabilizer", "icon": "medicine"},
	"energyTonic": {"name": "Energy Tonic", "icon": "medicine"},
	"appetiteStimulant": {"name": "Appetite Stimulant", "icon": "medicine"},
	"detoxHerbs": {"name": "Detox Herbs", "icon": "medicine"},
	"vitaminSupplements": {"name": "Vitamins", "icon": "medicine"}
}



func _show_feeding_panel():
	# Legacy entry point; route to the unified action-tab UX.
	_show_action_tab("feed")

func _close_feeding_panel():
	if feeding_panel_overlay:
		feeding_panel_overlay.queue_free()
		feeding_panel_overlay = null

func _on_food_item_selected(food_key: String):
	held_item = {"type": "food", "name": food_key, "icon": FOOD_TYPES[food_key]["icon"]}
	_close_feeding_panel()
	# Create held item sprite
	_create_held_item_sprite()

func _on_water_selected():
	held_item = {"type": "water", "name": "water", "icon": "water"}
	_close_feeding_panel()
	_create_held_item_sprite()

func _create_held_item_sprite():
	if held_item_sprite:
		held_item_sprite.queue_free()
	
	held_item_sprite = Sprite2D.new()
	var tex = load_icon_texture(held_item["icon"])
	if tex:
		held_item_sprite.texture = tex
		held_item_sprite.scale = Vector2(2, 2)
		held_item_sprite.z_index = 100
		add_child(held_item_sprite)

func _handle_held_item_click(click_position: Vector2):
	var pd = game_manager.get_active_pet_data()
	var pet = game_manager.get_active_pet()
	
	if pd == null or pet == null:
		_clear_held_item()
		return
	
	# Check if click is near the pet (pet area is around y=280)
	var pet_area = Rect2(pet.position - Vector2(40, 30), Vector2(80, 60))
	if not pet_area.has_point(click_position):
		# Clicked outside pet area - cancel held item
		_clear_held_item()
		return
	
	# Try to apply the held item
	var result = {"accepted": false, "correct": false, "message": ""}
	
	match held_item.type:
		"food":
			result.accepted = game_manager.apply_food(game_manager.active_pet_index, held_item.name)
			if result.accepted:
				result.message = "Ate it. +Need satisfied."
				if sound_manager:
					sound_manager.play_sound(pd.animal_type, "feed", pd.gender)
			else:
				result.message = "Not hungry right now."
				if sound_manager:
					sound_manager.play_sound(pd.animal_type, "idle", pd.gender)
		"water":
			result.accepted = game_manager.apply_water(game_manager.active_pet_index)
			if result.accepted:
				result.message = "Drank water. +Hydration."
				if sound_manager:
					sound_manager.play_sound(pd.animal_type, "drink", pd.gender)
			else:
				result.message = "Not thirsty right now."
		"medicine":
			result = game_manager.apply_medicine(game_manager.active_pet_index, held_item.name)
			if result.accepted:
				# Decrement medicine inventory
				var med_id = held_item.name
				medicine_inventory[med_id] = max(0, medicine_inventory.get(med_id, 0) - 1)
				if sound_manager:
					sound_manager.play_sound(pd.animal_type, "pet", pd.gender)
			else:
				if sound_manager:
					sound_manager.play_sound(pd.animal_type, "idle", pd.gender)
	
	# Clear held item after attempt
	_clear_held_item()
	
	# Show feedback message briefly
	_show_feedback_message(result.message)
	
	# Update stats
	_on_stats_updated()

func _clear_held_item():
	held_item = {"type": "", "name": "", "icon": ""}
	if held_item_sprite:
		held_item_sprite.queue_free()
		held_item_sprite = null

func _show_feedback_message(message: String):
	if message.strip_edges() == "":
		return

	var now_ms = Time.get_ticks_msec()
	if message == feedback_last_text and (now_ms - feedback_last_at_ms) < TOAST_REPEAT_SUPPRESS_MS:
		return
	feedback_last_text = message
	feedback_last_at_ms = now_ms

	if feedback_label and is_instance_valid(feedback_label):
		feedback_label.queue_free()
		feedback_label = null
	if feedback_tween and feedback_tween.is_running():
		feedback_tween.kill()
		feedback_tween = null

	feedback_label = Label.new()
	feedback_label.text = message
	feedback_label.add_theme_font_size_override("font_size", 12)
	feedback_label.add_theme_color_override("font_color", COLOR_TEXT)
	feedback_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	feedback_label.size = Vector2(float(get_window().size.x), 18)
	feedback_label.position = Vector2(0, float(get_window().size.y) - 150.0)
	feedback_label.modulate.a = 0.0
	feedback_label.z_index = 220
	add_child(feedback_label)
	var current_feedback := feedback_label

	feedback_tween = create_tween()
	feedback_tween.tween_property(current_feedback, "modulate:a", 1.0, TOAST_FADE_SEC)
	feedback_tween.tween_interval(TOAST_VISIBLE_SEC)
	feedback_tween.tween_property(current_feedback, "modulate:a", 0.0, TOAST_FADE_SEC)
	feedback_tween.tween_callback(func():
		if is_instance_valid(current_feedback):
			current_feedback.queue_free()
		if feedback_label == current_feedback:
			feedback_label = null
		feedback_tween = null
	)

func _toggle_sound(btn: Button):
	settings["sound_effects"] = not settings["sound_effects"]
	btn.text = "ON" if settings["sound_effects"] else "OFF"
	if sound_manager:
		sound_manager.set_enabled(settings["sound_effects"])

func play_sound(sound_name: String):
	if not settings.get("sound_effects", true):
		return
	
	# Try to load sound file, play if exists
	var sound_path = "res://sounds/" + sound_name + ".ogg"
	if ResourceLoader.exists(sound_path):
		var stream = load(sound_path)
		if stream:
			sound_player.stream = stream
			sound_player.play()
	else:
		# Try .wav
		sound_path = "res://sounds/" + sound_name + ".wav"
		if ResourceLoader.exists(sound_path):
			var stream = load(sound_path)
			if stream:
				sound_player.stream = stream
				sound_player.play()
		# Otherwise silently skip (placeholder system)

func _apply_ghost_mode():
	ghost_overlay = find_child("ghost_overlay", true, false)
	if settings["ghost_mode"]:
		if not ghost_overlay:
			ghost_overlay = ColorRect.new()
			ghost_overlay.name = "ghost_overlay"
			ghost_overlay.color = Color(0.1, 0.1, 0.15, 0.3)
			ghost_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
			add_child(ghost_overlay)
			# Move to front
			move_child(ghost_overlay, get_child_count() - 1)
	else:
		if ghost_overlay:
			ghost_overlay.queue_free()
			ghost_overlay = null

func _reset_save():
	# Clear all pets (fresh start) but keep settings
	game_manager.pet_datas.clear()
	game_manager.in_memoriam.clear()
	
	# Remove all pet nodes
	for pet in game_manager.pets:
		if pet:
			pet.queue_free()
	game_manager.pets.clear()
	game_manager.active_pet_index = 0
	
	# Save empty state
	_do_auto_save()
	
	# Fresh start - show egg selection, keep settings
	show_egg_selection()
