extends Node2D

const GameManager = preload("res://scripts/game_manager.gd")
const Pet = preload("res://scripts/pet.gd")
const PetData = preload("res://scripts/pet_data.gd")
const SoundManager = preload("res://scripts/sound_manager.gd")

var game_manager: GameManager
var ui_visible: bool = true
var window_has_focus: bool = true
var overlay_ui_pinned: bool = false
var window_positioned: bool = false
var egg_selection_active: bool = false
var doctors_note_visible: bool = false

var stats_panel: VBoxContainer
var actions_bar: HBoxContainer
var title_label: Label
var pet_portrait: TextureRect
var pet_name_label: Label
var pet_gender_label: Label
var pet_age_label: Label
var focus_backdrop: ColorRect
var hud_hit_surface: ColorRect
var background: ColorRect
var bg_mid: ColorRect
var bg_ground: ColorRect
var environment_stages: Array[Dictionary] = []
var nav_arrows: HBoxContainer
var add_pet_button: Button
var doctor_button: Button
var minimize_button: Button
var settings_button: Button
var pin_ui_button: Button
var basket_button: Button
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
var basket_overlay: Control
var doctors_note_overlay: Control
var feeding_panel_overlay: Control
var sound_player: AudioStreamPlayer
var ghost_overlay: ColorRect
var celestial_sprite: TextureRect
var sound_manager: SoundManager
var held_item_sprite: Sprite2D
var held_item: Dictionary = {"type": "", "name": "", "icon": ""}
var thrown_ball_sprite: Sprite2D
var celestial_texture_cache: Dictionary = {}
var egg_texture_cache: Dictionary = {}
var basket_entries: Array[Dictionary] = []
var portrait_texture_cache: Dictionary = {}
var environment_texture_cache: Dictionary = {}
var memorial_sprites: Dictionary = {}
var visual_camera: Camera2D
var _last_window_position: Vector2i = Vector2i.ZERO
var _window_shake_cooldown_sec: float = 0.0
var _window_shake_initialized: bool = false


# Game settings
const DEFAULT_SETTINGS = {
	"bell": true,
	"tick_rate": 60.0,
	"auto_save": true,
	"auto_save_interval": 3,
	"click_through": false,
	"desktop_companion_roam": true,
	"ghost_mode": false,
	"experimental_monitor_roam": false,
	"sound_effects": true,
	"pet_visual_animation_blending": true,
	"pet_visual_position_interpolation": true,
	"pet_visual_idle_micro_behaviors": true,
	"pet_visual_particle_effects": true,
	"pet_visual_window_shake_reaction": true,
	"main_menu_text_color": "#9cbd0f",
	"detail_text_color": "#d8e0d8",
	"status_box_palette": "classic"
}
var settings = DEFAULT_SETTINGS.duplicate(true)

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
var celestial_update_timer: float = 0.0
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
var automation_running: bool = false
var overlay_command_poll_sec: float = 0.0
var native_focus_poll_sec: float = 0.0
var native_focus_watcher_pid: int = -1
var native_focus_state: Dictionary = {}
var native_focus_state_updated_at_ms: int = -1
var native_focus_backend: String = ""
var debug_last_pinned_dispatch: Dictionary = {}
var habitat_proof_mode: bool = false

const AUTOMATION_ENV := "WEVITO_AUTOMATION"
const AUTOMATION_SCENARIO_ENV := "WEVITO_AUTOMATION_SCENARIO"
const AUTOMATION_SCREENSHOT_PATH_ENV := "WEVITO_AUTOMATION_SCREENSHOT_PATH"
const AUTOMATION_REPORT_PATH := "user://automation_report.json"
const AUTOMATION_SAVE_PATH := "user://save_slot.json"
const RUNTIME_STATE_PATH := "user://runtime_state.json"
const OVERLAY_PIN_ACTION := "toggle-overlay-ui"
const OVERLAY_PIN_HOTKEY := "Ctrl+Shift+P"
const BASKET_CAPTURE_ACTION := "capture-basket-link"
const BASKET_CAPTURE_HOTKEY := "Ctrl+Shift+B"
const OVERLAY_COMMAND_PATH := "user://overlay_command.json"
const OVERLAY_COMMAND_POLL_INTERVAL_SEC := 0.2
const NATIVE_FOCUS_POLL_INTERVAL_SEC := 0.04
const NATIVE_FOCUS_STALE_MS := 450
const NATIVE_CLICK_MAX_AGE_MS := 220
const NATIVE_HELPER_EXE := "WevitoDesktopBridge.exe"
const BASKET_MAX_LINKS := 5
const BASKET_SUPPORTED_DROP_EXTENSIONS := ["url", "webloc", "desktop", "txt"]

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
const HABITAT_MANIFEST_STAGE_SIZE := Vector2(400.0, 240.0)
const STAGE_NODE_KEYS := ["bg", "mid", "ground", "primary_shadow", "pet_shadow", "decor", "accent", "frame"]
const Z_BACKDROP := 0
const Z_FAR_PROP := 10
const Z_GROUND_CONTACT := 20
const Z_PET_SHADOW := 30
const Z_PET_BODY := 40
const Z_HELD_OR_CARRIED_PROP := 50
const Z_NEAR_OCCLUDER := 60
const Z_UI_OVERLAY := 70
const Z_FOCUS_BACKDROP := Z_BACKDROP - 5
const Z_MODAL_OVERLAY := Z_UI_OVERLAY + 430
const Z_FLOATING_FEEDBACK := Z_UI_OVERLAY + 150
const MONITOR_ROAM_MARGIN_X := 28.0
const MONITOR_ROAM_MARGIN_Y := 38.0
const MONITOR_ROAM_STRIP_HEIGHT := 170
const PET_ROAM_BAND_HEIGHT := 112.0
const DESKTOP_STAGE_MARGIN_RIGHT := 42.0
const DESKTOP_STAGE_MARGIN_BOTTOM := 42.0
const DESKTOP_STAGE_GAP := 10.0

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

func _ensure_runtime_input_actions():
	if not InputMap.has_action(OVERLAY_PIN_ACTION):
		InputMap.add_action(OVERLAY_PIN_ACTION)

	var has_pin_hotkey := false
	for event in InputMap.action_get_events(OVERLAY_PIN_ACTION):
		if event is InputEventKey:
			var key_event = event as InputEventKey
			if key_event.keycode == KEY_P and key_event.ctrl_pressed and key_event.shift_pressed:
				has_pin_hotkey = true
				break

	if not has_pin_hotkey:
		var pin_event = InputEventKey.new()
		pin_event.keycode = KEY_P
		pin_event.ctrl_pressed = true
		pin_event.shift_pressed = true
		InputMap.action_add_event(OVERLAY_PIN_ACTION, pin_event)

	if not InputMap.has_action(BASKET_CAPTURE_ACTION):
		InputMap.add_action(BASKET_CAPTURE_ACTION)

	for event in InputMap.action_get_events(BASKET_CAPTURE_ACTION):
		if event is InputEventKey:
			var key_event = event as InputEventKey
			if key_event.keycode == KEY_B and key_event.ctrl_pressed and key_event.shift_pressed:
				return

	var basket_event = InputEventKey.new()
	basket_event.keycode = KEY_B
	basket_event.ctrl_pressed = true
	basket_event.shift_pressed = true
	InputMap.action_add_event(BASKET_CAPTURE_ACTION, basket_event)

func _is_passive_overlay_mode() -> bool:
	return (not window_has_focus) and (not overlay_ui_pinned)

func _is_pinned_overlay_mode() -> bool:
	return (not window_has_focus) and overlay_ui_pinned

func _should_show_runtime_ui() -> bool:
	return window_has_focus or overlay_ui_pinned

func _should_allow_runtime_ui_input() -> bool:
	return window_has_focus or overlay_ui_pinned

func _sync_legacy_overlay_settings():
	settings["experimental_monitor_roam"] = settings.get("desktop_companion_roam", true)

func _update_overlay_pin_button():
	if pin_ui_button == null:
		return
	pin_ui_button.set_pressed_no_signal(overlay_ui_pinned)
	pin_ui_button.add_theme_color_override("font_color", COLOR_TEXT if overlay_ui_pinned else _detail_text_color())
	pin_ui_button.tooltip_text = "Pinned HUD %s. Toggle with %s." % [
		"enabled" if overlay_ui_pinned else "disabled",
		OVERLAY_PIN_HOTKEY
	]

func _apply_window_input_mode():
	DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_MOUSE_PASSTHROUGH, _is_passive_overlay_mode())
	DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_NO_FOCUS, _is_pinned_overlay_mode())
	if _is_passive_overlay_mode():
		get_window().mouse_passthrough_polygon = PackedVector2Array()
	else:
		get_window().mouse_passthrough_polygon = _build_active_overlay_input_polygon()
	_update_overlay_pin_button()

func _update_focus_backdrop():
	if focus_backdrop == null:
		return
	focus_backdrop.position = Vector2.ZERO
	focus_backdrop.size = Vector2(float(get_window().size.x), float(get_window().size.y))
	# Keep the desktop visible behind the overlay; only the game elements should render.
	focus_backdrop.visible = false

func _apply_runtime_visibility_state():
	ui_visible = _should_show_runtime_ui()
	_set_all_ui_controls_visible(ui_visible)
	_set_environment_stages_visible(true)
	_set_ui_clickable(_should_allow_runtime_ui_input())
	_update_focus_backdrop()
	_apply_window_input_mode()

func _set_overlay_ui_pin(enabled: bool, show_feedback: bool = true):
	if overlay_ui_pinned == enabled:
		_update_overlay_pin_button()
		return

	overlay_ui_pinned = enabled
	_apply_window_mode_layout(window_has_focus)
	if not overlay_ui_pinned and not window_has_focus:
		if action_tab_open:
			last_open_action_tab = current_action_tab
		if action_tab_overlay:
			action_tab_overlay.visible = false
	if overlay_ui_pinned and not window_has_focus:
		_recall_all_pets_home(1.2, true)
	elif not overlay_ui_pinned and not window_has_focus:
		if monitor_roam_active:
			_start_monitor_roam_all_pets()
		else:
			_recall_all_pets_home(0.0)
	_apply_runtime_visibility_state()
	if not window_has_focus:
		_restore_external_foreground_focus_if_needed()

	if show_feedback:
		var feedback_message = "HUD pinned. UI stays usable over other apps."
		if not overlay_ui_pinned:
			feedback_message = "HUD released. UI hides again when another app is focused."
		_show_feedback_message(feedback_message)

func _update_basket_button_state():
	if basket_button == null:
		return
	var count = basket_entries.size()
	var font_color = _detail_text_color()
	if count > 0:
		font_color = _main_text_color()
	if count >= BASKET_MAX_LINKS:
		font_color = Color(0.95, 0.72, 0.24, 1.0)
	basket_button.text = "BIN"
	basket_button.add_theme_color_override("font_color", font_color)
	basket_button.tooltip_text = "Link basket %d/%d. Capture clipboard with %s." % [
		count,
		BASKET_MAX_LINKS,
		BASKET_CAPTURE_HOTKEY
	]

func _strip_wrapping_punctuation(text: String) -> String:
	var trimmed = text.strip_edges()
	var leading = "\"'(<[{"
	var trailing = "\"').,;:!?]}>"
	while trimmed.length() > 0 and leading.contains(trimmed.substr(0, 1)):
		trimmed = trimmed.substr(1)
	while trimmed.length() > 0 and trailing.contains(trimmed.substr(trimmed.length() - 1, 1)):
		trimmed = trimmed.substr(0, trimmed.length() - 1)
	return trimmed

func _normalize_basket_url(text: String) -> String:
	var normalized = _strip_wrapping_punctuation(text)
	if normalized == "":
		return ""
	var regex = RegEx.new()
	if regex.compile("(?i)\\b((?:https?|ftp)://[^\\s\"'<>]+|mailto:[^\\s\"'<>]+|www\\.[^\\s\"'<>]+)") == OK:
		var match = regex.search(normalized)
		if match:
			normalized = _strip_wrapping_punctuation(match.get_string(1))
	if normalized == "":
		return ""
	if normalized.begins_with("www."):
		normalized = "https://" + normalized
	elif normalized.find("://") == -1 and not normalized.begins_with("mailto:") and normalized.find(" ") == -1 and normalized.find("\t") == -1 and normalized.find(".") >= 0:
		normalized = "https://" + normalized
	if normalized.find(" ") >= 0 or normalized.find("\t") >= 0:
		return ""
	return normalized

func _basket_contains_url(url: String) -> bool:
	for entry in basket_entries:
		if str(entry.get("url", "")).to_lower() == url.to_lower():
			return true
	return false

func _basket_source_label(source: String) -> String:
	return _truncate_with_ellipsis(source.strip_edges(), 32)

func _basket_entry_summary(entry: Dictionary) -> String:
	var url = str(entry.get("url", ""))
	var label = url
	var scheme_index = label.find("://")
	if scheme_index >= 0:
		label = label.substr(scheme_index + 3)
	if label.ends_with("/"):
		label = label.substr(0, label.length() - 1)
	return _truncate_with_ellipsis(label, 34)

func _persist_basket_if_enabled():
	if settings.get("auto_save", true):
		_do_auto_save()

func _basket_add_url(raw_url: String, source: String = "clipboard", show_feedback: bool = true) -> bool:
	var url = _normalize_basket_url(raw_url)
	if url == "":
		if show_feedback:
			_show_feedback_message("Basket only accepts valid links.")
		return false
	if _basket_contains_url(url):
		if show_feedback:
			_show_feedback_message("That link is already in the basket.")
		return false
	if basket_entries.size() >= BASKET_MAX_LINKS:
		if show_feedback:
			_show_feedback_message("Basket is full. Remove a link before adding another.")
		return false
	basket_entries.append({
		"url": url,
		"source": _basket_source_label(source),
		"added_at": int(Time.get_unix_time_from_system())
	})
	_update_basket_button_state()
	_refresh_basket_overlay()
	_persist_basket_if_enabled()
	if show_feedback:
		_show_feedback_message("Saved link to basket (%d/%d)." % [basket_entries.size(), BASKET_MAX_LINKS])
	return true

func _remove_basket_entry(index: int, show_feedback: bool = true):
	if index < 0 or index >= basket_entries.size():
		return
	basket_entries.remove_at(index)
	_update_basket_button_state()
	_refresh_basket_overlay()
	_persist_basket_if_enabled()
	if show_feedback:
		_show_feedback_message("Removed link from basket.")

func _capture_clipboard_link(show_feedback: bool = true) -> bool:
	var clipboard_text = DisplayServer.clipboard_get().strip_edges()
	if clipboard_text == "":
		if show_feedback:
			_show_feedback_message("Clipboard is empty.")
		return false
	return _basket_add_url(clipboard_text, "clipboard", show_feedback)

func _copy_basket_entry_to_clipboard(index: int, close_after: bool = true):
	if index < 0 or index >= basket_entries.size():
		return
	var entry = basket_entries[index]
	var url = str(entry.get("url", ""))
	if url == "":
		return
	DisplayServer.clipboard_set(url)
	if close_after:
		_close_basket_overlay()
	_show_feedback_message("Copied link to clipboard.")

func _open_basket_entry(index: int):
	if index < 0 or index >= basket_entries.size():
		return
	var url = str(basket_entries[index].get("url", ""))
	if url == "":
		return
	var err = OS.shell_open(url)
	if err != OK:
		_show_feedback_message("Could not open that link.")
		return
	_close_basket_overlay()
	_show_feedback_message("Opened link.")

func _read_url_from_drop_file(file_path: String) -> String:
	var ext = file_path.get_extension().to_lower()
	if not BASKET_SUPPORTED_DROP_EXTENSIONS.has(ext):
		return ""
	if not FileAccess.file_exists(file_path):
		return ""
	var file = FileAccess.open(file_path, FileAccess.READ)
	if file == null:
		return ""
	var raw = file.get_as_text()
	file.close()
	if raw.strip_edges() == "":
		return ""
	if ext == "url" or ext == "desktop":
		for line in raw.split("\n", false):
			var trimmed = line.strip_edges()
			if trimmed.to_upper().begins_with("URL="):
				return _normalize_basket_url(trimmed.substr(4))
	if ext == "webloc":
		var plist_regex = RegEx.new()
		if plist_regex.compile("(?is)<key>URL</key>\\s*<string>(.*?)</string>") == OK:
			var plist_match = plist_regex.search(raw)
			if plist_match:
				return _normalize_basket_url(plist_match.get_string(1))
	return _normalize_basket_url(raw)

func _on_window_files_dropped(files: PackedStringArray):
	var added := 0
	for file_path in files:
		var url = _read_url_from_drop_file(file_path)
		if url != "" and _basket_add_url(url, file_path.get_file(), false):
			added += 1
	if added > 0:
		_show_feedback_message("Added %d dropped link%s to basket." % [added, "" if added == 1 else "s"])
	elif files.size() > 0:
		_show_feedback_message("Drop a URL shortcut or a text file that contains a link.")

func _on_overlay_pin_toggled(enabled: bool):
	_set_overlay_ui_pin(enabled)

func _native_focus_watcher_supported() -> bool:
	return OS.get_name() == "Windows" and not str(DisplayServer.get_name()).containsn("headless")

func _native_helper_candidates() -> PackedStringArray:
	var candidates = PackedStringArray()
	var executable_path = OS.get_executable_path()
	if executable_path != "":
		candidates.append(executable_path.get_base_dir().path_join(NATIVE_HELPER_EXE))
	candidates.append(ProjectSettings.globalize_path("res://builds/release/%s" % NATIVE_HELPER_EXE))
	candidates.append(ProjectSettings.globalize_path("res://builds/desktop_bridge/%s" % NATIVE_HELPER_EXE))
	return candidates

func _native_helper_path() -> String:
	for candidate in _native_helper_candidates():
		if candidate != "" and FileAccess.file_exists(candidate):
			return candidate
	return ""

func _native_helper_available() -> bool:
	return _native_helper_path() != ""

func _native_focus_runtime_id() -> int:
	return OS.get_process_id()

func _native_focus_state_path() -> String:
	return "user://native_focus_state_%d.json" % _native_focus_runtime_id()

func _native_focus_stop_path() -> String:
	return "user://native_focus_stop_%d.signal" % _native_focus_runtime_id()

func _native_focus_script_path() -> String:
	return "user://native_focus_watcher_%d.ps1" % _native_focus_runtime_id()

func _ps_single_quote(text: String) -> String:
	return "'" + text.replace("'", "''") + "'"

func _native_focus_watcher_script_text() -> String:
	var parent_pid = _native_focus_runtime_id()
	var state_path = _ps_single_quote(ProjectSettings.globalize_path(_native_focus_state_path()))
	var stop_path = _ps_single_quote(ProjectSettings.globalize_path(_native_focus_stop_path()))
	var lines = PackedStringArray([
		"$ErrorActionPreference = 'SilentlyContinue'",
		"Add-Type @\"",
		"using System;",
		"using System.Text;",
		"using System.Runtime.InteropServices;",
		"public static class NativeWindow {",
		"    [DllImport(\"user32.dll\")] public static extern IntPtr GetForegroundWindow();",
		"    [DllImport(\"user32.dll\")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);",
		"    [DllImport(\"user32.dll\", CharSet = CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder text, int count);",
		"    [DllImport(\"user32.dll\", CharSet = CharSet.Unicode)] public static extern int GetClassNameW(IntPtr hWnd, StringBuilder text, int count);",
		"    [DllImport(\"user32.dll\")] public static extern short GetAsyncKeyState(int vKey);",
		"}",
		"\"@",
		"$parentPid = %d" % parent_pid,
		"$statePath = %s" % state_path,
		"$stopPath = %s" % stop_path,
		"$lastJson = ''",
		"$lastLeftDown = $false",
		"$lastLeftPressMs = 0",
		"$lastLeftReleaseMs = 0",
		"while ($true) {",
		"    if (Test-Path $stopPath) { break }",
		"    if ($null -eq (Get-Process -Id $parentPid -ErrorAction SilentlyContinue)) { break }",
		"    $hwnd = [NativeWindow]::GetForegroundWindow()",
		"    $foregroundPid = 0",
		"    $windowTitle = ''",
		"    $windowClass = ''",
		"    if ($hwnd -ne [IntPtr]::Zero) {",
		"        [void][NativeWindow]::GetWindowThreadProcessId($hwnd, [ref]$foregroundPid)",
		"        $titleBuilder = New-Object System.Text.StringBuilder 512",
		"        $classBuilder = New-Object System.Text.StringBuilder 128",
		"        [void][NativeWindow]::GetWindowTextW($hwnd, $titleBuilder, $titleBuilder.Capacity)",
		"        [void][NativeWindow]::GetClassNameW($hwnd, $classBuilder, $classBuilder.Capacity)",
		"        $windowTitle = $titleBuilder.ToString()",
		"        $windowClass = $classBuilder.ToString()",
		"    }",
		"    $processName = ''",
		"    if ($foregroundPid -gt 0) {",
		"        $proc = Get-Process -Id $foregroundPid -ErrorAction SilentlyContinue",
		"        if ($proc) { $processName = $proc.ProcessName }",
		"    }",
		"    $isShellSurface = $windowClass -in @('Progman', 'WorkerW', 'Shell_TrayWnd')",
		"    $leftButtonDown = ([NativeWindow]::GetAsyncKeyState(0x01) -band 0x8000) -ne 0",
		"    $nowMs = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()",
		"    if ($leftButtonDown -and -not $lastLeftDown) { $lastLeftPressMs = $nowMs }",
		"    elseif (-not $leftButtonDown -and $lastLeftDown) { $lastLeftReleaseMs = $nowMs }",
		"    $lastLeftDown = $leftButtonDown",
		"    $payload = @{",
		"        foreground_pid = [int]$foregroundPid",
		"        process_name = $processName",
		"        window_class = $windowClass",
		"        window_title = $windowTitle",
		"        is_shell_surface = [bool]$isShellSurface",
		"        left_button_down = [bool]$leftButtonDown",
		"        last_left_press_ms = [int64]$lastLeftPressMs",
		"        last_left_release_ms = [int64]$lastLeftReleaseMs",
		"        updated_at_unix_ms = [int64]$nowMs",
		"    } | ConvertTo-Json -Compress",
		"    if ($payload -ne $lastJson) {",
		"        Set-Content -Path $statePath -Value $payload -Encoding UTF8",
		"        $lastJson = $payload",
		"    }",
		"    Start-Sleep -Milliseconds 40",
		"}"
	])
	return "\n".join(lines)

func _start_native_focus_watcher():
	if not _native_focus_watcher_supported() or _automation_requested():
		return

	var stop_path = _native_focus_stop_path()
	var state_path = _native_focus_state_path()
	native_focus_backend = ""
	if FileAccess.file_exists(stop_path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(stop_path))
	if FileAccess.file_exists(state_path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(state_path))

	var helper_path = _native_helper_path()
	if helper_path != "":
		native_focus_watcher_pid = OS.create_process(
			helper_path,
			PackedStringArray([
				"watch-focus",
				"--parent-pid",
				str(_native_focus_runtime_id()),
				"--state-path",
				ProjectSettings.globalize_path(state_path),
				"--stop-path",
				ProjectSettings.globalize_path(stop_path)
			]),
			false
		)
		if native_focus_watcher_pid != -1:
			native_focus_backend = "helper"
			return

	var script_path = _native_focus_script_path()
	var script_file = FileAccess.open(script_path, FileAccess.WRITE)
	if script_file == null:
		return
	script_file.store_string(_native_focus_watcher_script_text())
	script_file.close()

	native_focus_watcher_pid = OS.create_process(
		"powershell.exe",
		PackedStringArray([
			"-NoProfile",
			"-ExecutionPolicy",
			"Bypass",
			"-WindowStyle",
			"Hidden",
			"-File",
			ProjectSettings.globalize_path(script_path)
		]),
		false
	)
	if native_focus_watcher_pid != -1:
		native_focus_backend = "powershell"

func _stop_native_focus_watcher():
	if not _native_focus_watcher_supported():
		return
	var stop_file = FileAccess.open(_native_focus_stop_path(), FileAccess.WRITE)
	if stop_file:
		stop_file.store_string("stop")
		stop_file.close()
	native_focus_backend = ""

func _poll_native_focus_state(delta: float):
	if not _native_focus_watcher_supported() or _automation_requested():
		return

	native_focus_poll_sec += delta
	if native_focus_poll_sec < NATIVE_FOCUS_POLL_INTERVAL_SEC:
		return
	native_focus_poll_sec = 0.0

	var state_path = _native_focus_state_path()
	if not FileAccess.file_exists(state_path):
		return

	var file = FileAccess.open(state_path, FileAccess.READ)
	if file == null:
		return
	var raw = file.get_as_text().strip_edges()
	file.close()
	if raw == "":
		return

	var parsed = JSON.parse_string(raw)
	if not (parsed is Dictionary):
		return

	native_focus_state = parsed as Dictionary
	native_focus_state_updated_at_ms = int(native_focus_state.get("updated_at_unix_ms", native_focus_state_updated_at_ms))

func _native_focus_state_is_fresh() -> bool:
	if native_focus_state_updated_at_ms < 0:
		return false
	var now_ms = int(Time.get_unix_time_from_system() * 1000.0)
	return abs(now_ms - native_focus_state_updated_at_ms) <= NATIVE_FOCUS_STALE_MS

func _native_left_click_recent(max_age_ms: int = NATIVE_CLICK_MAX_AGE_MS) -> bool:
	if not _native_focus_state_is_fresh():
		return false
	var now_ms = int(Time.get_unix_time_from_system() * 1000.0)
	var last_press_ms = int(native_focus_state.get("last_left_press_ms", -1))
	var last_release_ms = int(native_focus_state.get("last_left_release_ms", -1))
	if last_press_ms > 0 and abs(now_ms - last_press_ms) <= max_age_ms:
		return true
	if last_release_ms > 0 and abs(now_ms - last_release_ms) <= max_age_ms:
		return true
	return false

func _native_recent_click_info(max_age_ms: int = NATIVE_CLICK_MAX_AGE_MS) -> Dictionary:
	if not _native_focus_state_is_fresh():
		return {}

	var now_ms = int(Time.get_unix_time_from_system() * 1000.0)
	var release_ms = int(native_focus_state.get("last_left_release_ms", -1))
	if release_ms > 0 and abs(now_ms - release_ms) <= max_age_ms:
		var release_point = Vector2(
			float(native_focus_state.get("last_left_release_x", -1000000.0)),
			float(native_focus_state.get("last_left_release_y", -1000000.0))
		)
		if release_point.x > -100000.0 and release_point.y > -100000.0:
			return {
				"point": release_point,
				"age_ms": abs(now_ms - release_ms),
				"phase": "release"
			}

	var press_ms = int(native_focus_state.get("last_left_press_ms", -1))
	if press_ms > 0 and abs(now_ms - press_ms) <= max_age_ms:
		var press_point = Vector2(
			float(native_focus_state.get("last_left_press_x", -1000000.0)),
			float(native_focus_state.get("last_left_press_y", -1000000.0))
		)
		if press_point.x > -100000.0 and press_point.y > -100000.0:
			return {
				"point": press_point,
				"age_ms": abs(now_ms - press_ms),
				"phase": "press"
			}

	return {}

func _screen_to_window_local_point(screen_point: Vector2) -> Vector2:
	return screen_point - Vector2(get_window().position)

func _native_cursor_screen_point() -> Vector2:
	if not _native_focus_state_is_fresh():
		return Vector2(-1000000.0, -1000000.0)
	return Vector2(
		float(native_focus_state.get("cursor_x", -1000000.0)),
		float(native_focus_state.get("cursor_y", -1000000.0))
	)

func _native_focus_is_effectively_ours() -> bool:
	if not _native_focus_state_is_fresh():
		return get_window().has_focus()
	var foreground_pid = int(native_focus_state.get("foreground_pid", -1))
	if foreground_pid == _native_focus_runtime_id():
		return true
	if bool(native_focus_state.get("is_shell_surface", false)):
		return true
	if foreground_pid <= 0:
		return true
	return false

func _restore_external_foreground_focus_if_needed():
	if not _native_focus_watcher_supported() or _automation_requested():
		return
	if not _native_focus_state_is_fresh():
		return

	var foreground_pid = int(native_focus_state.get("foreground_pid", -1))
	if foreground_pid <= 0 or foreground_pid == _native_focus_runtime_id():
		return

	var foreground_hwnd = int(native_focus_state.get("foreground_hwnd", 0))
	var helper_path = _native_helper_path()
	if helper_path != "" and foreground_hwnd > 0:
		OS.create_process(
			helper_path,
			PackedStringArray([
				"activate-window",
				"--hwnd",
				str(foreground_hwnd),
				"--delay-ms",
				"120"
			]),
			false
		)
		return

	OS.create_process(
		"powershell.exe",
		PackedStringArray([
			"-NoProfile",
			"-ExecutionPolicy",
			"Bypass",
			"-WindowStyle",
			"Hidden",
			"-Command",
			"Start-Sleep -Milliseconds 120; $shell = New-Object -ComObject WScript.Shell; [void]$shell.AppActivate(%d)" % foreground_pid
		]),
		false
	)

func _replay_current_left_click_after_focus():
	if not _native_focus_watcher_supported() or _automation_requested():
		return
	var helper_path = _native_helper_path()
	if helper_path != "":
		OS.create_process(
			helper_path,
			PackedStringArray([
				"left-click",
				"--delay-ms",
				"70",
				"--hold-ms",
				"18"
			]),
			false
		)
		return
	OS.create_process(
		"powershell.exe",
		PackedStringArray([
			"-NoProfile",
			"-ExecutionPolicy",
			"Bypass",
			"-WindowStyle",
			"Hidden",
			"-Command",
			"Start-Sleep -Milliseconds 70; Add-Type @\"`nusing System;`nusing System.Runtime.InteropServices;`npublic static class NativeClick {`n    [DllImport(\"user32.dll\")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);`n}`n\"@; [NativeClick]::mouse_event(0x0002,0,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 18; [NativeClick]::mouse_event(0x0004,0,0,0,[UIntPtr]::Zero)"
		]),
		false
	)

func _poll_overlay_command(delta: float):
	overlay_command_poll_sec += delta
	if overlay_command_poll_sec < OVERLAY_COMMAND_POLL_INTERVAL_SEC:
		return
	overlay_command_poll_sec = 0.0

	if not FileAccess.file_exists(OVERLAY_COMMAND_PATH):
		return

	var file = FileAccess.open(OVERLAY_COMMAND_PATH, FileAccess.READ)
	if file == null:
		return

	var raw = file.get_as_text().strip_edges()
	file.close()
	DirAccess.remove_absolute(ProjectSettings.globalize_path(OVERLAY_COMMAND_PATH))

	if raw == "":
		return

	var parsed = JSON.parse_string(raw)
	if not (parsed is Dictionary):
		return

	var command = str((parsed as Dictionary).get("command", "")).to_lower()
	match command:
		"toggle_overlay_ui":
			_set_overlay_ui_pin(not overlay_ui_pinned)
		"pin_overlay_ui":
			_set_overlay_ui_pin(true)
		"release_overlay_ui":
			_set_overlay_ui_pin(false)
		"capture_clipboard_link":
			_capture_clipboard_link(false)
		"dump_runtime_state":
			_write_runtime_state()
		"show_window":
			_show_from_tray()

func _reconcile_window_focus_state():
	if startup_focus_guard or automation_running or _automation_requested():
		return
	var actual_focus = _native_focus_is_effectively_ours() if _native_focus_watcher_supported() else get_window().has_focus()
	if actual_focus != window_has_focus:
		_handle_focus_change(actual_focus)

func _ready():
	position_window()
	window_has_focus = true
	_setup_visual_camera()
	_ensure_runtime_input_actions()
	_start_native_focus_watcher()
	if get_window() and not get_window().files_dropped.is_connected(_on_window_files_dropped):
		get_window().files_dropped.connect(_on_window_files_dropped)

	focus_backdrop = ColorRect.new()
	focus_backdrop.color = Color(0.0, 0.0, 0.0, 0.0)
	focus_backdrop.mouse_filter = Control.MOUSE_FILTER_IGNORE
	focus_backdrop.z_index = Z_FOCUS_BACKDROP
	add_child(focus_backdrop)

	hud_hit_surface = ColorRect.new()
	hud_hit_surface.color = Color(0.0, 0.0, 0.0, 0.01)
	hud_hit_surface.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hud_hit_surface.z_index = Z_UI_OVERLAY
	add_child(hud_hit_surface)
	
	# Create audio player
	sound_player = AudioStreamPlayer.new()
	add_child(sound_player)
	
	# Create sound manager
	sound_manager = SoundManager.new()
	add_child(sound_manager)
	sound_manager.set_enabled(settings.get("sound_effects", true))
	
	# Create environment stage layers (one slot initially; grows with pet count)
	_ensure_environment_stage_nodes(1)

	celestial_sprite = TextureRect.new()
	celestial_sprite.mouse_filter = Control.MOUSE_FILTER_IGNORE
	celestial_sprite.z_index = Z_FAR_PROP
	celestial_sprite.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	celestial_sprite.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	add_child(celestial_sprite)
	
	game_manager = GameManager.new()
	add_child(game_manager)
	_apply_runtime_settings()
	
	game_manager.stats_updated.connect(_on_stats_updated)
	game_manager.pet_died.connect(_on_pet_died)
	game_manager.naming_needed.connect(_on_naming_needed)
	game_manager.pet_state_changed.connect(_on_pet_state_changed)
	game_manager.active_pet_changed.connect(_on_active_pet_changed)
	game_manager.pet_added.connect(_on_pet_added)

	var restored_from_save = _load_save_state()
	_apply_runtime_settings()
	
	create_ui()
	_update_basket_button_state()
	_apply_window_mode_layout(window_has_focus)
	_apply_ghost_mode()
	_apply_runtime_visibility_state()
	
	# Add first pet with egg selection if no save restored playable pets.
	if not restored_from_save:
		show_egg_selection()
	
	_on_stats_updated()
	update_environment_background()
	_update_celestial_sprite(true)
	var pending_name_index = _find_pending_name_index()
	if pending_name_index >= 0:
		call_deferred("_show_naming_popup", pending_name_index)
	call_deferred("_finalize_startup_focus")
	if _automation_requested():
		call_deferred("_run_automation_suite")

func _exit_tree():
	_stop_native_focus_watcher()

func _apply_runtime_settings():
	_sync_legacy_overlay_settings()
	if game_manager:
		game_manager.tick_rate = max(1.0, float(settings.get("tick_rate", game_manager.tick_rate)))
	if sound_manager:
		sound_manager.set_enabled(settings.get("sound_effects", true))
	if game_manager:
		for pet in game_manager.pets:
			if pet == null:
				continue
			pet.animation_blending_enabled = bool(settings.get("pet_visual_animation_blending", true))
			pet.position_interpolation_enabled = bool(settings.get("pet_visual_position_interpolation", true))
			pet.idle_micro_behaviors_enabled = bool(settings.get("pet_visual_idle_micro_behaviors", true))
			pet.particle_effects_enabled = bool(settings.get("pet_visual_particle_effects", true))
			pet.window_shake_reaction_enabled = bool(settings.get("pet_visual_window_shake_reaction", true))

func _automation_requested() -> bool:
	return OS.get_environment(AUTOMATION_ENV) == "1"

func _finalize_startup_focus():
	window_has_focus = true
	_apply_window_mode_layout(window_has_focus)
	_apply_runtime_visibility_state()
	await get_tree().create_timer(1.5).timeout
	startup_focus_guard = false

func _process(delta):
	_poll_native_focus_state(delta)
	_reconcile_window_focus_state()
	_detect_user_window_shake(delta)
	_cleanup_expired_memorials()

	# Pet nodes are processed automatically by Godot since they're in the scene tree
	if not ui_visible:
		# Enforce unfocused presentation in case any UI controls were created after focus-out.
		_set_all_ui_controls_visible(false)
		_set_environment_stages_visible(true)

	_poll_overlay_command(delta)
	
	# Update held item sprite to follow mouse
	if held_item_sprite and held_item.type != "":
		held_item_sprite.position = get_global_mouse_position()
	
	# Auto-save
	if settings.get("auto_save", true):
		auto_save_timer += delta
		if auto_save_timer >= settings.get("auto_save_interval", 60):
			auto_save_timer = 0
			_do_auto_save()

	celestial_update_timer += delta
	if celestial_update_timer >= 5.0:
		celestial_update_timer = 0.0
		_update_celestial_sprite()

func _setup_visual_camera():
	if visual_camera and is_instance_valid(visual_camera):
		return
	visual_camera = Camera2D.new()
	visual_camera.name = "VisualSmoothingCamera"
	visual_camera.position = Vector2.ZERO
	visual_camera.position_smoothing_enabled = true
	visual_camera.position_smoothing_speed = 8.0
	visual_camera.make_current()
	add_child(visual_camera)

func _detect_user_window_shake(delta: float):
	if startup_focus_guard or automation_running or _automation_requested():
		return
	if not window_has_focus:
		_window_shake_initialized = false
		return
	_window_shake_cooldown_sec = max(0.0, _window_shake_cooldown_sec - delta)
	var current_position = DisplayServer.window_get_position()
	if not _window_shake_initialized:
		_last_window_position = current_position
		_window_shake_initialized = true
		return
	var window_delta = Vector2(current_position - _last_window_position).length()
	_last_window_position = current_position
	if window_delta < 90.0 or _window_shake_cooldown_sec > 0.0:
		return
	_window_shake_cooldown_sec = 4.0
	if game_manager == null:
		return
	for pet in game_manager.pets:
		if pet and pet.has_method("request_window_shake_reaction"):
			pet.request_window_shake_reaction()

func _do_auto_save():
	# Auto-save to single file
	var save_data = {
		"schema_version": 2,
		"pets": [],
		"in_memoriam": [],
		"link_basket": [],
		"timestamp": Time.get_unix_time_from_system(),
		"auto": true,
		"active_pet_index": game_manager.active_pet_index
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
				"water_bowl_capacity": pet_data.water_bowl_capacity,
				"is_dead": pet_data.is_dead,
				"is_ghost": pet_data.is_ghost,
				"death_elapsed_sec": pet_data.death_elapsed_sec,
				"memorial_object_id": pet_data.memorial_object_id,
				"memorial_position": {"x": pet_data.memorial_position.x, "y": pet_data.memorial_position.y},
				"memorial_expires_at": pet_data.memorial_expires_at,
				"is_sleeping": pet_data.is_sleeping,
				"is_hatching": pet_data.is_hatching,
				"is_naming_pending": pet_data.is_naming_pending,
				"emotion": pet_data.emotion,
				"stage": pet_data.stage,
				"position": {"x": pet_data.position.x, "y": pet_data.position.y},
				"target_position": {"x": pet_data.target_position.x, "y": pet_data.target_position.y},
				"is_wandering": pet_data.is_wandering,
				"active_treatments": pet_data.active_treatments,
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
	for entry in basket_entries:
		save_data["link_basket"].append({
			"url": str(entry.get("url", "")),
			"source": str(entry.get("source", "")),
			"added_at": int(entry.get("added_at", int(Time.get_unix_time_from_system())))
		})
	
	var save_path = "user://save_slot.json"
	var file = FileAccess.open(save_path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(save_data))
		file.close()

func _merge_settings(saved_settings: Dictionary) -> Dictionary:
	var merged = DEFAULT_SETTINGS.duplicate(true)
	for key in saved_settings.keys():
		merged[key] = saved_settings[key]
	return merged

func _dict_to_vector2(value, fallback: Vector2) -> Vector2:
	if value is Dictionary:
		return Vector2(
			float((value as Dictionary).get("x", fallback.x)),
			float((value as Dictionary).get("y", fallback.y))
		)
	return fallback

func _restore_in_memoriam_entry(entry: Dictionary):
	var pd = PetData.new()
	pd.name = str(entry.get("name", pd.name))
	pd.animal_type = str(entry.get("animal_type", pd.animal_type))
	pd.gender = str(entry.get("gender", pd.gender))
	pd.age_at_death = int(entry.get("age_at_death", pd.age_at_death))
	pd.death_sprite_path = str(entry.get("death_sprite_path", ""))
	game_manager.in_memoriam.append(pd)

func _restore_pet_from_save(entry: Dictionary):
	if not game_manager.can_add_pet():
		return

	var pet = Pet.new()
	add_child(pet)
	game_manager.pets.append(pet)

	var default_pos = Vector2(80.0 + (game_manager.pets.size() - 1) * 100.0, 280.0)
	var pd = PetData.new()
	pd.name = str(entry.get("name", pd.name))
	pd.animal_type = str(entry.get("animal_type", pd.animal_type))
	pd.egg_color = str(entry.get("egg_color", pd.egg_color))
	pd.gender = str(entry.get("gender", pd.gender))
	pd.age_minutes = int(entry.get("age_minutes", pd.age_minutes))
	pd.hunger = float(entry.get("hunger", pd.hunger))
	pd.hydration = float(entry.get("hydration", pd.hydration))
	pd.happiness = float(entry.get("happiness", pd.happiness))
	pd.energy = float(entry.get("energy", pd.energy))
	pd.health = float(entry.get("health", pd.health))
	pd.cleanliness = float(entry.get("cleanliness", pd.cleanliness))
	pd.affection = float(entry.get("affection", pd.affection))
	pd.grooming = float(entry.get("grooming", pd.grooming))
	pd.fitness = float(entry.get("fitness", pd.fitness))
	pd.water_bowl_level = float(entry.get("water_bowl_level", pd.water_bowl_level))
	pd.water_bowl_capacity = float(entry.get("water_bowl_capacity", pd.water_bowl_capacity))
	pd.is_dead = bool(entry.get("is_dead", false))
	pd.is_ghost = bool(entry.get("is_ghost", false))
	pd.death_elapsed_sec = float(entry.get("death_elapsed_sec", 0.0))
	pd.memorial_object_id = str(entry.get("memorial_object_id", ""))
	pd.memorial_position = _dict_to_vector2(entry.get("memorial_position", {}), pd.position)
	pd.memorial_expires_at = int(entry.get("memorial_expires_at", 0))
	pd.is_sleeping = bool(entry.get("is_sleeping", false))
	var was_hatching = bool(entry.get("is_hatching", false))
	pd.is_hatching = false
	pd.is_naming_pending = bool(entry.get("is_naming_pending", was_hatching and pd.name == "Wevito"))
	pd.emotion = str(entry.get("emotion", pd.emotion))
	pd.position = _dict_to_vector2(entry.get("position", {}), default_pos)
	pd.target_position = _dict_to_vector2(entry.get("target_position", {}), pd.position)
	pd.is_wandering = bool(entry.get("is_wandering", false))
	var saved_conditions = entry.get("conditions", {})
	if saved_conditions is Dictionary:
		pd.conditions = (saved_conditions as Dictionary).duplicate(true)
	var saved_treatments = entry.get("active_treatments", [])
	if saved_treatments is Array:
		pd.active_treatments.clear()
		for treatment in saved_treatments:
			if treatment is Dictionary:
				pd.active_treatments.append((treatment as Dictionary).duplicate(true))

	var personality = entry.get("personality", {})
	if personality is Dictionary:
		pd.food_love = float(personality.get("food_love", pd.food_love))
		pd.cuddle_need = float(personality.get("cuddle_need", pd.cuddle_need))
		pd.pet_cleanliness = float(personality.get("pet_cleanliness", pd.pet_cleanliness))
		pd.activity_level = float(personality.get("activity_level", pd.activity_level))
		pd.cheerfulness = float(personality.get("cheerfulness", pd.cheerfulness))
		pd.social_need = float(personality.get("social_need", pd.social_need))
		pd.playfulness = float(personality.get("playfulness", pd.playfulness))
		pd.stubbornness = float(personality.get("stubbornness", pd.stubbornness))

	pd.stage = int(entry.get("stage", pd.get_stage_from_age(pd.age_minutes)))
	game_manager.pet_datas.append(pd)
	pet.setup(pd)
	if pd.is_sleeping:
		pet.perform_action("rest")
	if pd.is_dead:
		_spawn_or_update_memorial(pd)

func _load_save_state() -> bool:
	var save_path = "user://save_slot.json"
	if not FileAccess.file_exists(save_path):
		return false

	var file = FileAccess.open(save_path, FileAccess.READ)
	if file == null:
		return false

	var raw = file.get_as_text()
	file.close()
	var parsed = JSON.parse_string(raw)
	if not (parsed is Dictionary):
		return false

	var save_data = parsed as Dictionary
	var saved_settings = save_data.get("settings", {})
	if saved_settings is Dictionary:
		settings = _merge_settings(saved_settings as Dictionary)
	_apply_runtime_settings()

	basket_entries.clear()
	var saved_basket = save_data.get("link_basket", [])
	if saved_basket is Array:
		for entry in saved_basket:
			if not (entry is Dictionary):
				continue
			var url = _normalize_basket_url(str((entry as Dictionary).get("url", "")))
			if url == "" or _basket_contains_url(url) or basket_entries.size() >= BASKET_MAX_LINKS:
				continue
			basket_entries.append({
				"url": url,
				"source": _basket_source_label(str((entry as Dictionary).get("source", "save"))),
				"added_at": int((entry as Dictionary).get("added_at", int(Time.get_unix_time_from_system())))
			})
	_update_basket_button_state()

	var saved_memoriam = save_data.get("in_memoriam", [])
	if saved_memoriam is Array:
		for entry in saved_memoriam:
			if entry is Dictionary:
				_restore_in_memoriam_entry(entry as Dictionary)

	var saved_pets = save_data.get("pets", [])
	if saved_pets is Array:
		for entry in saved_pets:
			if entry is Dictionary:
				_restore_pet_from_save(entry as Dictionary)

	var max_index = max(0, game_manager.pet_datas.size() - 1)
	game_manager.active_pet_index = clamp(int(save_data.get("active_pet_index", 0)), 0, max_index)
	return game_manager.get_pet_count() > 0

func _find_pending_name_index() -> int:
	for i in range(game_manager.pet_datas.size()):
		var pd = game_manager.pet_datas[i]
		if pd and pd.is_naming_pending:
			return i
	return -1

func _clear_runtime_state():
	close_all_overlays()
	_clear_held_item()
	_clear_memorial_sprites()
	basket_entries.clear()
	_update_basket_button_state()
	if feedback_label and is_instance_valid(feedback_label):
		feedback_label.queue_free()
		feedback_label = null
	if feedback_tween and feedback_tween.is_running():
		feedback_tween.kill()
		feedback_tween = null
	for pet in game_manager.pets:
		if pet:
			pet.queue_free()
	game_manager.pets.clear()
	game_manager.pet_datas.clear()
	game_manager.in_memoriam.clear()
	game_manager.forage_state.clear()
	game_manager.workout_heat.clear()
	game_manager.active_pet_index = 0
	update_environment_background()
	_on_stats_updated()
	update_add_button()

func _reload_runtime_from_save() -> bool:
	_clear_runtime_state()
	var restored = _load_save_state()
	_apply_runtime_settings()
	_apply_window_mode_layout(window_has_focus)
	_apply_runtime_visibility_state()
	update_environment_background()
	_on_stats_updated()
	update_add_button()
	return restored

func _clear_memorial_sprites():
	for key in memorial_sprites.keys():
		var sprite = memorial_sprites[key]
		if sprite and is_instance_valid(sprite):
			sprite.queue_free()
	memorial_sprites.clear()

func _memorial_key(pd: PetData) -> String:
	if pd == null:
		return ""
	return "%s:%s:%s:%s" % [pd.name, pd.animal_type, pd.gender, pd.age_at_death]

func _spawn_or_update_memorial(pd: PetData):
	if pd == null or pd.memorial_object_id == "":
		return
	if pd.memorial_expires_at > 0 and int(Time.get_unix_time_from_system()) >= pd.memorial_expires_at:
		return

	var key = _memorial_key(pd)
	if key == "":
		return

	var marker = memorial_sprites.get(key, null) as Sprite2D
	if marker == null or not is_instance_valid(marker):
		marker = Sprite2D.new()
		marker.texture = _load_ui_asset_texture("res://sprites/items/toys_b/%s.png" % pd.memorial_object_id)
		marker.z_index = Z_GROUND_CONTACT + 1
		marker.scale = Vector2(2.0, 2.0)
		marker.modulate = Color(1.0, 1.0, 1.0, 0.9)
		add_child(marker)
		memorial_sprites[key] = marker

	marker.position = pd.memorial_position
	marker.visible = marker.texture != null

func _cleanup_expired_memorials():
	if game_manager == null:
		return

	var now = int(Time.get_unix_time_from_system())
	var live_keys: Dictionary = {}
	for pd in game_manager.pet_datas:
		if pd == null or pd.memorial_object_id == "":
			continue
		var key = _memorial_key(pd)
		if key == "":
			continue
		if pd.memorial_expires_at > 0 and now >= pd.memorial_expires_at:
			var expired_marker = memorial_sprites.get(key, null)
			if expired_marker and is_instance_valid(expired_marker):
				expired_marker.queue_free()
			memorial_sprites.erase(key)
			pd.memorial_object_id = ""
			pd.memorial_expires_at = 0
			continue
		live_keys[key] = true
		_spawn_or_update_memorial(pd)

	for key in memorial_sprites.keys():
		if live_keys.has(key):
			continue
		var marker = memorial_sprites[key]
		if marker and is_instance_valid(marker):
			marker.queue_free()
		memorial_sprites.erase(key)

func _set_pet_name(pet_index: int, pet_name: String):
	var safe_name = pet_name.strip_edges()
	if safe_name == "":
		safe_name = "Wevito"
	if pet_index >= 0 and pet_index < game_manager.pet_datas.size():
		var pd = game_manager.pet_datas[pet_index]
		pd.name = safe_name
		pd.is_naming_pending = false

func _spawn_pet_for_automation(egg_color: String, pet_name: String):
	if not game_manager.can_add_pet():
		return
	add_new_pet_with_color(egg_color)
	var pet_index = game_manager.get_pet_count() - 1
	if pet_index >= 0:
		game_manager.finish_hatching(pet_index)
		_set_pet_name(pet_index, pet_name)
		if naming_overlay:
			naming_overlay.queue_free()
			naming_overlay = null
	_on_stats_updated()
	update_environment_background()

func _automation_assert(checks: Array, name: String, passed: bool, details: String = ""):
	checks.append({
		"name": name,
		"passed": passed,
		"details": details
	})

func _automation_read_json(path: String):
	if not FileAccess.file_exists(path):
		return null
	var file = FileAccess.open(path, FileAccess.READ)
	if file == null:
		return null
	var raw = file.get_as_text()
	file.close()
	if raw.strip_edges() == "":
		return null
	return JSON.parse_string(raw)

func _automation_write_report(report: Dictionary):
	var file = FileAccess.open(AUTOMATION_REPORT_PATH, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(report))
		file.close()

func _automation_species_from_scenario(scenario: String) -> String:
	var prefix = "c_phase_6_5_habitat_mirror_"
	if scenario.begins_with(prefix):
		return scenario.substr(prefix.length()).strip_edges()
	return ""

func _automation_capture_screenshot(path: String) -> bool:
	if path.strip_edges() == "":
		return false
	await get_tree().process_frame
	var texture = get_viewport().get_texture()
	if texture == null:
		return false
	var image = texture.get_image()
	if image == null:
		return false
	return image.save_png(path) == OK

func _rect2_to_dict(rect: Rect2) -> Dictionary:
	return {
		"x": rect.position.x,
		"y": rect.position.y,
		"w": rect.size.x,
		"h": rect.size.y
	}

func _control_rect_dict(ctrl: Control):
	if ctrl == null or not is_instance_valid(ctrl):
		return null
	return _rect2_to_dict(ctrl.get_global_rect())

func _collect_action_button_rects() -> Dictionary:
	var rects := {}
	if actions_bar == null:
		return rects
	for child in actions_bar.get_children():
		if child is Button:
			var btn = child as Button
			rects[str(btn.text).to_lower()] = _control_rect_dict(btn)
	return rects

func _write_runtime_state():
	var runtime_state = {
		"window_has_focus": window_has_focus,
		"overlay_ui_pinned": overlay_ui_pinned,
		"ui_visible": ui_visible,
		"monitor_roam_active": monitor_roam_active,
		"native_focus_backend": native_focus_backend,
		"native_helper_available": _native_helper_available(),
		"last_pinned_dispatch": debug_last_pinned_dispatch,
		"native_focus_state": native_focus_state,
		"window_size": {
			"x": get_window().size.x,
			"y": get_window().size.y
		},
		"window_position": {
			"x": get_window().position.x,
			"y": get_window().position.y
		},
		"basket_button": _control_rect_dict(basket_button),
		"pin_button": _control_rect_dict(pin_ui_button),
		"settings_button": _control_rect_dict(settings_button),
		"action_buttons": _collect_action_button_rects(),
		"basket_overlay_visible": basket_overlay != null and basket_overlay.visible,
		"basket_capture_button": null,
		"basket_close_button": null
	}
	if basket_overlay:
		runtime_state["basket_capture_button"] = _control_rect_dict(basket_overlay.find_child("basket_capture_button", true, false) as Control)
		runtime_state["basket_close_button"] = _control_rect_dict(basket_overlay.find_child("Close", true, false) as Control)

	var file = FileAccess.open(RUNTIME_STATE_PATH, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(runtime_state))
		file.close()

func _automation_hover_hits(target: Control) -> bool:
	if target == null:
		return false
	var rect = target.get_global_rect()
	if rect.size.x <= 0.0 or rect.size.y <= 0.0:
		return false
	Input.warp_mouse(rect.position + (rect.size * 0.5))
	await get_tree().process_frame
	var hovered = get_viewport().gui_get_hovered_control()
	if hovered == null:
		return false
	return hovered == target or hovered.is_ancestor_of(target) or target.is_ancestor_of(hovered)

func _automation_control_desc(ctrl: Control) -> String:
	if ctrl == null:
		return "<null>"
	var desc = ctrl.get_class()
	if ctrl.name != "":
		desc += ":" + String(ctrl.name)
	elif ctrl is Button and (ctrl as Button).text != "":
		desc += ":" + (ctrl as Button).text
	elif ctrl.tooltip_text != "":
		desc += ":" + ctrl.tooltip_text
	return desc

func _automation_collect_controls_at_point(root: Node, point: Vector2, out: Array):
	for i in range(root.get_child_count() - 1, -1, -1):
		var child = root.get_child(i)
		if child is Control:
			var ctrl = child as Control
			if not ctrl.visible:
				continue
			if ctrl.mouse_filter == Control.MOUSE_FILTER_IGNORE:
				_automation_collect_controls_at_point(ctrl, point, out)
				continue
			if ctrl.get_global_rect().has_point(point):
				out.append(_automation_control_desc(ctrl))
				_automation_collect_controls_at_point(ctrl, point, out)
		else:
			_automation_collect_controls_at_point(child, point, out)

func _automation_controls_at_target(target: Control) -> Array:
	var hits: Array = []
	if target == null:
		return hits
	var rect = target.get_global_rect()
	if rect.size.x <= 0.0 or rect.size.y <= 0.0:
		return hits
	_automation_collect_controls_at_point(self, rect.position + (rect.size * 0.5), hits)
	return hits

func _automation_pet_positions_within_current_bounds() -> Dictionary:
	var result = {
		"passed": true,
		"details": []
	}
	if game_manager == null:
		result["passed"] = false
		result["details"].append("missing game_manager")
		return result

	var w = float(get_window().size.x)
	var h = float(get_window().size.y)
	var slots = _get_environment_slot_rects(w, h, game_manager.get_pet_count())
	var band_top = _get_pet_roam_band_top(h)
	for i in range(game_manager.pets.size()):
		var pet = game_manager.pets[i]
		if pet == null or pet.pet_data == null:
			result["passed"] = false
			result["details"].append("pet%d missing" % i)
			continue

		var bounds = Rect2(MONITOR_ROAM_MARGIN_X, band_top, max(24.0, w - (MONITOR_ROAM_MARGIN_X * 2.0)), PET_ROAM_BAND_HEIGHT)
		if not monitor_roam_active:
			if i >= slots.size():
				result["passed"] = false
				result["details"].append("pet%d no slot" % i)
				continue
			var slot = slots[i]
			bounds = Rect2(slot.position.x + 8.0, slot.position.y, max(12.0, slot.size.x - 16.0), slot.size.y)

		var pos = pet.pet_data.position
		var target = pet.pet_data.target_position
		var pos_ok = pos.x >= bounds.position.x - 0.5 and pos.x <= bounds.end.x + 0.5
		var target_ok = target.x >= bounds.position.x - 0.5 and target.x <= bounds.end.x + 0.5
		if not pos_ok or not target_ok:
			result["passed"] = false
		result["details"].append("pet%d bounds=%s pos=%s target=%s pos_ok=%s target_ok=%s" % [
			i,
			str(bounds),
			str(pos),
			str(target),
			str(pos_ok),
			str(target_ok)
		])
	return result

func _run_automation_suite():
	if automation_running:
		return
	automation_running = true
	await get_tree().create_timer(1.8).timeout

	var checks: Array = []
	var scenario = OS.get_environment(AUTOMATION_SCENARIO_ENV)
	var native_window_checks = not str(DisplayServer.get_name()).containsn("headless") and get_window().size.x > 100 and get_window().size.y > 100
	var automation_colors = ["red", "blue", "yellow"]
	var automation_names = ["Alpha", "Bravo", "Charlie"]
	_automation_assert(checks, "boot_completed", true, scenario)
	_automation_assert(checks, "startup_overlay_or_restore", game_manager.get_pet_count() > 0 or egg_selection_overlay != null, "pets=%d" % game_manager.get_pet_count())

	while game_manager.can_add_pet() and game_manager.get_pet_count() < 3:
		var next_index = game_manager.get_pet_count()
		_spawn_pet_for_automation(automation_colors[next_index], automation_names[next_index])

	await get_tree().process_frame
	_apply_window_mode_layout(true)
	update_environment_background()
	_on_stats_updated()

	if scenario == "force_save_position_recovery":
		var recovery = _automation_pet_positions_within_current_bounds()
		_automation_assert(checks, "save_position_recovery_clamps_loaded_pets", bool(recovery.get("passed", false)), " | ".join(PackedStringArray(recovery.get("details", []))))
		var recovery_passed = true
		for check in checks:
			if not bool(check.get("passed", false)):
				recovery_passed = false
				break
		_automation_write_report({
			"scenario": scenario,
			"passed": recovery_passed,
			"checks": checks,
			"pet_count": game_manager.get_pet_count(),
			"settings": settings
		})
		get_tree().quit(0 if recovery_passed else 1)
		return

	var expected_pet_count = game_manager.get_pet_count()
	_automation_assert(checks, "three_pets_active", expected_pet_count == 3, "count=%d" % expected_pet_count)
	var hover_details: Array = []
	var settings_hits = _automation_controls_at_target(settings_button)
	var settings_hit_ok = false
	for hit in settings_hits:
		if str(hit).begins_with("Button:"):
			settings_hit_ok = true
			break
	var basket_hits = _automation_controls_at_target(basket_button)
	var basket_hit_ok = false
	for hit in basket_hits:
		if str(hit).begins_with("Button:"):
			basket_hit_ok = true
			break
	var action_hit_ok = false
	var action_hits: Array = []
	if actions_bar and actions_bar.get_child_count() > 0:
		action_hits = _automation_controls_at_target(actions_bar.get_child(0) as Control)
		for hit in action_hits:
			if str(hit).begins_with("Button:"):
				action_hit_ok = true
				break
	hover_details.append("settings=" + ",".join(PackedStringArray(settings_hits)))
	hover_details.append("basket=" + ",".join(PackedStringArray(basket_hits)))
	hover_details.append("action=" + ",".join(PackedStringArray(action_hits)))
	_automation_assert(checks, "core_ui_buttons_hit_testable", settings_hit_ok and basket_hit_ok and action_hit_ok, " | ".join(PackedStringArray(hover_details)))

	var focused_overlay_ok = true
	if native_window_checks and monitor_roam_active:
		focused_overlay_ok = (
			get_window().mouse_passthrough_polygon.size() >= 4
			and not DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_MOUSE_PASSTHROUGH)
		)
	_automation_assert(checks, "focused_overlay_input_region_enabled", focused_overlay_ok, "points=%d" % get_window().mouse_passthrough_polygon.size())

	var all_tabs_ok = true
	for tab_name in ["feed", "pet", "rest", "groom", "exercise", "medicine"]:
		_show_action_tab(tab_name)
		await get_tree().process_frame
		all_tabs_ok = all_tabs_ok and action_tab_overlay != null and current_action_tab == tab_name
	_automation_assert(checks, "all_action_tabs_open", all_tabs_ok)

	var active_pd = game_manager.get_active_pet_data()
	if active_pd:
		active_pd.hunger = 40.0
		active_pd.hydration = 35.0
		active_pd.happiness = 30.0
		active_pd.energy = 45.0
		active_pd.health = 70.0
		active_pd.cleanliness = 32.0
		active_pd.affection = 28.0
		active_pd.grooming = 24.0
		active_pd.fitness = 22.0
		active_pd.water_bowl_level = 100.0
		active_pd.conditions.clear()
	_on_stats_updated()

	var habitat_mirror_species = _automation_species_from_scenario(scenario)
	if habitat_mirror_species != "":
		var habitat_pet = game_manager.get_active_pet()
		active_pd = game_manager.get_active_pet_data()
		var screenshot_path = OS.get_environment(AUTOMATION_SCREENSHOT_PATH_ENV)
		var species_allowed = GameManager.ANIMAL_TYPES.has(habitat_mirror_species)
		var screenshot_ok = false
		var screenshot_details = "species=%s path=%s" % [habitat_mirror_species, screenshot_path]
		if habitat_pet and active_pd and species_allowed:
			settings["desktop_companion_roam"] = false
			settings["experimental_monitor_roam"] = false
			settings["ghost_mode"] = false
			overlay_ui_pinned = true
			window_has_focus = true
			_apply_runtime_settings()
			_apply_window_mode_layout(true)
			get_window().size = Vector2i(520, 480)
			close_all_overlays()
			habitat_proof_mode = true
			for extra_index in range(game_manager.get_pet_count() - 1, 0, -1):
				var extra_pet = game_manager.pets[extra_index]
				if extra_pet:
					extra_pet.queue_free()
				game_manager.pets.remove_at(extra_index)
				game_manager.pet_datas.remove_at(extra_index)
			game_manager.active_pet_index = 0
			habitat_pet = game_manager.get_active_pet()
			active_pd = game_manager.get_active_pet_data()
			active_pd.animal_type = habitat_mirror_species
			active_pd.gender = "female"
			active_pd.egg_color = "blue"
			active_pd.stage = 1
			active_pd.is_hatching = false
			active_pd.is_sleeping = false
			habitat_pet.setup(active_pd)
			habitat_pet.z_index = Z_PET_BODY
			habitat_pet.current_animation = "idle"
			habitat_pet.animation_frame = 0
			habitat_pet.update_sprite()
			if habitat_pet.sprite and habitat_pet.sprite.texture == null:
				var proof_frame = habitat_pet._load_texture("res://sprites_runtime/%s/baby/female/blue/idle_00.png" % habitat_mirror_species)
				if proof_frame:
					habitat_pet.sprite.texture = proof_frame
			if habitat_pet.sprite:
				habitat_pet.sprite.scale *= 0.45
			game_manager.set_active_pet(game_manager.active_pet_index)
			update_environment_background()
			_recall_all_pets_home(0.0, false)
			_set_all_ui_controls_visible(false)
			_set_environment_stages_visible(true)
			await get_tree().process_frame
			await get_tree().process_frame
			var pet_stage_position = get_pet_home_position_for_node(habitat_pet, "home")
			if habitat_pet.sprite and habitat_pet.sprite.texture:
				var texture_extent = habitat_pet.sprite.texture.get_size() * habitat_pet.sprite.scale * 0.5
				pet_stage_position.y -= texture_extent.y
				var proof_slots = _get_environment_slot_rects(float(get_window().size.x), float(get_window().size.y), 1)
				if not proof_slots.is_empty():
					var proof_slot = proof_slots[0]
					pet_stage_position.x = clamp(pet_stage_position.x, proof_slot.position.x + texture_extent.x, proof_slot.end.x - texture_extent.x)
					pet_stage_position.y = clamp(pet_stage_position.y, proof_slot.position.y + texture_extent.y, proof_slot.end.y - texture_extent.y)
			habitat_pet.position = pet_stage_position
			habitat_pet.pet_data.position = habitat_pet.position
			habitat_pet.pet_data.target_position = habitat_pet.position
			await get_tree().process_frame
			screenshot_ok = await _automation_capture_screenshot(screenshot_path)
			var texture_size = Vector2.ZERO
			var texture_path = ""
			if habitat_pet.sprite and habitat_pet.sprite.texture:
				texture_size = habitat_pet.sprite.texture.get_size()
				texture_path = habitat_pet.sprite.texture.get_path()
			screenshot_details += " saved=%s position=%s z=%d visible=%s texture=%s size=%s" % [
				str(screenshot_ok),
				str(habitat_pet.position),
				habitat_pet.z_index,
				str(habitat_pet.visible),
				texture_path,
				str(texture_size)
			]
		else:
			screenshot_details += " allowed=%s active_pet=%s" % [str(species_allowed), str(habitat_pet != null)]
		_automation_assert(checks, "c_phase_6_5_habitat_mirror_screenshot", screenshot_ok, screenshot_details)
		var habitat_report = {
			"scenario": scenario,
			"passed": screenshot_ok,
			"checks": checks,
			"species": habitat_mirror_species,
			"screenshot_path": screenshot_path
		}
		_automation_write_report(habitat_report)
		get_tree().quit(0 if screenshot_ok else 1)
		return

	if scenario == "force_low_hydration_drink":
		var drink_pet = game_manager.get_active_pet()
		active_pd = game_manager.get_active_pet_data()
		var auto_drink_ok = false
		var auto_drink_details = "no active pet"
		if drink_pet and active_pd:
			active_pd.animal_type = "goose"
			active_pd.gender = "female"
			active_pd.egg_color = "blue"
			active_pd.stage = 1
			active_pd.hydration = 8.0
			active_pd.water_bowl_level = 100.0
			active_pd.is_sleeping = false
			drink_pet.setup(active_pd)
			var hydration_before = active_pd.hydration
			var triggered = game_manager.force_auto_drink_for_test(game_manager.active_pet_index)
			await get_tree().process_frame
			auto_drink_ok = triggered and active_pd.hydration > hydration_before and (drink_pet.current_animation == "drink" or drink_pet.current_animation == "eat")
			auto_drink_details = "triggered=%s hydration %.1f->%.1f animation=%s" % [
				str(triggered),
				hydration_before,
				active_pd.hydration,
				drink_pet.current_animation
			]
		_automation_assert(checks, "forced_low_hydration_plays_drink_animation", auto_drink_ok, auto_drink_details)

	if scenario == "force_fetch_sequence":
		var fetch_pet = game_manager.get_active_pet()
		active_pd = game_manager.get_active_pet_data()
		var fetch_ok = false
		var fetch_details = "no active pet"
		if fetch_pet and active_pd:
			active_pd.animal_type = "goose"
			active_pd.gender = "female"
			active_pd.egg_color = "blue"
			active_pd.stage = 1
			active_pd.affection = 100.0
			fetch_pet.setup(active_pd)
			var ball_target = fetch_pet.position + Vector2(80, 0)
			var result = game_manager.perform_fetch_sequence(ball_target)
			await get_tree().create_timer(7.4).timeout
			fetch_ok = bool(result.get("accepted", false)) and not fetch_pet.is_fetch_sequence_active() and fetch_pet.current_animation in ["happy", "idle"]
			fetch_details = "accepted=%s active=%s animation=%s" % [
				str(result.get("accepted", false)),
				str(fetch_pet.is_fetch_sequence_active()),
				fetch_pet.current_animation
			]
		_automation_assert(checks, "forced_fetch_sequence_completes", fetch_ok, fetch_details)

	active_pd = game_manager.get_active_pet_data()
	if active_pd:
		active_pd.hunger = 40.0
		active_pd.hydration = 35.0
		active_pd.happiness = 30.0
		active_pd.energy = 45.0
		active_pd.health = 70.0
		active_pd.cleanliness = 32.0
		active_pd.affection = 28.0
		active_pd.grooming = 24.0
		active_pd.fitness = 22.0
		active_pd.water_bowl_level = 100.0
		active_pd.conditions.clear()
	_on_stats_updated()

	var slots = _get_environment_slot_rects(float(get_window().size.x), float(get_window().size.y), game_manager.get_pet_count())
	_show_action_tab("feed")
	await get_tree().process_frame
	var recall_ok = true
	for i in range(game_manager.pets.size()):
		var pet = game_manager.pets[i]
		if pet == null or i >= slots.size():
			recall_ok = false
			continue
		var expected_x = slots[i].position.x + (slots[i].size.x * 0.5)
		if abs(pet.pet_data.target_position.x - expected_x) > 1.0:
			recall_ok = false
	_automation_assert(checks, "action_tab_recalls_all_pets", recall_ok)
	_do_action_from_tab("feed_small")
	await get_tree().process_frame
	var action_tab_refresh_ok = action_tab_overlay != null and current_action_tab == "feed"
	_show_action_tab("pet")
	await get_tree().process_frame
	action_tab_refresh_ok = action_tab_refresh_ok and action_tab_overlay != null and current_action_tab == "pet"
	_automation_assert(checks, "action_tabs_remain_switchable_after_action", action_tab_refresh_ok)

	var action_effects_ok = true
	active_pd = game_manager.get_active_pet_data()
	if active_pd:
		var hunger_before = active_pd.hunger
		_show_action_tab("feed")
		await get_tree().process_frame
		_do_action_from_tab("feed_full")
		await get_tree().process_frame
		action_effects_ok = action_effects_ok and active_pd.hunger > hunger_before and current_action_tab == "feed"

		var affection_before = active_pd.affection
		_show_action_tab("pet")
		await get_tree().process_frame
		_do_action_from_tab("pet_pat")
		await get_tree().process_frame
		action_effects_ok = action_effects_ok and active_pd.affection > affection_before and current_action_tab == "pet"

		var sleeping_before = active_pd.is_sleeping
		_show_action_tab("rest")
		await get_tree().process_frame
		_do_action_from_tab("rest_toggle")
		await get_tree().process_frame
		action_effects_ok = action_effects_ok and active_pd.is_sleeping != sleeping_before and current_action_tab == "rest"
		_do_action_from_tab("rest_toggle")
		await get_tree().process_frame
		action_effects_ok = action_effects_ok and active_pd.is_sleeping == sleeping_before and current_action_tab == "rest"

		var grooming_before = active_pd.grooming
		_show_action_tab("groom")
		await get_tree().process_frame
		_do_action_from_tab("groom_haircut")
		await get_tree().process_frame
		action_effects_ok = action_effects_ok and active_pd.grooming > grooming_before and current_action_tab == "groom"

		var fitness_before = active_pd.fitness
		_show_action_tab("exercise")
		await get_tree().process_frame
		_do_action_from_tab("exercise_workout")
		await get_tree().process_frame
		action_effects_ok = action_effects_ok and active_pd.fitness > fitness_before and current_action_tab == "exercise"
	_automation_assert(checks, "core_actions_update_pet_state", action_effects_ok)

	var medicine_flow_ok = true
	active_pd = game_manager.get_active_pet_data()
	if active_pd:
		active_pd.conditions["respiratoryProblems"] = 1
		medicine_inventory["antibiotics"] = 1
		_show_action_tab("medicine")
		await get_tree().process_frame
		_show_medicine_collect()
		await get_tree().process_frame
		medicine_flow_ok = medicine_flow_ok and action_menu_overlay != null and guidebook_page == 2
		_on_hold_medicine("antibiotics")
		await get_tree().process_frame
		medicine_flow_ok = medicine_flow_ok and held_item.get("type", "") == "medicine"
		var active_pet = game_manager.get_active_pet()
		if active_pet:
			_handle_held_item_click(active_pet.position)
			await get_tree().process_frame
			medicine_flow_ok = medicine_flow_ok and held_item.get("type", "") == "" and medicine_inventory.get("antibiotics", 0) == 0 and not active_pd.conditions.has("respiratoryProblems")
		else:
			medicine_flow_ok = false
	_automation_assert(checks, "medicine_tab_holds_and_applies_item", medicine_flow_ok)

	var basket_flow_ok = true
	var basket_details: Array[String] = []
	basket_entries.clear()
	_update_basket_button_state()
	var automation_basket_url = "https://example.com/alpha?src=wevito-automation"
	var clipboard_roundtrip_supported = not str(DisplayServer.get_name()).containsn("headless")
	if clipboard_roundtrip_supported:
		DisplayServer.clipboard_set(automation_basket_url)
		basket_flow_ok = basket_flow_ok and _capture_clipboard_link(false)
	else:
		basket_flow_ok = basket_flow_ok and _basket_add_url(automation_basket_url, "automation", false)
	basket_details.append("after_clipboard=%d" % basket_entries.size())
	basket_flow_ok = basket_flow_ok and basket_entries.size() == 1
	basket_flow_ok = basket_flow_ok and not _basket_add_url(automation_basket_url, "automation", false)
	basket_details.append("after_duplicate=%d" % basket_entries.size())

	var automation_drop_path = ProjectSettings.globalize_path("user://automation_drop.url")
	var drop_file = FileAccess.open(automation_drop_path, FileAccess.WRITE)
	if drop_file:
		drop_file.store_string("[InternetShortcut]\nURL=https://example.com/drop-from-file\n")
		drop_file.close()
		_on_window_files_dropped(PackedStringArray([automation_drop_path]))
		await get_tree().process_frame
	else:
		basket_flow_ok = false
	basket_details.append("after_drop=%d" % basket_entries.size())
	basket_flow_ok = basket_flow_ok and basket_entries.size() == 2

	_show_basket_overlay()
	await get_tree().process_frame
	basket_flow_ok = basket_flow_ok and basket_overlay != null
	_copy_basket_entry_to_clipboard(0, false)
	await get_tree().process_frame
	if clipboard_roundtrip_supported:
		basket_flow_ok = basket_flow_ok and DisplayServer.clipboard_get() == str(basket_entries[0].get("url", ""))
	_remove_basket_entry(1, false)
	await get_tree().process_frame
	basket_flow_ok = basket_flow_ok and basket_entries.size() == 1
	basket_details.append("after_remove=%d" % basket_entries.size())
	_close_basket_overlay()
	if FileAccess.file_exists(automation_drop_path):
		DirAccess.remove_absolute(automation_drop_path)
	_automation_assert(checks, "basket_capture_drop_retrieve_flow", basket_flow_ok, " | ".join(PackedStringArray(basket_details)))

	for i in range(game_manager.pets.size()):
		var pet = game_manager.pets[i]
		if pet == null or i >= slots.size():
			continue
		var home_x = slots[i].position.x + (slots[i].size.x * 0.5)
		pet.position = Vector2(home_x, pet.position.y)
		pet.pet_data.position = pet.position
		pet.pet_data.target_position = pet.position
		pet.pet_state = Pet.PetState.WANDERING
	_show_action_tab("pet")
	await get_tree().process_frame
	_close_action_tab()
	await get_tree().create_timer(0.05).timeout
	var hold_ok = true
	var hold_resume_ok = true
	for pet in game_manager.pets:
		if pet == null or float(pet.get("_home_lock_timer")) < 1.8:
			hold_ok = false
		if pet == null or not bool(pet.get("_resume_wandering_after_home_lock")):
			hold_resume_ok = false
	_automation_assert(checks, "action_tab_close_starts_home_hold", hold_ok)
	_automation_assert(checks, "action_tab_close_marks_roam_resume", hold_resume_ok)
	await get_tree().create_timer(2.15).timeout
	var roam_resume_ok = false
	var roam_resume_details: Array[String] = []
	for i in range(game_manager.pets.size()):
		var pet = game_manager.pets[i]
		if pet == null or i >= slots.size():
			continue
		var home_x = slots[i].position.x + (slots[i].size.x * 0.5)
		roam_resume_details.append("pet%d state=%s target=%.1f home=%.1f lock=%.2f resume=%s wander=%s" % [
			i,
			str(int(pet.pet_state)),
			pet.pet_data.target_position.x,
			home_x,
			float(pet.get("_home_lock_timer")),
			str(bool(pet.get("_resume_wandering_after_home_lock"))),
			str(bool(pet.pet_data.is_wandering))
		])
		if abs(pet.pet_data.target_position.x - home_x) > 6.0:
			roam_resume_ok = true
			break
	_automation_assert(checks, "pets_resume_roam_after_action_hold", roam_resume_ok, " | ".join(PackedStringArray(roam_resume_details)))

	_handle_focus_change(false)
	await get_tree().process_frame
	_automation_assert(checks, "focus_out_hides_hud", (not ui_visible) and stats_panel != null and not stats_panel.visible)
	var passive_overlay_ok = true if not native_window_checks else DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_MOUSE_PASSTHROUGH)
	_automation_assert(checks, "passive_overlay_passthrough_enabled", passive_overlay_ok)
	_set_overlay_ui_pin(true, false)
	await get_tree().process_frame
	var pinned_overlay_ok = true if not native_window_checks else (
		ui_visible
		and stats_panel != null
		and stats_panel.visible
		and not DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_MOUSE_PASSTHROUGH)
	)
	_automation_assert(checks, "pinned_overlay_keeps_ui_visible", pinned_overlay_ok)
	_set_overlay_ui_pin(false, false)
	await get_tree().process_frame
	var released_overlay_ok = (not ui_visible)
	if native_window_checks:
		released_overlay_ok = released_overlay_ok and DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_MOUSE_PASSTHROUGH)
	_automation_assert(checks, "unpinned_overlay_restores_passive_mode", released_overlay_ok)
	var command_file = FileAccess.open(OVERLAY_COMMAND_PATH, FileAccess.WRITE)
	if command_file:
		command_file.store_string(JSON.stringify({"command": "pin_overlay_ui"}))
		command_file.close()
	await get_tree().create_timer(OVERLAY_COMMAND_POLL_INTERVAL_SEC + 0.05).timeout
	_automation_assert(checks, "external_pin_command_works", overlay_ui_pinned and ui_visible)
	command_file = FileAccess.open(OVERLAY_COMMAND_PATH, FileAccess.WRITE)
	if command_file:
		command_file.store_string(JSON.stringify({"command": "release_overlay_ui"}))
		command_file.close()
	await get_tree().create_timer(OVERLAY_COMMAND_POLL_INTERVAL_SEC + 0.05).timeout
	_automation_assert(checks, "external_release_command_works", (not overlay_ui_pinned) and (not ui_visible))
	_handle_focus_change(true)
	await get_tree().process_frame
	var focus_in_ok = ui_visible and stats_panel != null and stats_panel.visible
	if native_window_checks:
		focus_in_ok = focus_in_ok and not DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_MOUSE_PASSTHROUGH)
	_automation_assert(checks, "focus_in_restores_hud", focus_in_ok)

	settings["desktop_companion_roam"] = true
	settings["experimental_monitor_roam"] = true
	settings["ghost_mode"] = true
	_apply_runtime_settings()
	_handle_focus_change(false)
	await get_tree().process_frame
	var roam_layout_ok = true if not native_window_checks else monitor_roam_active and get_window().size.x > WINDOW_FOCUSED_SIZE.x and get_window().size.y > WINDOW_FOCUSED_SIZE.y
	_automation_assert(checks, "horizontal_monitor_roam_layout", roam_layout_ok, "size=%s" % str(get_window().size))
	_handle_focus_change(true)
	await get_tree().process_frame
	_do_auto_save()
	var save_data = _automation_read_json(AUTOMATION_SAVE_PATH)
	var save_ok = save_data is Dictionary and (save_data as Dictionary).get("pets", []).size() == game_manager.get_pet_count() and (save_data as Dictionary).get("link_basket", []).size() == basket_entries.size()
	_automation_assert(checks, "save_writes_current_pet_count", save_ok)
	var saved_before_reset = JSON.stringify(save_data) if save_data != null else ""

	_reset_save()
	await get_tree().create_timer(0.05).timeout
	var reset_data = _automation_read_json(AUTOMATION_SAVE_PATH)
	var reset_ok = reset_data is Dictionary and (reset_data as Dictionary).get("pets", []).size() == 0 and (reset_data as Dictionary).get("link_basket", []).size() == 0 and bool((reset_data as Dictionary).get("settings", {}).get("ghost_mode", false))
	_automation_assert(checks, "reset_clears_pets_preserves_settings", reset_ok)

	if saved_before_reset != "":
		var restore_file = FileAccess.open(AUTOMATION_SAVE_PATH, FileAccess.WRITE)
		if restore_file:
			restore_file.store_string(saved_before_reset)
			restore_file.close()
	var restored = _reload_runtime_from_save()
	await get_tree().process_frame
	_automation_assert(checks, "reload_restores_previous_state", restored and game_manager.get_pet_count() == expected_pet_count and basket_entries.size() == 1 and bool(settings.get("desktop_companion_roam", true)))

	var passed = true
	for check in checks:
		if not bool(check.get("passed", false)):
			passed = false
			break

	var report = {
		"scenario": scenario,
		"passed": passed,
		"checks": checks,
		"pet_count": game_manager.get_pet_count(),
		"settings": settings
	}
	_automation_write_report(report)
	get_tree().quit(0 if passed else 1)

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
	if basket_overlay:
		basket_overlay.queue_free()
		basket_overlay = null
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
	card.position = _modal_card_target_position(card.size)

func _create_priority_modal(card_size: Vector2, backdrop_alpha: float = 0.6) -> Dictionary:
	var overlay = Control.new()
	overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay.mouse_filter = Control.MOUSE_FILTER_STOP
	overlay.z_index = Z_MODAL_OVERLAY
	add_child(overlay)

	var overlay_bg = ColorRect.new()
	overlay_bg.color = Color(COLOR_BACKDROP.r, COLOR_BACKDROP.g, COLOR_BACKDROP.b, backdrop_alpha)
	overlay_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay.add_child(overlay_bg)

	var card = Panel.new()
	card.name = "modal_card"
	card.size = card_size
	card.position = _modal_card_target_position(card_size)
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
		var egg_btn = _create_egg_choice_button(egg_color)
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
	if _monitor_roam_requested():
		_attempt_monitor_roam_layout()
	else:
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
	return bool(settings.get("desktop_companion_roam", true))

func _attempt_monitor_roam_layout() -> bool:
	var screen = DisplayServer.window_get_current_screen()
	var usable_rect = DisplayServer.screen_get_usable_rect(screen)
	get_window().size = usable_rect.size
	get_window().position = usable_rect.position
	var actual = get_window().size
	return actual.x >= int(usable_rect.size.x * 0.9) and actual.y >= int(usable_rect.size.y * 0.9)

func _get_overlay_panel_rect(window_width: float, window_height: float, pet_count: int) -> Rect2:
	if not monitor_roam_active or window_width < 900.0:
		return Rect2(Vector2.ZERO, Vector2(window_width, window_height))

	var count = max(1, pet_count)
	var desktop_stage_w = clamp(window_width * 0.06, 120.0, 180.0)
	var desktop_total_w = (desktop_stage_w * count) + (DESKTOP_STAGE_GAP * max(0, count - 1))
	var panel_w = min(window_width, max(float(WINDOW_FOCUSED_SIZE.x), desktop_total_w + (HUD_PADDING_X * 2.0) + 16.0))
	var panel_h = min(window_height, float(WINDOW_FOCUSED_SIZE.y))
	var panel_pos = Vector2(
		max(0.0, window_width - panel_w - float(WINDOW_MARGIN.x)),
		max(0.0, window_height - panel_h - float(WINDOW_MARGIN.y))
	)
	return Rect2(panel_pos, Vector2(panel_w, panel_h))

func _get_pet_roam_band_top(window_height: float) -> float:
	return max(0.0, window_height - PET_ROAM_BAND_HEIGHT)

func _modal_card_target_position(card_size: Vector2) -> Vector2:
	var win_size = Vector2(float(get_window().size.x), float(get_window().size.y))
	if monitor_roam_active and win_size.x >= 900.0:
		var count = max(1, game_manager.get_pet_count()) if game_manager else 1
		var panel = _get_overlay_panel_rect(win_size.x, win_size.y, count)
		return Vector2(
			clamp(panel.position.x + panel.size.x - card_size.x - 8.0, 8.0, max(8.0, win_size.x - card_size.x - 8.0)),
			clamp(panel.position.y + panel.size.y - card_size.y - 8.0, 8.0, max(8.0, win_size.y - card_size.y - 8.0))
		)
	return Vector2(
		max(8.0, (win_size.x - card_size.x) * 0.5),
		max(8.0, (win_size.y - card_size.y) * 0.5)
	)

func _build_active_overlay_input_polygon() -> PackedVector2Array:
	var window_size = get_window().size
	var w = float(window_size.x)
	var h = float(window_size.y)
	if w <= 0.0 or h <= 0.0:
		return PackedVector2Array()
	if not monitor_roam_active or w < 900.0:
		return PackedVector2Array([
			Vector2.ZERO,
			Vector2(w, 0.0),
			Vector2(w, h),
			Vector2(0.0, h)
		])

	var count = max(1, game_manager.get_pet_count()) if game_manager else 1
	var panel = _get_overlay_panel_rect(w, h, count)
	var band_top = _get_pet_roam_band_top(h)
	var panel_left = clamp(panel.position.x, 0.0, w)
	var panel_top = clamp(panel.position.y, 0.0, h)
	return PackedVector2Array([
		Vector2(0.0, band_top),
		Vector2(panel_left, band_top),
		Vector2(panel_left, panel_top),
		Vector2(w, panel_top),
		Vector2(w, h),
		Vector2(0.0, h)
	])

func _apply_window_mode_layout(focused: bool):
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	var should_monitor_roam = _monitor_roam_requested()
	monitor_roam_active = should_monitor_roam and _attempt_monitor_roam_layout()
	if not monitor_roam_active:
		if focused or overlay_ui_pinned:
			get_window().size = WINDOW_FOCUSED_SIZE
			_pin_window_bottom_right()
		else:
			get_window().size = WINDOW_UNFOCUSED_SIZE
			_pin_window_bottom_right()
	_apply_responsive_ui_layout(get_window().size.y)
	_apply_environment_layout(get_window().size.y)
	_apply_pet_bounds_for_mode(focused or overlay_ui_pinned)
	_update_focus_backdrop()

func _apply_responsive_ui_layout(window_height: int):
	var w = float(get_window().size.x)
	var h = float(window_height)
	var count = max(1, game_manager.get_pet_count()) if game_manager else 1
	var panel = _get_overlay_panel_rect(w, h, count)
	var panel_x = panel.position.x
	var panel_y = panel.position.y
	var panel_w = panel.size.x
	var panel_h = panel.size.y
	var top_zone_h = panel_h * TOP_ZONE_PCT
	var hud_top = panel_y + top_zone_h
	var hud_h = panel_h * HUD_ZONE_PCT
	var hud_bottom = hud_top + hud_h
	var content_w = max(120.0, panel_w - (HUD_PADDING_X * 2.0))

	var icon_size = Vector2(28, 24)
	var basket_size = Vector2(40, 24)
	var icon_gap = 4.0
	var top_row_y = panel_y + 6.0
	var second_row_y = panel_y + 30.0
	var right_x = panel_x + panel_w - HUD_PADDING_X - icon_size.x
	var top_row_secondary_x = right_x - basket_size.x - icon_gap
	var right_row4_start = right_x - ((icon_size.x + icon_gap) * 3.0)
	var center_left = panel_x + HUD_PADDING_X + icon_size.x + 8.0
	var center_right = right_row4_start - 8.0
	var center_w = max(96.0, center_right - center_left)
	var center_mid = center_left + (center_w * 0.5)
	var nav_y = second_row_y + 1.0
	var indicators_x = center_mid + 16.0
	var identity_y = hud_top + IDENTITY_ROW_Y_OFFSET
	var portrait_size = 40.0
	var portrait_gap = 8.0
	var identity_left = panel_x + HUD_PADDING_X + portrait_size + portrait_gap
	var identity_w = max(140.0, content_w - portrait_size - portrait_gap)
	var name_w = max(88.0, identity_w * IDENTITY_NAME_RATIO)
	var age_x = identity_left + name_w + IDENTITY_GENDER_WIDTH + (IDENTITY_INNER_GAP * 2.0)
	var age_w = max(72.0, identity_w - (name_w + IDENTITY_GENDER_WIDTH + (IDENTITY_INNER_GAP * 2.0)))

	if hud_hit_surface:
		hud_hit_surface.position = Vector2(panel_x, panel_y)
		hud_hit_surface.size = Vector2(panel_w, panel_h)

	if doctor_button:
		doctor_button.position = Vector2(panel_x + HUD_PADDING_X, top_row_y)
	if minimize_button:
		minimize_button.position = Vector2(panel_x + HUD_PADDING_X, second_row_y)
	if add_pet_button:
		add_pet_button.position = Vector2(right_x, top_row_y)
	if basket_button:
		basket_button.position = Vector2(top_row_secondary_x, top_row_y)
	if pin_ui_button:
		pin_ui_button.position = Vector2(right_row4_start, second_row_y)
	if ghost_button:
		ghost_button.position = Vector2(right_row4_start + icon_size.x + icon_gap, second_row_y)
	if memoriam_button:
		memoriam_button.position = Vector2(right_row4_start + ((icon_size.x + icon_gap) * 2.0), second_row_y)
	if settings_button:
		settings_button.position = Vector2(right_x, second_row_y)

	if title_label:
		title_label.position = Vector2(center_left, panel_y + 8)
		title_label.size = Vector2(center_w, 16)

	if pet_indicators:
		pet_indicators.position = Vector2(indicators_x, nav_y + 2.0)
		pet_indicators.size = Vector2(max(40.0, center_right - indicators_x), 14)

	if pet_portrait:
		pet_portrait.position = Vector2(panel_x + HUD_PADDING_X, identity_y - 10.0)
		pet_portrait.size = Vector2(portrait_size, portrait_size)

	if pet_name_label:
		pet_name_label.position = Vector2(identity_left, identity_y)
		pet_name_label.size = Vector2(name_w, 14)

	if pet_gender_label:
		pet_gender_label.position = Vector2(identity_left + name_w + IDENTITY_INNER_GAP, identity_y)
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
		stats_panel.position = Vector2(identity_left, stats_top)
		stats_panel.size = Vector2(max(120.0, content_w - portrait_size - portrait_gap), stats_h)

	if actions_bar:
		actions_bar.position = Vector2(panel_x + HUD_PADDING_X, hud_bottom - HUD_ACTIONS_HEIGHT)
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
		stage_bg.z_index = Z_BACKDROP
		add_child(stage_bg)

		var stage_mid = ColorRect.new()
		stage_mid.color = Color(0.18, 0.2, 0.21)
		stage_mid.mouse_filter = Control.MOUSE_FILTER_IGNORE
		stage_mid.z_index = Z_FAR_PROP
		add_child(stage_mid)

		var stage_ground = ColorRect.new()
		stage_ground.color = Color(0.06, 0.07, 0.08)
		stage_ground.mouse_filter = Control.MOUSE_FILTER_IGNORE
		stage_ground.z_index = Z_GROUND_CONTACT
		add_child(stage_ground)

		var stage_primary_shadow = Polygon2D.new()
		stage_primary_shadow.color = Color(0.0, 0.0, 0.0, 0.18)
		stage_primary_shadow.z_index = Z_PET_SHADOW
		add_child(stage_primary_shadow)

		var stage_pet_shadow = Polygon2D.new()
		stage_pet_shadow.color = Color(0.0, 0.0, 0.0, 0.24)
		stage_pet_shadow.z_index = Z_PET_SHADOW
		add_child(stage_pet_shadow)

		var stage_decor = TextureRect.new()
		stage_decor.mouse_filter = Control.MOUSE_FILTER_IGNORE
		stage_decor.z_index = Z_FAR_PROP
		stage_decor.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		stage_decor.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		add_child(stage_decor)

		var stage_accent = TextureRect.new()
		stage_accent.mouse_filter = Control.MOUSE_FILTER_IGNORE
		stage_accent.z_index = Z_FAR_PROP
		stage_accent.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		stage_accent.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		add_child(stage_accent)

		var stage_frame = Panel.new()
		stage_frame.mouse_filter = Control.MOUSE_FILTER_IGNORE
		stage_frame.z_index = Z_NEAR_OCCLUDER
		add_child(stage_frame)

		environment_stages.append({"bg": stage_bg, "mid": stage_mid, "ground": stage_ground, "primary_shadow": stage_primary_shadow, "pet_shadow": stage_pet_shadow, "decor": stage_decor, "accent": stage_accent, "frame": stage_frame})

	while environment_stages.size() > target:
		var stage = environment_stages.pop_back()
		for key in STAGE_NODE_KEYS:
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
	if habitat_proof_mode:
		var viewport_size = get_viewport().get_visible_rect().size if get_viewport() else Vector2(window_width, window_height)
		window_width = viewport_size.x
		window_height = viewport_size.y
		var proof_w = clamp(window_width * 0.78, 248.0, 420.0)
		var proof_h = clamp(window_height * 0.62, 220.0, 300.0)
		slots.append(Rect2(Vector2((window_width - proof_w) * 0.5, (window_height - proof_h) * 0.5), Vector2(proof_w, proof_h)))
		return slots
	if monitor_roam_active and window_width >= 900.0:
		var panel = _get_overlay_panel_rect(window_width, window_height, c)
		var desktop_stage_w = clamp(window_width * 0.06, 120.0, 180.0)
		var desktop_stage_h = clamp(window_height * 0.16, 110.0, 180.0)
		var desktop_total_w = (desktop_stage_w * c) + (DESKTOP_STAGE_GAP * max(0, c - 1))
		var desktop_start_x = panel.position.x + max(0.0, (panel.size.x - desktop_total_w) * 0.5)
		var desktop_stage_top = max(panel.position.y + (panel.size.y * 0.56), panel.end.y - desktop_stage_h - 18.0)
		for i in range(c):
			slots.append(Rect2(
				Vector2(desktop_start_x + (i * (desktop_stage_w + DESKTOP_STAGE_GAP)), desktop_stage_top),
				Vector2(desktop_stage_w, desktop_stage_h)
			))
		return slots

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
	if monitor_roam_active:
		var w = float(get_window().size.x)
		var h = float(get_window().size.y)
		var band_top = _get_pet_roam_band_top(h)
		for pet in game_manager.pets:
			if pet and pet.has_method("set_wander_bounds"):
				pet.set_wander_bounds(Rect2(MONITOR_ROAM_MARGIN_X, band_top, max(24.0, w - (MONITOR_ROAM_MARGIN_X * 2.0)), PET_ROAM_BAND_HEIGHT))
	elif focused:
		var slots = _get_environment_slot_rects(float(get_window().size.x), float(get_window().size.y), game_manager.get_pet_count())
		for i in range(game_manager.pets.size()):
			var pet = game_manager.pets[i]
			if pet == null:
				continue
			if i < slots.size() and pet.has_method("set_wander_bounds"):
				var s = slots[i]
				pet.set_wander_bounds(Rect2(s.position.x + 8.0, s.position.y, max(12.0, s.size.x - 16.0), s.size.y))
	else:
		var slots = _get_environment_slot_rects(float(get_window().size.x), float(get_window().size.y), game_manager.get_pet_count())
		for i in range(game_manager.pets.size()):
			var pet = game_manager.pets[i]
			if pet == null:
				continue
			if i < slots.size() and pet.has_method("set_wander_bounds"):
				var s = slots[i]
				pet.set_wander_bounds(Rect2(s.position.x + 8.0, s.position.y, max(12.0, s.size.x - 16.0), s.size.y))

func _set_environment_stages_visible(should_show: bool):
	for stage in environment_stages:
		for key in STAGE_NODE_KEYS:
			var node = stage.get(key)
			if node:
				node.visible = should_show

func _is_environment_stage_control(ctrl: Control) -> bool:
	if focus_backdrop == ctrl:
		return true
	if celestial_sprite == ctrl:
		return true
	for stage in environment_stages:
		for key in ["bg", "mid", "ground", "decor", "accent", "frame"]:
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

func _recall_all_pets_home(hold_seconds: float = 0.0, resume_wandering_after_hold: bool = false):
	if game_manager == null:
		return
	var slots = _get_environment_slot_rects(float(get_window().size.x), float(get_window().size.y), game_manager.get_pet_count())
	for i in range(game_manager.pets.size()):
		var pet = game_manager.pets[i]
		if pet == null:
			continue
		if i < slots.size() and pet.has_method("move_to_home"):
			var center_x = slots[i].position.x + (slots[i].size.x * 0.5)
			pet.move_to_home(center_x, hold_seconds, resume_wandering_after_hold)

func get_pet_home_position_for_node(pet_node: Pet, action_family: String = "home") -> Vector2:
	var floor_y = float(get_window().size.y) - PET_FLOOR_INSET
	if game_manager == null or pet_node == null:
		return Vector2(float(get_window().size.x) * 0.5, floor_y)
	var pet_index = game_manager.pets.find(pet_node)
	if pet_index < 0:
		return Vector2(float(get_window().size.x) * 0.5, floor_y)
	var slots = _get_environment_slot_rects(float(get_window().size.x), float(get_window().size.y), game_manager.get_pet_count())
	if pet_index >= slots.size():
		return Vector2(float(get_window().size.x) * 0.5, floor_y)
	var slot = slots[pet_index]
	var anchor = Vector2(0.5, 0.82)
	if pet_node.pet_data and game_manager.has_method("get_habitat_anchor"):
		anchor = game_manager.get_habitat_anchor(pet_node.pet_data.animal_type, action_family)
	return Vector2(
		slot.position.x + (slot.size.x * clamp(anchor.x, 0.05, 0.95)),
		slot.position.y + (slot.size.y * clamp(anchor.y, 0.15, 0.95))
	)

func _ellipse_polygon(radius: Vector2, segments: int = 28) -> PackedVector2Array:
	var points := PackedVector2Array()
	var safe_segments = max(8, segments)
	for i in range(safe_segments):
		var angle = (TAU * float(i)) / float(safe_segments)
		points.append(Vector2(cos(angle) * radius.x, sin(angle) * radius.y))
	return points

func _stage_anchor_position(slot: Rect2, animal_type: String, action_family: String = "home") -> Vector2:
	var anchor = Vector2(0.5, 0.82)
	if game_manager and game_manager.has_method("get_habitat_anchor"):
		anchor = game_manager.get_habitat_anchor(animal_type, action_family)
	return Vector2(
		slot.position.x + (slot.size.x * clamp(anchor.x, 0.05, 0.95)),
		slot.position.y + (slot.size.y * clamp(anchor.y, 0.15, 0.95))
	)

func _stage_manifest_slot_rect(stage_slot: Rect2, animal_type: String, slot_id: String) -> Rect2:
	if game_manager == null or not game_manager.has_method("get_habitat_loadout"):
		return Rect2(Vector2.ZERO, Vector2.ZERO)
	var loadout = game_manager.get_habitat_loadout(animal_type)
	var slots = loadout.get("slots", [])
	if not (slots is Array):
		return Rect2(Vector2.ZERO, Vector2.ZERO)
	for manifest_slot in slots:
		if not (manifest_slot is Dictionary) or str(manifest_slot.get("slotId", "")) != slot_id:
			continue
		var rect = manifest_slot.get("defaultRect", {})
		if not (rect is Dictionary):
			return Rect2(Vector2.ZERO, Vector2.ZERO)
		var sx = stage_slot.size.x / HABITAT_MANIFEST_STAGE_SIZE.x
		var sy = stage_slot.size.y / HABITAT_MANIFEST_STAGE_SIZE.y
		return Rect2(
			Vector2(stage_slot.position.x + (float(rect.get("left", 0.0)) * sx), stage_slot.position.y + (float(rect.get("top", 0.0)) * sy)),
			Vector2(float(rect.get("width", 0.0)) * sx, float(rect.get("height", 0.0)) * sy)
		)
	return Rect2(Vector2.ZERO, Vector2.ZERO)

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
		var stage_decor = stage.get("decor") as TextureRect
		var stage_accent = stage.get("accent") as TextureRect
		var stage_frame = stage.get("frame") as Panel
		var primary_shadow = stage.get("primary_shadow") as Polygon2D
		var pet_shadow = stage.get("pet_shadow") as Polygon2D
		var slot_pet_data = game_manager.pet_datas[i] if game_manager and i < game_manager.pet_datas.size() else null
		var animal_type = slot_pet_data.animal_type if slot_pet_data else "goose"

		if stage_bg:
			stage_bg.position = Vector2(x, stage_top)
			stage_bg.size = Vector2(stage_w, stage_h)
		if stage_mid:
			stage_mid.position = Vector2(x, mid_top)
			stage_mid.size = Vector2(stage_w, max(0.0, ground_top - mid_top))
		if stage_ground:
			stage_ground.position = Vector2(x, ground_top)
			stage_ground.size = Vector2(stage_w, max(0.0, (stage_top + stage_h) - ground_top))
		if stage_decor:
			var decor_w = max(48.0, stage_w - 10.0)
			var decor_h = max(48.0, stage_h * 0.72)
			stage_decor.position = Vector2(x + ((stage_w - decor_w) * 0.5), stage_top + (stage_h - decor_h) - 10.0)
			stage_decor.size = Vector2(decor_w, decor_h)
		if stage_accent:
			var accent_rect = _stage_manifest_slot_rect(slot, animal_type, "primary")
			if accent_rect.size.x <= 0.0 or accent_rect.size.y <= 0.0:
				var accent_w = max(24.0, stage_w * 0.34)
				var accent_h = max(24.0, stage_h * 0.28)
				accent_rect = Rect2(Vector2(x + stage_w - accent_w - 6.0, ground_top - (accent_h * 0.28)), Vector2(accent_w, accent_h))
			stage_accent.position = accent_rect.position
			stage_accent.size = accent_rect.size
		if stage_frame:
			stage_frame.position = Vector2(x, stage_top)
			stage_frame.size = Vector2(stage_w, stage_h)
		if primary_shadow:
			primary_shadow.position = _stage_anchor_position(slot, animal_type, "home") + Vector2(0.0, stage_h * 0.012)
			primary_shadow.polygon = _ellipse_polygon(Vector2(max(12.0, stage_w * 0.18), max(2.5, stage_h * 0.028)))
			primary_shadow.visible = slot.size.x > 0.0 and slot.size.y > 0.0
		if pet_shadow:
			pet_shadow.position = _stage_anchor_position(slot, animal_type, "rest") + Vector2(0.0, stage_h * 0.035)
			pet_shadow.polygon = _ellipse_polygon(Vector2(max(10.0, stage_w * 0.14), max(2.5, stage_h * 0.024)))
			pet_shadow.visible = slot.size.x > 0.0 and slot.size.y > 0.0

	var floor_y = h - PET_FLOOR_INSET
	_set_pet_floor(floor_y)
	_update_environment_stage_selection()
	_update_celestial_layout()

func _set_pet_floor(floor_y: float):
	if game_manager == null:
		return
	for pet in game_manager.pets:
		if pet and pet.has_method("set_floor_y"):
			pet.set_floor_y(floor_y)

func _apply_mouse_passthrough_for_mode(focused: bool):
	_apply_window_input_mode()

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
		actions_bar.mouse_filter = Control.MOUSE_FILTER_PASS if clickable else Control.MOUSE_FILTER_IGNORE
	if action_tab_overlay:
		action_tab_overlay.mouse_filter = filter
	if action_menu_overlay:
		action_menu_overlay.mouse_filter = filter
	if settings_overlay:
		settings_overlay.mouse_filter = filter
	if basket_overlay:
		basket_overlay.mouse_filter = filter
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
		title_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if pet_portrait:
		pet_portrait.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if pet_name_label:
		pet_name_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if pet_gender_label:
		pet_gender_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if pet_age_label:
		pet_age_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if nav_arrows:
		nav_arrows.mouse_filter = Control.MOUSE_FILTER_PASS if clickable else Control.MOUSE_FILTER_IGNORE
	if add_pet_button:
		add_pet_button.mouse_filter = filter
	if doctor_button:
		doctor_button.mouse_filter = filter
	if minimize_button:
		minimize_button.mouse_filter = filter
	if settings_button:
		settings_button.mouse_filter = filter
	if pin_ui_button:
		pin_ui_button.mouse_filter = filter
	if basket_button:
		basket_button.mouse_filter = filter
	if ghost_button:
		ghost_button.mouse_filter = filter
	if memoriam_button:
		memoriam_button.mouse_filter = filter
	if pet_indicators:
		pet_indicators.mouse_filter = Control.MOUSE_FILTER_IGNORE
	
	# Set all children of actions_bar to ignore clicks when not focused
	if actions_bar:
		_set_container_clicks(actions_bar, clickable, true)
	if nav_arrows:
		_set_container_clicks(nav_arrows, clickable, true)
	if stats_panel:
		_set_container_clicks(stats_panel, false, false)

func _set_container_clicks(container: Control, clickable: bool, allow_children: bool = false):
	if container == null:
		return
	var filter = Control.MOUSE_FILTER_STOP if clickable else Control.MOUSE_FILTER_IGNORE
	container.mouse_filter = Control.MOUSE_FILTER_PASS if clickable and allow_children else filter
	for child in container.get_children():
		if child is Control:
			var child_control = child as Control
			var is_interactive_child = child_control is Button or child_control is LineEdit or child_control is HSlider or child_control is ScrollContainer
			if is_interactive_child:
				child_control.mouse_filter = filter
			else:
				child_control.mouse_filter = Control.MOUSE_FILTER_IGNORE
			if not is_interactive_child:
				_set_container_clicks(child_control, clickable, allow_children)

func _collect_clickable_controls_at_point(root: Node, point: Vector2, out: Array):
	for i in range(root.get_child_count() - 1, -1, -1):
		var child = root.get_child(i)
		if child is Control:
			var ctrl = child as Control
			if not ctrl.visible or ctrl.mouse_filter == Control.MOUSE_FILTER_IGNORE:
				continue
			if ctrl.get_global_rect().has_point(point):
				_collect_clickable_controls_at_point(ctrl, point, out)
				if ctrl is Button:
					out.append(ctrl)
		else:
			_collect_clickable_controls_at_point(child, point, out)

func _dispatch_overlay_button_at_point(point: Vector2) -> bool:
	var btn = _find_top_clickable_button_at_point(point)
	debug_last_pinned_dispatch = {
		"mode": "point_dispatch",
		"point": {"x": point.x, "y": point.y},
		"button_text": btn.text if btn else "",
		"button_name": btn.name if btn else "",
		"window_has_focus": window_has_focus,
		"overlay_ui_pinned": overlay_ui_pinned
	}
	if btn == null:
		return false
	_emit_button_action(btn)
	debug_last_pinned_dispatch["result"] = "emitted"
	return true

func _dispatch_pinned_overlay_click(point: Vector2) -> bool:
	if not overlay_ui_pinned or window_has_focus:
		return false
	var dispatch_point = point
	var screen_point = _native_cursor_screen_point()
	if screen_point.x > -100000.0 and screen_point.y > -100000.0:
		dispatch_point = _screen_to_window_local_point(screen_point)
	return _dispatch_overlay_button_at_point(dispatch_point)

func _dispatch_recent_pinned_activation_click(attempt: int = 0):
	if not overlay_ui_pinned or not window_has_focus:
		debug_last_pinned_dispatch = {
			"mode": "activation_dispatch",
			"attempt": attempt,
			"result": "skipped_not_pinned_or_focused",
			"window_has_focus": window_has_focus,
			"overlay_ui_pinned": overlay_ui_pinned
		}
		return
	var click_info = _native_recent_click_info(NATIVE_CLICK_MAX_AGE_MS + (attempt * 80))
	if click_info.is_empty():
		debug_last_pinned_dispatch = {
			"mode": "activation_dispatch",
			"attempt": attempt,
			"result": "no_click_info",
			"window_has_focus": window_has_focus,
			"overlay_ui_pinned": overlay_ui_pinned,
			"native_focus_state": native_focus_state
		}
		if native_focus_backend == "helper" and attempt < 4:
			await get_tree().create_timer(0.05).timeout
			_dispatch_recent_pinned_activation_click(attempt + 1)
		elif native_focus_backend != "helper":
			_replay_current_left_click_after_focus()
		return
	var point_value = click_info.get("point", Vector2(-1000000.0, -1000000.0))
	if typeof(point_value) != TYPE_VECTOR2:
		debug_last_pinned_dispatch = {
			"mode": "activation_dispatch",
			"attempt": attempt,
			"result": "invalid_point",
			"click_info": click_info
		}
		if native_focus_backend == "helper" and attempt < 4:
			await get_tree().create_timer(0.05).timeout
			_dispatch_recent_pinned_activation_click(attempt + 1)
		elif native_focus_backend != "helper":
			_replay_current_left_click_after_focus()
		return
	var local_point = _screen_to_window_local_point(point_value)
	debug_last_pinned_dispatch = {
		"mode": "activation_dispatch",
		"attempt": attempt,
		"result": "dispatching",
		"screen_point": {"x": point_value.x, "y": point_value.y},
		"local_point": {"x": local_point.x, "y": local_point.y},
		"click_info": click_info
	}
	_dispatch_overlay_button_at_point(local_point)

func _find_top_clickable_button_at_point(point: Vector2) -> Button:
	var hits: Array = []
	_collect_clickable_controls_at_point(self, point, hits)
	for hit in hits:
		if hit is Button:
			var btn = hit as Button
			if not btn.disabled:
				return btn
	return null

func _emit_button_action(btn: Button):
	if btn == null or not is_instance_valid(btn) or btn.disabled:
		return
	if btn.toggle_mode:
		var toggled_state = not btn.button_pressed
		btn.set_pressed_no_signal(toggled_state)
		btn.toggled.emit(toggled_state)
	btn.pressed.emit()

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
		_apply_pet_bounds_for_mode(window_has_focus)
		_recenter_modal_card(egg_selection_overlay)
		_recenter_modal_card(naming_overlay)
		_recenter_modal_card(death_overlay)
		_recenter_modal_card(settings_overlay)
		_recenter_modal_card(basket_overlay)

func _handle_focus_change(focused: bool):
	var was_focused = window_has_focus
	window_has_focus = focused

	var activation_click_recent = _native_left_click_recent() or bool(native_focus_state.get("left_button_down", false)) or Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT)
	var dispatch_activation_click = focused and overlay_ui_pinned and not was_focused and (activation_click_recent or native_focus_backend == "helper")

	# Save current tab state when losing focus
	if not focused and action_tab_open and not overlay_ui_pinned:
		last_open_action_tab = current_action_tab
	if not focused and action_tab_overlay and not overlay_ui_pinned:
		action_tab_overlay.visible = false
	
	var should_show_ui = _should_show_runtime_ui()
	if should_show_ui != ui_visible:
		ui_visible = should_show_ui

		if stats_panel:
			stats_panel.visible = should_show_ui
		if actions_bar:
			actions_bar.visible = should_show_ui
		if title_label:
			title_label.visible = should_show_ui
		if pet_portrait:
			pet_portrait.visible = should_show_ui
		if pet_name_label:
			pet_name_label.visible = should_show_ui
		if pet_gender_label:
			pet_gender_label.visible = should_show_ui
		if pet_age_label:
			pet_age_label.visible = should_show_ui
		if nav_arrows:
			nav_arrows.visible = should_show_ui
		if add_pet_button:
			add_pet_button.visible = should_show_ui
		if doctor_button:
			doctor_button.visible = should_show_ui
		if minimize_button:
			minimize_button.visible = should_show_ui
		if settings_button:
			settings_button.visible = should_show_ui
		if pin_ui_button:
			pin_ui_button.visible = should_show_ui
		if basket_button:
			basket_button.visible = should_show_ui
		if ghost_button:
			ghost_button.visible = should_show_ui
		if memoriam_button:
			memoriam_button.visible = should_show_ui
		if pet_indicators:
			pet_indicators.visible = should_show_ui
		if action_tab_overlay:
			action_tab_overlay.visible = should_show_ui
		if action_menu_overlay:
			action_menu_overlay.visible = should_show_ui
		if settings_overlay:
			settings_overlay.visible = should_show_ui
		if basket_overlay:
			basket_overlay.visible = should_show_ui
		if memoriam_overlay:
			memoriam_overlay.visible = should_show_ui
		if doctors_note_overlay:
			doctors_note_overlay.visible = should_show_ui
		if egg_selection_overlay:
			egg_selection_overlay.visible = should_show_ui
		if naming_overlay:
			naming_overlay.visible = should_show_ui
		if death_overlay:
			death_overlay.visible = should_show_ui
		if feeding_panel_overlay:
			feeding_panel_overlay.visible = should_show_ui
		if ghost_overlay:
			ghost_overlay.visible = should_show_ui
	
	# Restore previous action tab when focus returns (unless a priority modal is open).
	if focused and last_open_action_tab != "" and egg_selection_overlay == null and naming_overlay == null and death_overlay == null and action_tab_overlay == null:
		_show_action_tab(last_open_action_tab)

	_apply_runtime_visibility_state()
	
	_apply_window_mode_layout(focused)
	if monitor_roam_active:
		if focused or overlay_ui_pinned:
			_recall_all_pets_home(1.2, true)
		else:
			_start_monitor_roam_all_pets()
	elif focused or overlay_ui_pinned:
		_recall_all_pets_home()
	else:
		_recall_all_pets_home(0.0)

	if dispatch_activation_click:
		call_deferred("_dispatch_recent_pinned_activation_click", 0)

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
	nav_arrows.z_index = Z_UI_OVERLAY
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
	pet_indicators.mouse_filter = Control.MOUSE_FILTER_IGNORE
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

	basket_button = Button.new()
	basket_button.text = "BIN"
	basket_button.custom_minimum_size = Vector2(40, 24)
	basket_button.size = Vector2(40, 24)
	basket_button.position = Vector2(238, 6)
	basket_button.z_index = Z_UI_OVERLAY
	basket_button.add_theme_font_size_override("font_size", 8)
	basket_button.add_theme_stylebox_override("normal", create_button_style_normal())
	basket_button.add_theme_stylebox_override("hover", create_button_style_hover())
	basket_button.add_theme_stylebox_override("pressed", create_button_style_pressed())
	basket_button.pressed.connect(_show_basket_overlay)
	add_child(basket_button)
	
	# Settings button
	settings_button = create_icon_button("settings", _show_settings_menu, "", "Settings")
	settings_button.position = Vector2(282, 30)
	add_child(settings_button)

	# Manual HUD pin keeps the UI usable while another app has focus.
	pin_ui_button = Button.new()
	pin_ui_button.text = "PIN"
	pin_ui_button.toggle_mode = true
	pin_ui_button.custom_minimum_size = Vector2(28, 24)
	pin_ui_button.size = Vector2(28, 24)
	pin_ui_button.position = Vector2(186, 30)
	pin_ui_button.z_index = Z_UI_OVERLAY
	pin_ui_button.add_theme_font_size_override("font_size", 8)
	pin_ui_button.add_theme_stylebox_override("normal", create_button_style_normal())
	pin_ui_button.add_theme_stylebox_override("hover", create_button_style_hover())
	pin_ui_button.add_theme_stylebox_override("pressed", create_button_style_pressed())
	pin_ui_button.toggled.connect(_on_overlay_pin_toggled)
	add_child(pin_ui_button)
	
	# Ghost mode button (moved to make room)
	ghost_button = create_icon_button("ghost", _toggle_ghost_mode, "", "Ghost mode")
	ghost_button.position = Vector2(218, 30)
	add_child(ghost_button)
	
	# In Memoriam button
	memoriam_button = create_icon_button("memoriam", _show_in_memoriam, "", "In Memoriam")
	memoriam_button.position = Vector2(250, 30)
	add_child(memoriam_button)
	
	# Pet portrait
	pet_portrait = TextureRect.new()
	pet_portrait.position = Vector2(10, 38)
	pet_portrait.size = Vector2(40, 40)
	pet_portrait.mouse_filter = Control.MOUSE_FILTER_IGNORE
	pet_portrait.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	pet_portrait.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	add_child(pet_portrait)

	# Pet name
	pet_name_label = Label.new()
	pet_name_label.add_theme_font_size_override("font_size", 10)
	pet_name_label.add_theme_color_override("font_color", _main_text_color())
	pet_name_label.position = Vector2(58, 48)
	pet_name_label.size = Vector2(108, 14)
	pet_name_label.clip_text = true
	pet_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	add_child(pet_name_label)

	# Pet gender marker (separate from name)
	pet_gender_label = Label.new()
	pet_gender_label.add_theme_font_size_override("font_size", 9)
	pet_gender_label.add_theme_color_override("font_color", _main_text_color().darkened(0.2))
	pet_gender_label.position = Vector2(170, 48)
	pet_gender_label.size = Vector2(20, 14)
	pet_gender_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(pet_gender_label)

	# Pet age
	pet_age_label = Label.new()
	pet_age_label.add_theme_font_size_override("font_size", 10)
	pet_age_label.add_theme_color_override("font_color", _main_text_color().darkened(0.2))
	pet_age_label.position = Vector2(196, 48)
	pet_age_label.size = Vector2(120, 14)
	pet_age_label.clip_text = true
	pet_age_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	add_child(pet_age_label)
	
	# Stats panel (upper UI area, above pet/environment)
	stats_panel = VBoxContainer.new()
	stats_panel.position = Vector2(58, 70)
	stats_panel.size = Vector2(252, 126)
	stats_panel.clip_contents = true
	stats_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
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
	actions_bar.z_index = Z_UI_OVERLAY
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

func _update_environment_stage_selection():
	if environment_stages.is_empty():
		return

	var active_index = clamp(game_manager.active_pet_index if game_manager else 0, 0, max(0, environment_stages.size() - 1))
	for i in range(environment_stages.size()):
		var frame = environment_stages[i].get("frame") as Panel
		if frame == null:
			continue

		var has_pet = game_manager != null and i < game_manager.get_pet_count()
		var is_active = has_pet and i == active_index
		var frame_style = StyleBoxFlat.new()
		frame_style.bg_color = Color(0.0, 0.0, 0.0, 0.0)
		frame_style.set_corner_radius_all(6)
		frame_style.set_border_width_all(1 if has_pet else 0)
		frame_style.border_color = Color(1.0, 1.0, 1.0, 0.0)

		if has_pet:
			frame_style.bg_color = Color(1.0, 1.0, 1.0, 0.02)
			frame_style.border_color = _detail_text_color().darkened(0.25)
		if is_active:
			frame_style.bg_color = Color(0.96, 0.83, 0.2, 0.08)
			frame_style.border_color = _main_text_color().lightened(0.2)
			frame_style.set_border_width_all(3)

		frame.add_theme_stylebox_override("panel", frame_style)

func create_stat_bar(label_text, stat_name):
	var container = HBoxContainer.new()
	container.custom_minimum_size = Vector2(280, 12)
	container.add_theme_constant_override("separation", 8)
	container.name = stat_name + "_row"

	var icon_ref = _status_icon_ref_for_stat(stat_name)
	if icon_ref != "":
		var icon = TextureRect.new()
		icon.name = stat_name + "_icon"
		icon.custom_minimum_size = Vector2(12, 12)
		icon.texture = _load_ui_asset_texture(icon_ref)
		icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		container.add_child(icon)

	var label = Label.new()
	label.name = stat_name + "_label"
	label.text = label_text
	label.custom_minimum_size = Vector2(88 if icon_ref != "" else 96, 12)
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

func _load_texture(path: String) -> Texture2D:
	if path == "":
		return null
	path = _prefer_shared_runtime_asset_path(path)
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

func _prefer_shared_runtime_asset_path(path: String) -> String:
	if path.begins_with("res://sprites/"):
		var runtime_path = path.replace("res://sprites/", "res://sprites_shared_runtime/")
		if _resource_or_file_exists(runtime_path):
			return runtime_path
	return path

func load_icon_texture(icon_name: String) -> Texture2D:
	var path = "res://sprites/icons/" + icon_name + ".png"
	return _load_texture(path)

func _load_egg_texture(stage_index: int) -> Texture2D:
	var key = str(stage_index)
	if egg_texture_cache.has(key):
		return egg_texture_cache[key]
	var path = "res://sprites/egg/egg_%02d.png" % stage_index
	var tex = _load_texture(path)
	if tex:
		egg_texture_cache[key] = tex
	return tex

func _load_celestial_texture(path: String) -> Texture2D:
	if celestial_texture_cache.has(path):
		return celestial_texture_cache[path]
	var tex = _load_texture(path)
	if tex:
		celestial_texture_cache[path] = tex
	return tex

func _load_environment_texture(path: String) -> Texture2D:
	if environment_texture_cache.has(path):
		return environment_texture_cache[path]
	var tex = _load_texture(path)
	if tex:
		environment_texture_cache[path] = tex
	return tex

func _load_portrait_texture(path: String) -> Texture2D:
	if portrait_texture_cache.has(path):
		return portrait_texture_cache[path]
	var tex = _load_texture(path)
	if tex:
		portrait_texture_cache[path] = tex
	return tex

func _portrait_age_key(pd: PetData) -> String:
	if pd == null:
		return "adult"
	match pd.stage:
		1:
			return "baby"
		2:
			return "teen"
		_:
			return "adult"

func _update_pet_portrait():
	if pet_portrait == null:
		return
	var pd = game_manager.get_active_pet_data() if game_manager else null
	if pd == null:
		pet_portrait.visible = false
		pet_portrait.texture = null
		pet_portrait.tooltip_text = ""
		return

	pet_portrait.visible = true
	if pd.stage <= 0 or pd.is_hatching:
		pet_portrait.texture = _load_egg_texture(0)
		pet_portrait.modulate = EGG_COLORS.get(pd.egg_color, Color.WHITE)
		pet_portrait.tooltip_text = "%s egg" % pd.egg_color.capitalize()
		return

	var portrait_path = "res://sprites/portraits/%s/%s_%s_%s.png" % [pd.animal_type, _portrait_age_key(pd), pd.gender, pd.egg_color]
	var portrait_tex = _load_portrait_texture(portrait_path)
	if portrait_tex == null:
		portrait_path = "res://sprites/portraits/%s/%s_%s.png" % [pd.animal_type, _portrait_age_key(pd), pd.gender]
		portrait_tex = _load_portrait_texture(portrait_path)
	pet_portrait.texture = portrait_tex
	var tint = EGG_COLORS.get(pd.egg_color, Color.WHITE)
	pet_portrait.modulate = Color.WHITE if portrait_path.contains("_" + pd.egg_color + ".png") else Color.WHITE.lerp(tint, 0.45)
	pet_portrait.tooltip_text = "%s %s %s" % [_portrait_age_key(pd).capitalize(), pd.gender.capitalize(), pd.animal_type.capitalize()]

func _environment_decor_path(animal_type: String) -> String:
	if animal_type == "":
		return ""
	return "res://sprites/environment/%s.png" % animal_type

func _item_asset_path(asset_id: String) -> String:
	if asset_id == "":
		return ""
	for category in ["containers", "food_birds", "food_herbivore", "food_omnivore", "toys_a", "toys_b", "utility"]:
		var path = "res://sprites/items/%s/%s.png" % [category, asset_id]
		if _resource_or_file_exists(path):
			return path
	return ""

func _environment_slot_asset_path(animal_type: String, slot_id: String) -> String:
	if game_manager == null or not game_manager.has_method("get_habitat_loadout"):
		return ""
	var loadout = game_manager.get_habitat_loadout(animal_type)
	var slots = loadout.get("slots", [])
	if not (slots is Array):
		return ""
	for slot in slots:
		if slot is Dictionary and str(slot.get("slotId", "")) == slot_id:
			return _item_asset_path(str(slot.get("assetId", "")))
	return ""

func _load_ui_asset_texture(asset_ref: String) -> Texture2D:
	if asset_ref.begins_with("res://"):
		return _load_texture(asset_ref)
	return load_icon_texture(asset_ref)

func _apply_button_icon(button: Button, asset_ref: String):
	if button == null or asset_ref == "":
		return
	var tex = _load_ui_asset_texture(asset_ref)
	if tex:
		button.icon = tex
		button.icon_alignment = HORIZONTAL_ALIGNMENT_LEFT

func _status_icon_ref_for_stat(stat_name: String) -> String:
	match stat_name:
		"hunger":
			return "res://sprites/status/hungry.png"
		"hydration":
			return "res://sprites/status/thirsty.png"
		"happiness":
			return "res://sprites/status/happy.png"
		"energy":
			return "res://sprites/status/sleepy.png"
		"health":
			return "res://sprites/status/sick.png"
		"cleanliness":
			return "res://sprites/status/dirty.png"
		"affection":
			return "res://sprites/status/comforted.png"
		_:
			return ""

func _moon_phase_index() -> int:
	var date = Time.get_date_dict_from_system()
	var year = int(date.get("year", 2000))
	var month = int(date.get("month", 1))
	var day = int(date.get("day", 1))
	if month < 3:
		year -= 1
		month += 12
	month += 1
	var c = int(365.25 * year)
	var e = int(30.6 * month)
	var jd = (c + e + day - 694039.09) / 29.5305882
	var phase = jd - floor(jd)
	var index = int(round(phase * 8.0))
	return ((index % 8) + 8) % 8

func _current_time_hours() -> float:
	var dt = Time.get_datetime_dict_from_system()
	return float(int(dt.get("hour", 12))) + (float(int(dt.get("minute", 0))) / 60.0)

func _celestial_is_moon_time() -> bool:
	var hour = _current_time_hours()
	return hour < 5.0 or hour >= 20.0

func _celestial_arc_progress() -> float:
	var hour = _current_time_hours()
	if _celestial_is_moon_time():
		var night_hour = hour if hour >= 20.0 else hour + 24.0
		return clamp((night_hour - 20.0) / 9.0, 0.0, 1.0)
	return clamp((hour - 5.0) / 15.0, 0.0, 1.0)

func _current_celestial_texture_path() -> String:
	var dt = Time.get_datetime_dict_from_system()
	var hour = int(dt.get("hour", 12))
	if hour < 5 or hour >= 20:
		return "res://sprites/celestial/moon_%02d.png" % _moon_phase_index()
	if hour < 8:
		return "res://sprites/celestial/sun_00.png"
	if hour < 16:
		return "res://sprites/celestial/sun_01.png"
	if hour < 19:
		return "res://sprites/celestial/sun_02.png"
	return "res://sprites/celestial/sun_03.png"

func _update_celestial_layout():
	if celestial_sprite == null:
		return
	if habitat_proof_mode:
		celestial_sprite.visible = false
		return
	if environment_stages.is_empty() or game_manager == null or game_manager.get_pet_count() == 0:
		celestial_sprite.visible = false
		return
	var slots = _get_environment_slot_rects(float(get_window().size.x), float(get_window().size.y), game_manager.get_pet_count())
	var active_index = clamp(game_manager.active_pet_index, 0, max(0, slots.size() - 1))
	if active_index >= slots.size():
		celestial_sprite.visible = false
		return
	var slot = slots[active_index]
	var is_moon = _celestial_is_moon_time()
	var progress = _celestial_arc_progress()
	var size = 34.0 if is_moon else 42.0
	var margin_x = 12.0
	var start_x = slot.position.x + margin_x
	var end_x = slot.position.x + slot.size.x - size - margin_x
	var arc_span = max(0.0, end_x - start_x)
	var x = start_x + (arc_span * progress)
	var base_y = slot.position.y + (slot.size.y * 0.18)
	var arc_height = max(14.0, slot.size.y * 0.16)
	var arc_lift = sin(progress * PI) * arc_height
	var y = base_y + (arc_height - arc_lift)
	celestial_sprite.visible = true
	celestial_sprite.position = Vector2(x, y)
	celestial_sprite.size = Vector2(size, size)
	celestial_sprite.modulate = Color(1.0, 1.0, 1.0, 0.9 if is_moon else 1.0)

func _update_celestial_sprite(force: bool = false):
	if celestial_sprite == null:
		return
	var path = _current_celestial_texture_path()
	if force or celestial_sprite.texture == null or str(celestial_sprite.get_meta("asset_path", "")) != path:
		var tex = _load_celestial_texture(path)
		if tex:
			celestial_sprite.texture = tex
			celestial_sprite.set_meta("asset_path", path)
	_update_celestial_layout()

func _create_egg_choice_button(egg_color: String) -> Button:
	var egg_btn = Button.new()
	egg_btn.custom_minimum_size = Vector2(40, 52)
	egg_btn.pressed.connect(_on_egg_selected.bind(egg_color))

	var egg_style = StyleBoxFlat.new()
	egg_style.bg_color = Color(1.0, 1.0, 1.0, 0.02)
	egg_style.set_corner_radius_all(8)
	egg_style.set_border_width_all(1)
	egg_style.border_color = EGG_COLORS[egg_color].darkened(0.25)
	egg_btn.add_theme_stylebox_override("normal", egg_style)

	var hover_style = egg_style.duplicate()
	hover_style.border_color = _detail_text_color()
	hover_style.bg_color = Color(1.0, 1.0, 1.0, 0.06)
	egg_btn.add_theme_stylebox_override("hover", hover_style)

	var press_style = egg_style.duplicate()
	press_style.bg_color = Color(1.0, 1.0, 1.0, 0.1)
	egg_btn.add_theme_stylebox_override("pressed", press_style)

	var egg_texture = TextureRect.new()
	egg_texture.mouse_filter = Control.MOUSE_FILTER_IGNORE
	egg_texture.texture = _load_egg_texture(0)
	egg_texture.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	egg_texture.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	egg_texture.position = Vector2(4, 3)
	egg_texture.size = Vector2(32, 40)
	egg_texture.modulate = EGG_COLORS[egg_color]
	egg_btn.add_child(egg_texture)

	return egg_btn

func create_icon_button(icon_name: String, action_callback: Callable, action_arg: String = "", tooltip: String = "") -> Button:
	var btn = Button.new()
	btn.custom_minimum_size = Vector2(28, 24)
	btn.size = Vector2(28, 24)
	btn.z_index = Z_UI_OVERLAY
	
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
		if pet_portrait:
			pet_portrait.visible = false
		if pet_name_label:
			pet_name_label.text = "No Pet"
			pet_name_label.tooltip_text = ""
		if pet_gender_label:
			pet_gender_label.text = ""
		if pet_age_label:
			pet_age_label.text = ""
		return
	
	var gender_symbol = "M" if pd.gender == "male" else "F"
	_update_pet_portrait()
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
	_update_environment_stage_selection()
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
		"drink":
			sound_manager.play_sound(animal, "drink", gender)
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
		"fetch_ball", "play_ball":
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
	_set_ui_clickable(ui_visible)

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
		_recall_all_pets_home(2.0, monitor_roam_active)
	action_tab_recall_pending_hold = false
	_set_ui_clickable(ui_visible)

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
		match item[1]:
			"feed_small":
				_apply_button_icon(btn, "res://sprites/items/food_omnivore/grain_mix.png")
			"feed_full":
				_apply_button_icon(btn, "res://sprites/items/food_omnivore/snack_bowl.png")
			"feed_treat":
				_apply_button_icon(btn, "res://sprites/items/food_omnivore/berry_cluster.png")
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
		match item[1]:
			"feed_hydrate":
				_apply_button_icon(btn2, "res://sprites/items/containers/water_bowl.png")
			"feed_forage":
				_apply_button_icon(btn2, "res://sprites/items/toys_a/leaf_pile.png")
		options2.add_child(btn2)
	_fit_row_buttons(options2, row_w, 24)

	var utility = HBoxContainer.new()
	utility.add_theme_constant_override("separation", 8)
	content.add_child(utility)

	var refill_btn = Button.new()
	refill_btn.text = "Refill Bowl"
	refill_btn.custom_minimum_size = Vector2(86, 24)
	refill_btn.pressed.connect(_on_refill_water)
	_apply_button_icon(refill_btn, "res://sprites/items/containers/water_bowl.png")
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
	_apply_button_icon(open_btn, "res://sprites/items/care/first_aid_kit.png")
	content.add_child(open_btn)
	_append_tab_hint(content, "Hold a treatment, then click your pet to apply it.")

func _create_rest_tab_content(content: VBoxContainer):
	var pd = game_manager.get_active_pet_data()
	var toggle = Button.new()
	toggle.text = "Wake Up" if pd and pd.is_sleeping else "Sleep"
	toggle.custom_minimum_size = Vector2(180, 30)
	toggle.pressed.connect(_do_action_from_tab.bind("rest_toggle"))
	_apply_button_icon(toggle, "res://sprites/items/toys_b/nest_bed.png")
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
		match item[1]:
			"groom_haircut":
				_apply_button_icon(btn, "res://sprites/items/care/grooming_brush.png")
			"groom_dental":
				_apply_button_icon(btn, "res://sprites/items/care/thermometer.png")
			"groom_bathing":
				_apply_button_icon(btn, "res://sprites/items/care/soap_bottle.png")
		row.add_child(btn)
	_fit_row_buttons(row, row_w, 24)
	_append_tab_hint(content, "Grooming improves coat quality and overall comfort.")

func _create_exercise_tab_content(content: VBoxContainer):
	var row_w = content.size.x
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation", 8)
	content.add_child(row)

	var fetch_btn = Button.new()
	fetch_btn.text = "Fetch Ball"
	fetch_btn.custom_minimum_size = Vector2(110, 28)
	fetch_btn.pressed.connect(_on_hold_ball)
	_apply_button_icon(fetch_btn, "res://sprites/items/toys_a/ball.png")
	row.add_child(fetch_btn)

	var workout_btn = Button.new()
	workout_btn.text = "Workout"
	workout_btn.custom_minimum_size = Vector2(110, 28)
	workout_btn.pressed.connect(_do_action_from_tab.bind("exercise_workout"))
	_apply_button_icon(workout_btn, "exercise")
	row.add_child(workout_btn)

	_fit_row_buttons(row, row_w, 28)
	_append_tab_hint(content, "Fetch uses the universal ball: pick it up, then click where to throw. Workout builds fitness faster but can injure if overused.")

func _create_pet_tab_content(content: VBoxContainer):
	var row_w = content.size.x
	var options = [["Head Pat", "pet_pat"], ["Cuddle", "pet_cuddle"], ["Play", "pet_play"], ["Talk", "pet_talk"]]
	for opt in options:
		var btn = Button.new()
		btn.text = opt[0]
		btn.custom_minimum_size = Vector2(row_w, 24)
		btn.add_theme_color_override("font_color", _detail_text_color())
		btn.pressed.connect(_do_action_from_tab.bind(opt[1]))
		match opt[1]:
			"pet_pat":
				_apply_button_icon(btn, "res://sprites/status/comforted.png")
			"pet_cuddle":
				_apply_button_icon(btn, "res://sprites/items/toys_b/blanket_mat.png")
			"pet_play":
				_apply_button_icon(btn, "res://sprites/items/toys_a/ball.png")
			"pet_talk":
				_apply_button_icon(btn, "res://sprites/status/happy.png")
		content.add_child(btn)
	_append_tab_hint(content, "Play chances improve as affection rises.")

func _do_action_from_tab(action_name: String):
	var reopen_tab = _base_action_name(action_name)
	var result = game_manager.perform_action(action_name)
	_play_action_sound(_sound_action_name(action_name))
	if result is Dictionary and result.get("message", "") != "":
		_show_feedback_message(result.get("message", ""))
	_on_stats_updated()
	if action_tab_open and reopen_tab != "":
		_show_action_tab(reopen_tab)

func _base_action_name(action_name: String) -> String:
	if action_name.begins_with("feed_"):
		return "feed"
	if action_name.begins_with("pet_"):
		return "pet"
	if action_name.begins_with("groom_"):
		return "groom"
	if action_name.begins_with("exercise_"):
		return "exercise"
	if action_name == "fetch_ball" or action_name == "play_ball":
		return "exercise"
	if action_name.begins_with("rest_"):
		return "rest"
	return action_name

func _sound_action_name(action_name: String) -> String:
	if action_name == "feed_hydrate":
		return "drink"
	if action_name == "fetch_ball" or action_name == "play_ball":
		return "exercise"
	return _base_action_name(action_name)

func show_action_menu(action_type: String):
	# Legacy entry point; route to the unified action-tab UX.
	_show_action_tab(action_type)

func add_food_options(container: VBoxContainer):
	for food_key in FOOD_TYPES.keys():
		var btn = Button.new()
		btn.text = FOOD_TYPES[food_key]["name"]
		btn.custom_minimum_size = Vector2(240, 25)
		btn.pressed.connect(_on_food_item_selected.bind(food_key))
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
	var fetch_btn = Button.new()
	fetch_btn.text = "Fetch Ball"
	fetch_btn.custom_minimum_size = Vector2(240, 25)
	fetch_btn.pressed.connect(_on_hold_ball)
	container.add_child(fetch_btn)

	var workout_btn = Button.new()
	workout_btn.text = "Workout"
	workout_btn.custom_minimum_size = Vector2(240, 25)
	workout_btn.pressed.connect(_do_action_from_tab.bind("exercise_workout"))
	container.add_child(workout_btn)

func _on_food_selected(_food: String):
	_close_action_menu()
	# Food gives different benefits
	game_manager.perform_action("feed")

func _close_action_menu():
	if action_menu_overlay:
		action_menu_overlay.queue_free()
		action_menu_overlay = null

func _show_basket_overlay():
	close_all_overlays()
	var auto_captured = false
	if overlay_ui_pinned and not window_has_focus:
		auto_captured = _capture_clipboard_link(false)
	var modal = _create_priority_modal(Vector2(292, 336), 0.58)
	basket_overlay = modal["overlay"]
	var card = modal["card"] as Panel
	card.name = "basket_card"

	var title = Label.new()
	title.text = "Link Basket"
	title.add_theme_font_size_override("font_size", 14)
	title.add_theme_color_override("font_color", _detail_text_color())
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(0, 8)
	title.size = Vector2(292, 22)
	card.add_child(title)

	var hint = Label.new()
	hint.name = "basket_hint"
	hint.text = "Copy a link, then capture it. Dropped URL shortcut/text files are accepted too."
	hint.add_theme_font_size_override("font_size", 9)
	hint.add_theme_color_override("font_color", _detail_text_color())
	hint.position = Vector2(12, 34)
	hint.size = Vector2(268, 34)
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD
	card.add_child(hint)

	var capture_btn = Button.new()
	capture_btn.name = "basket_capture_button"
	capture_btn.text = "Capture Clipboard"
	capture_btn.custom_minimum_size = Vector2(136, 24)
	capture_btn.position = Vector2(12, 74)
	capture_btn.pressed.connect(_capture_clipboard_link)
	card.add_child(capture_btn)

	var count_label = Label.new()
	count_label.name = "basket_count_label"
	count_label.position = Vector2(160, 76)
	count_label.size = Vector2(120, 20)
	count_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	count_label.add_theme_font_size_override("font_size", 9)
	count_label.add_theme_color_override("font_color", _detail_text_color())
	card.add_child(count_label)

	var scroll = ScrollContainer.new()
	scroll.name = "basket_scroll"
	scroll.position = Vector2(12, 106)
	scroll.size = Vector2(268, 184)
	card.add_child(scroll)

	var content = VBoxContainer.new()
	content.name = "basket_content"
	content.custom_minimum_size = Vector2(252, 184)
	content.add_theme_constant_override("separation", 8)
	scroll.add_child(content)

	var close_btn = Button.new()
	close_btn.text = "Close"
	close_btn.custom_minimum_size = Vector2(96, 24)
	close_btn.position = Vector2(98, 300)
	close_btn.pressed.connect(_close_basket_overlay)
	card.add_child(close_btn)

	_refresh_basket_overlay()
	_apply_detail_text_theme(basket_overlay)
	if auto_captured:
		_show_feedback_message("Captured copied link into basket.")

func _refresh_basket_overlay():
	_update_basket_button_state()
	if basket_overlay == null:
		return
	var count_label = basket_overlay.find_child("basket_count_label", true, false) as Label
	if count_label:
		count_label.text = "%d / %d saved" % [basket_entries.size(), BASKET_MAX_LINKS]

	var content = basket_overlay.find_child("basket_content", true, false) as VBoxContainer
	if content == null:
		return
	for child in content.get_children():
		child.queue_free()

	if basket_entries.is_empty():
		var empty_label = Label.new()
		empty_label.text = "No links saved yet."
		empty_label.add_theme_font_size_override("font_size", 10)
		empty_label.add_theme_color_override("font_color", _detail_text_color())
		content.add_child(empty_label)

		var hotkey_label = Label.new()
		hotkey_label.text = "Shortcut: %s" % BASKET_CAPTURE_HOTKEY
		hotkey_label.add_theme_font_size_override("font_size", 9)
		hotkey_label.add_theme_color_override("font_color", COLOR_TEXT_DIM)
		content.add_child(hotkey_label)
		return

	for i in range(basket_entries.size()):
		var entry = basket_entries[i]
		var row = VBoxContainer.new()
		row.custom_minimum_size = Vector2(252, 48)
		row.add_theme_constant_override("separation", 4)
		content.add_child(row)

		var pick_btn = Button.new()
		pick_btn.text = "%d. %s" % [i + 1, _basket_entry_summary(entry)]
		pick_btn.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
		pick_btn.clip_text = true
		pick_btn.custom_minimum_size = Vector2(252, 22)
		pick_btn.tooltip_text = str(entry.get("url", ""))
		pick_btn.pressed.connect(_copy_basket_entry_to_clipboard.bind(i, true))
		row.add_child(pick_btn)

		var meta_row = HBoxContainer.new()
		meta_row.add_theme_constant_override("separation", 6)
		row.add_child(meta_row)

		var source_label = Label.new()
		source_label.text = str(entry.get("source", "saved"))
		source_label.custom_minimum_size = Vector2(118, 18)
		source_label.add_theme_font_size_override("font_size", 8)
		source_label.add_theme_color_override("font_color", COLOR_TEXT_DIM)
		meta_row.add_child(source_label)

		var open_btn = Button.new()
		open_btn.text = "OPEN"
		open_btn.custom_minimum_size = Vector2(48, 18)
		open_btn.add_theme_font_size_override("font_size", 8)
		open_btn.pressed.connect(_open_basket_entry.bind(i))
		meta_row.add_child(open_btn)

		var remove_btn = Button.new()
		remove_btn.text = "DEL"
		remove_btn.custom_minimum_size = Vector2(42, 18)
		remove_btn.add_theme_font_size_override("font_size", 8)
		remove_btn.pressed.connect(_remove_basket_entry.bind(i, true))
		meta_row.add_child(remove_btn)

func _close_basket_overlay():
	if basket_overlay:
		_fade_out_and_free(basket_overlay, UI_FADE_OUT_SEC)
		basket_overlay = null

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
	if action_menu_overlay == null:
		show_medicine_menu()
		return
	guidebook_page = 0
	_update_guidebook_content()

func _show_guidebook_medicines():
	if action_menu_overlay == null:
		show_medicine_menu()
		guidebook_page = 1
		_update_guidebook_content()
		return
	guidebook_page = 1
	_update_guidebook_content()

func _update_guidebook_content():
	if action_menu_overlay == null:
		return
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

			var icon = TextureRect.new()
			icon.custom_minimum_size = Vector2(18, 18)
			icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
			icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
			icon.texture = _load_ui_asset_texture(str(med_info.get("icon", "")))
			med_row.add_child(icon)
			
			# Name label
			var name_lbl = Label.new()
			name_lbl.text = med_info["name"] + " (" + str(count) + ")"
			name_lbl.add_theme_font_size_override("font_size", 9)
			name_lbl.add_theme_color_override("font_color", COLOR_TEXT)
			name_lbl.custom_minimum_size = Vector2(150, 20)
			med_row.add_child(name_lbl)

func _show_medicine_collect():
	if action_menu_overlay == null:
		show_medicine_menu()
		guidebook_page = 2
		_update_guidebook_content()
		return
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
		var med_info = MEDICINE_INFO.get(med_id, {"icon": "medicine"})
		held_item = {"type": "medicine", "name": med_id, "icon": med_info.get("icon", "medicine")}
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
		var stage_decor = stage.get("decor") as TextureRect
		var stage_accent = stage.get("accent") as TextureRect
		if stage_bg:
			stage_bg.color = env.get("bg", COLOR_BG)
		if stage_mid:
			stage_mid.color = env.get("mid", Color(0.18, 0.2, 0.21))
		if stage_ground:
			stage_ground.color = env.get("ground", Color(0.06, 0.07, 0.08))
		if stage_decor:
			if slot_pet_data:
				var decor_path = _environment_decor_path(slot_pet_data.animal_type)
				stage_decor.texture = _load_environment_texture(decor_path)
				stage_decor.visible = stage_decor.texture != null
			else:
				stage_decor.texture = null
				stage_decor.visible = false
		if stage_accent:
			if slot_pet_data:
				var accent_path = _environment_slot_asset_path(slot_pet_data.animal_type, "primary")
				stage_accent.texture = _load_environment_texture(accent_path)
				stage_accent.visible = stage_accent.texture != null
			else:
				stage_accent.texture = null
				stage_accent.visible = false

	_apply_environment_layout(get_window().size.y)
	_update_environment_stage_selection()
	_update_celestial_sprite(true)

func _on_active_pet_changed(_index):
	update_environment_background()
	_on_stats_updated()
	_update_celestial_sprite(true)

func _on_pet_added(_index):
	update_environment_background()
	_on_stats_updated()
	_update_celestial_sprite(true)

func _on_pet_state_changed(_state):
	pass

func _on_pet_died(pet_data: PetData):
	var pet_index = game_manager.pet_datas.find(pet_data)
	if pet_index >= 0:
		if game_manager.pets.size() > pet_index:
			var pet = game_manager.pets[pet_index]
			if pet:
				pet.pause_wandering()
				pet.current_animation = "sad"
				pet.animation_frame = 0
		_spawn_or_update_memorial(pet_data)

	if game_manager.active_pet_index >= game_manager.pets.size():
		game_manager.active_pet_index = max(0, game_manager.pets.size() - 1)

	_show_death_popup(pet_data)

	update_environment_background()
	_on_stats_updated()
	_update_celestial_sprite(true)

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
	_set_pet_name(pet_index, name_input.text)
	
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
	
	var pin_hint_row = HBoxContainer.new()
	pin_hint_row.add_theme_constant_override("separation", 10)
	options_container.add_child(pin_hint_row)

	var pin_hint_label = Label.new()
	pin_hint_label.text = "HUD Pin:"
	pin_hint_label.add_theme_font_size_override("font_size", 10)
	pin_hint_label.add_theme_color_override("font_color", _detail_text_color())
	pin_hint_label.custom_minimum_size = Vector2(120, 20)
	pin_hint_row.add_child(pin_hint_label)

	var pin_hint_value = Label.new()
	pin_hint_value.text = OVERLAY_PIN_HOTKEY
	pin_hint_value.add_theme_font_size_override("font_size", 10)
	pin_hint_value.add_theme_color_override("font_color", _detail_text_color())
	pin_hint_row.add_child(pin_hint_value)

	var basket_hint_row = HBoxContainer.new()
	basket_hint_row.add_theme_constant_override("separation", 10)
	options_container.add_child(basket_hint_row)

	var basket_hint_label = Label.new()
	basket_hint_label.text = "Basket Capture:"
	basket_hint_label.add_theme_font_size_override("font_size", 10)
	basket_hint_label.add_theme_color_override("font_color", _detail_text_color())
	basket_hint_label.custom_minimum_size = Vector2(120, 20)
	basket_hint_row.add_child(basket_hint_label)

	var basket_hint_value = Label.new()
	basket_hint_value.text = BASKET_CAPTURE_HOTKEY
	basket_hint_value.add_theme_font_size_override("font_size", 10)
	basket_hint_value.add_theme_color_override("font_color", _detail_text_color())
	basket_hint_row.add_child(basket_hint_value)

	# Desktop roam toggle
	var roam_row = HBoxContainer.new()
	roam_row.add_theme_constant_override("separation", 10)
	options_container.add_child(roam_row)

	var roam_label = Label.new()
	roam_label.text = "Desktop Roam:"
	roam_label.add_theme_font_size_override("font_size", 10)
	roam_label.add_theme_color_override("font_color", _detail_text_color())
	roam_label.custom_minimum_size = Vector2(120, 20)
	roam_row.add_child(roam_label)

	var roam_btn = Button.new()
	roam_btn.text = "ON" if settings["desktop_companion_roam"] else "OFF"
	roam_btn.custom_minimum_size = Vector2(60, 20)
	roam_btn.pressed.connect(_toggle_monitor_roam.bind(roam_btn))
	roam_row.add_child(roam_btn)
	
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
	_set_overlay_ui_pin(not overlay_ui_pinned)
	if btn:
		btn.text = "ON" if overlay_ui_pinned else "OFF"

func _toggle_monitor_roam(btn: Button):
	settings["desktop_companion_roam"] = not settings["desktop_companion_roam"]
	settings["experimental_monitor_roam"] = settings["desktop_companion_roam"]
	btn.text = "ON" if settings["desktop_companion_roam"] else "OFF"
	_apply_window_mode_layout(window_has_focus)
	if not window_has_focus:
		if _monitor_roam_requested():
			_start_monitor_roam_all_pets()
		else:
			_recall_all_pets_home(0.0)
	_apply_runtime_visibility_state()

func _apply_click_through_mode():
	_apply_runtime_visibility_state()
	if window_has_focus or not monitor_roam_active:
		_pin_window_bottom_right()

func _minimize_window():
	get_window().visible = false

func _input(event):
	if event.is_action_pressed("show-window"):
		_show_from_tray()
	elif event.is_action_pressed(OVERLAY_PIN_ACTION):
		_set_overlay_ui_pin(not overlay_ui_pinned)
	elif event.is_action_pressed(BASKET_CAPTURE_ACTION):
		_capture_clipboard_link()
	
	# Handle click to feed/medicate pet
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		var mouse_event = event as InputEventMouseButton
		if mouse_event.pressed:
			if _dispatch_pinned_overlay_click(mouse_event.position):
				get_viewport().set_input_as_handled()
				return
			if held_item.type != "":
				_handle_held_item_click(mouse_event.position)

func _show_from_tray():
	get_window().visible = true
	get_window().mode = Window.MODE_WINDOWED
	position_window()
	_handle_focus_change(true)

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
			
			if mem_pd.death_sprite_path != "":
				var tex = _load_texture(mem_pd.death_sprite_path)
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
	"plant": {"name": "Plant", "icon": "res://sprites/items/food_herbivore/leafy_greens.png", "stat": "hunger", "value": 20},
	"meat": {"name": "Meat", "icon": "res://sprites/items/food_predator/meat_chunk.png", "stat": "hunger", "value": 25},
	"sweet": {"name": "Treat", "icon": "res://sprites/items/food_omnivore/berry_cluster.png", "stat": "happiness", "value": 15},
	"salty": {"name": "Salty", "icon": "res://sprites/items/food_omnivore/fish_scrap.png", "stat": "energy", "value": 10}
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
	"woundClean": {"name": "Wound Cleaning", "icon": "res://sprites/items/care/bandage_roll.png"},
	"antibiotics": {"name": "Antibiotics", "icon": "res://sprites/items/care/pill_bottle.png"},
	"jointSupport": {"name": "Joint Support", "icon": "res://sprites/items/care/thermometer.png"},
	"immuneBoost": {"name": "Immune Booster", "icon": "res://sprites/items/care/medicine_dropper.png"},
	"dentalCare": {"name": "Dental Care", "icon": "res://sprites/items/care/grooming_brush.png"},
	"moodStabilizer": {"name": "Mood Stabilizer", "icon": "res://sprites/items/care/soap_bottle.png"},
	"energyTonic": {"name": "Energy Tonic", "icon": "res://sprites/items/care/syringe.png"},
	"appetiteStimulant": {"name": "Appetite Stimulant", "icon": "res://sprites/items/care/first_aid_kit.png"},
	"detoxHerbs": {"name": "Detox Herbs", "icon": "res://sprites/items/care/towel.png"},
	"vitaminSupplements": {"name": "Vitamins", "icon": "res://sprites/items/care/medicine_dropper.png"}
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
	held_item = {"type": "water", "name": "water", "icon": "res://sprites/items/containers/water_bowl.png"}
	_close_feeding_panel()
	_create_held_item_sprite()

func _on_hold_ball():
	held_item = {"type": "toy", "name": "ball", "icon": "res://sprites/items/toys_a/ball.png"}
	_close_action_tab()
	_create_held_item_sprite()
	_show_feedback_message("Throw the ball anywhere in the habitat stage.")

func _create_held_item_sprite():
	if held_item_sprite:
		held_item_sprite.queue_free()
	
	held_item_sprite = Sprite2D.new()
	var icon_ref = str(held_item.get("icon", ""))
	var tex = _load_ui_asset_texture(icon_ref)
	if tex:
		held_item_sprite.texture = tex
		held_item_sprite.scale = Vector2(2, 2)
		held_item_sprite.z_index = Z_HELD_OR_CARRIED_PROP
		add_child(held_item_sprite)

func _handle_held_item_click(click_position: Vector2):
	var pd = game_manager.get_active_pet_data()
	var pet = game_manager.get_active_pet()
	
	if pd == null or pet == null:
		_clear_held_item()
		return

	if held_item.type == "toy" and held_item.name == "ball":
		_handle_ball_throw(click_position)
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

func _handle_ball_throw(click_position: Vector2):
	var pd = game_manager.get_active_pet_data()
	var pet = game_manager.get_active_pet()
	if pd == null or pet == null:
		_clear_held_item()
		return

	var result = game_manager.perform_fetch_sequence(click_position)
	_spawn_ball_throw_marker(click_position, pet.position)
	if sound_manager:
		sound_manager.play_sound(pd.animal_type, "exercise", pd.gender)
	_clear_held_item()
	if result is Dictionary and result.get("message", "") != "":
		_show_feedback_message(result.get("message", ""))
	_on_stats_updated()

func _spawn_ball_throw_marker(target_position: Vector2, pet_position: Vector2):
	if thrown_ball_sprite and is_instance_valid(thrown_ball_sprite):
		thrown_ball_sprite.queue_free()
	var marker = Sprite2D.new()
	marker.texture = _load_ui_asset_texture("res://sprites/items/toys_a/ball.png")
	if marker.texture == null:
		marker.queue_free()
		thrown_ball_sprite = null
		return
	thrown_ball_sprite = marker
	marker.position = target_position
	marker.scale = Vector2(2, 2)
	marker.z_index = Z_HELD_OR_CARRIED_PROP
	add_child(marker)

	var return_point = pet_position + Vector2(0, -24)
	var tween = create_tween()
	tween.tween_property(marker, "position", return_point, 0.45)
	tween.tween_property(marker, "modulate:a", 0.0, 0.25)
	tween.tween_callback(func():
		if is_instance_valid(marker):
			marker.queue_free()
		if thrown_ball_sprite == marker:
			thrown_ball_sprite = null
	)

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
	feedback_label.z_index = Z_FLOATING_FEEDBACK
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
			ghost_overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
			add_child(ghost_overlay)
			# Move to front
			move_child(ghost_overlay, get_child_count() - 1)
	else:
		if ghost_overlay:
			ghost_overlay.queue_free()
			ghost_overlay = null

func _reset_save():
	_clear_runtime_state()
	# Save empty state
	_do_auto_save()
	
	# Fresh start - show egg selection, keep settings
	show_egg_selection()
