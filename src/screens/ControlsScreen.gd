extends CanvasLayer

export var GameScene: PackedScene

var _canProceed = false


func _ready():
	pass
	
func _process(delta):
	if Input.is_action_just_pressed("shoot"):
		_canProceed = true
	if Input.is_action_just_released("shoot") and _canProceed:
		get_tree().change_scene_to(GameScene)
