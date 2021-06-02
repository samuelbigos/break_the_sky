extends Node2D

var lifetime = 1.5
var worldPos: Vector2


func setScore(score: int):
	$FloatingScore.text = "%d" % score

func _ready():
	$FloatingScore.add_color_override("font_color", Colours.Secondary)

func _process(delta):
	$FloatingScore.visible = true
	global_position = worldPos - Vector2(0.0, 20.0)
	lifetime -= delta
	if lifetime < 0.0:
		queue_free()
