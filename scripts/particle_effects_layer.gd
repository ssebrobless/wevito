extends Node2D
class_name ParticleEffectsLayer

const MAX_ACTIVE_PARTICLES := 18

func play_effect(effect_name: String, origin: Vector2 = Vector2.ZERO, facing: int = 1):
	match effect_name:
		"sleep":
			_spawn_text_burst(["z", "z", "z"], origin + Vector2(0, -42), Color(0.72, 0.86, 1.0, 0.95), Vector2(4, -28))
		"happy":
			_spawn_text_burst(["<3", "<3", "<3", "<3"], origin + Vector2(0, -28), Color(1.0, 0.58, 0.74, 0.95), Vector2(0, -24))
		"eat":
			_spawn_dot_burst(2, origin + Vector2(20 * facing, -10), Color(0.78, 0.55, 0.28, 0.95), Vector2(8 * facing, 12))
		"bathe":
			_spawn_text_burst(["o", "o", "o", "o", "o", "o"], origin + Vector2(0, -10), Color(0.72, 0.94, 1.0, 0.8), Vector2(0, -30))
		"sad":
			_spawn_text_burst([".", "."], origin + Vector2(12 * facing, -18), Color(0.52, 0.72, 1.0, 0.9), Vector2(0, 18))
		"walk":
			_spawn_dot_burst(1, origin + Vector2(-20 * facing, 18), Color(0.72, 0.65, 0.55, 0.45), Vector2(-8 * facing, -4))

func _spawn_text_burst(texts: Array, origin: Vector2, color: Color, drift: Vector2):
	for i in range(texts.size()):
		var label = Label.new()
		label.text = str(texts[i])
		label.modulate = color
		label.position = origin + Vector2((i - texts.size() * 0.5) * 8.0, randf_range(-3.0, 3.0))
		label.add_theme_font_size_override("font_size", 10)
		add_child(label)
		_trim_particles()
		_animate_particle(label, drift + Vector2(randf_range(-5.0, 5.0), randf_range(-5.0, 5.0)))

func _spawn_dot_burst(count: int, origin: Vector2, color: Color, drift: Vector2):
	for i in range(count):
		var dot = ColorRect.new()
		dot.color = color
		dot.size = Vector2(4, 4)
		dot.position = origin + Vector2(randf_range(-4.0, 4.0), randf_range(-4.0, 4.0))
		add_child(dot)
		_trim_particles()
		_animate_particle(dot, drift + Vector2(randf_range(-4.0, 4.0), randf_range(-4.0, 4.0)))

func _animate_particle(node: CanvasItem, drift: Vector2):
	var tween = create_tween()
	tween.tween_property(node, "position", node.position + drift, 0.55)
	tween.parallel().tween_property(node, "modulate:a", 0.0, 0.55)
	tween.tween_callback(func():
		if is_instance_valid(node):
			node.queue_free()
	)

func _trim_particles():
	while get_child_count() > MAX_ACTIVE_PARTICLES:
		var child = get_child(0)
		if child:
			child.queue_free()
