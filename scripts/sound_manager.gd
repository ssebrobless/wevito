class_name SoundManager
extends Node

const MALE_PITCH_MULTIPLIER = 0.8
var audio_player: AudioStreamPlayer
var sound_enabled: bool = true

const ANIMAL_SOUNDS = {
	"rat": {
		"idle": [[880, 0.1], [1100, 0.1, 0.08]],
		"feed": [[440, 0.15], [550, 0.1, 0.1], [660, 0.15, 0.2]],
		"chew": [[600, 0.08], [650, 0.06, 0.05], [700, 0.08, 0.1]],
		"drink": [[300, 0.15], [280, 0.12, 0.1]],
		"rest": [[220, 0.5]],
		"pet": [[1200, 0.1], [1400, 0.15, 0.08]],
		"bathe": [[330, 0.2], [440, 0.15, 0.15]],
		"groom": [[700, 0.15]],
		"exercise": [[520, 0.1], [650, 0.1, 0.06]]
	},
	"crow": {
		"idle": [[440, 0.15], [550, 0.1, 0.12]],
		"feed": [[380, 0.12], [480, 0.15, 0.1], [440, 0.1, 0.2]],
		"chew": [[500, 0.1], [550, 0.08, 0.08]],
		"drink": [[350, 0.12], [320, 0.1, 0.1]],
		"rest": [[280, 0.4]],
		"pet": [[660, 0.12], [780, 0.1, 0.1]],
		"bathe": [[400, 0.2]],
		"groom": [[520, 0.12], [640, 0.1, 0.08]],
		"exercise": [[300, 0.1], [400, 0.1, 0.08]]
	},
	"fox": {
		"idle": [[320, 0.2], [400, 0.1, 0.15]],
		"feed": [[360, 0.15], [440, 0.12, 0.12], [380, 0.1, 0.22]],
		"chew": [[450, 0.1], [500, 0.08, 0.08]],
		"drink": [[250, 0.15], [230, 0.12, 0.1]],
		"rest": [[200, 0.5]],
		"pet": [[500, 0.15], [600, 0.12, 0.1]],
		"bathe": [[280, 0.2], [350, 0.15, 0.18]],
		"groom": [[420, 0.15]],
		"exercise": [[260, 0.15], [340, 0.12, 0.1]]
	},
	"snake": {
		"idle": [[150, 0.25]],
		"feed": [[200, 0.2], [180, 0.25, 0.15]],
		"chew": [[250, 0.15], [220, 0.12, 0.1]],
		"drink": [[150, 0.2], [140, 0.15, 0.12]],
		"rest": [[100, 0.6]],
		"pet": [[280, 0.15]],
		"bathe": [[220, 0.2]],
		"groom": [[300, 0.15]],
		"exercise": [[180, 0.2]]
	},
	"deer": {
		"idle": [[350, 0.15], [440, 0.1, 0.1]],
		"feed": [[280, 0.25]],
		"chew": [[400, 0.12], [450, 0.1, 0.1]],
		"drink": [[200, 0.18], [180, 0.15, 0.12]],
		"rest": [[180, 0.5]],
		"pet": [[480, 0.12], [560, 0.1, 0.1]],
		"bathe": [[240, 0.2], [300, 0.15, 0.16]],
		"groom": [[400, 0.15]],
		"exercise": [[320, 0.15], [400, 0.1, 0.1]]
	},
	"frog": {
		"idle": [[600, 0.1], [720, 0.12, 0.08]],
		"feed": [[480, 0.12], [560, 0.1, 0.1], [640, 0.1, 0.2]],
		"chew": [[550, 0.1], [600, 0.08, 0.08]],
		"drink": [[400, 0.12], [380, 0.1, 0.1]],
		"rest": [[200, 0.4]],
		"pet": [[800, 0.1], [900, 0.12, 0.08]],
		"bathe": [[400, 0.15], [500, 0.12, 0.12]],
		"groom": [[560, 0.12]],
		"exercise": [[360, 0.1], [440, 0.1, 0.08]]
	},
	"pigeon": {
		"idle": [[520, 0.15], [600, 0.1, 0.1]],
		"feed": [[440, 0.12], [520, 0.1, 0.1]],
		"chew": [[500, 0.1], [550, 0.08, 0.08]],
		"drink": [[300, 0.15], [280, 0.12, 0.1]],
		"rest": [[220, 0.4]],
		"pet": [[700, 0.12], [800, 0.1, 0.1]],
		"bathe": [[380, 0.2]],
		"groom": [[580, 0.12]],
		"exercise": [[400, 0.12], [480, 0.1, 0.1]]
	},
	"raccoon": {
		"idle": [[380, 0.15]],
		"feed": [[320, 0.12], [400, 0.1, 0.1], [360, 0.08, 0.18]],
		"chew": [[450, 0.1], [500, 0.08, 0.08]],
		"drink": [[280, 0.15], [260, 0.12, 0.1]],
		"rest": [[180, 0.5]],
		"pet": [[540, 0.12], [640, 0.1, 0.1]],
		"bathe": [[280, 0.18], [340, 0.12, 0.15]],
		"groom": [[480, 0.15]],
		"exercise": [[300, 0.15], [380, 0.1, 0.1]]
	},
	"squirrel": {
		"idle": [[720, 0.08], [840, 0.06, 0.06]],
		"feed": [[560, 0.1], [680, 0.08, 0.08], [620, 0.06, 0.15]],
		"chew": [[650, 0.08], [700, 0.06, 0.06]],
		"drink": [[400, 0.12], [380, 0.1, 0.08]],
		"rest": [[240, 0.4]],
		"pet": [[900, 0.1], [1000, 0.08, 0.08]],
		"bathe": [[500, 0.15]],
		"groom": [[760, 0.1]],
		"exercise": [[480, 0.08], [560, 0.06, 0.05], [640, 0.08, 0.1]]
	},
	"goose": {
		"idle": [[420, 0.18], [500, 0.1, 0.15]],
		"feed": [[340, 0.15], [420, 0.12, 0.12], [380, 0.1, 0.22]],
		"chew": [[480, 0.12], [520, 0.1, 0.1]],
		"drink": [[280, 0.18], [260, 0.15, 0.12]],
		"rest": [[200, 0.5]],
		"pet": [[580, 0.15], [680, 0.1, 0.1]],
		"bathe": [[320, 0.18], [400, 0.12, 0.15]],
		"groom": [[520, 0.15]],
		"exercise": [[280, 0.2], [360, 0.15, 0.15]]
	}
}

const HATCH_SOUNDS = [[523, 0.15], [659, 0.15, 0.1], [784, 0.15, 0.2], [1047, 0.3, 0.3]]
const GHOST_SOUNDS = [[440, 0.3], [330, 0.4, 0.2], [220, 0.5, 0.4]]

func _ready():
	audio_player = AudioStreamPlayer.new()
	add_child(audio_player)

func set_enabled(enabled: bool):
	sound_enabled = enabled

func play_sound(animal: String, action: String, gender: String = "female"):
	if not sound_enabled:
		return
	var animal_data = ANIMAL_SOUNDS.get(animal, {})
	var sounds = animal_data.get(action, [])
	if sounds.size() == 0:
		return
	_play_sounds_sequence(sounds, gender)

func play_hatch_sound(gender: String = "female"):
	if not sound_enabled:
		return
	_play_sounds_sequence(HATCH_SOUNDS, gender)

func play_ghost_sound():
	if not sound_enabled:
		return
	_play_sounds_sequence(GHOST_SOUNDS, "female")

func _play_sounds_sequence(sounds: Array, gender: String):
	var is_male = (gender == "male")
	for sound in sounds:
		var freq = sound[0]
		var dur = sound[1]
		var delay = sound[2] if sound.size() > 2 else 0.0
		if is_male:
			freq = int(freq * MALE_PITCH_MULTIPLIER)
		if delay > 0:
			await get_tree().create_timer(delay).timeout
		_play_tone(freq, dur)

func _play_tone(freq: float, duration: float):
	var sample_rate = 44100
	var num_samples = int(sample_rate * duration)
	var samples = PackedByteArray()
	samples.resize(num_samples)
	var amplitude = 0.3
	var two_pi = 6.28318530718
	for i in range(num_samples):
		var t = float(i) / sample_rate
		var value = sin(two_pi * freq * t)
		var sample = int(value * amplitude * 127)
		samples[i] = sample + 128
	var stream = AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_8_BITS
	stream.data = samples
	stream.loop_mode = AudioStreamWAV.LOOP_DISABLED
	audio_player.stream = stream
	audio_player.play()
	await get_tree().create_timer(duration).timeout
