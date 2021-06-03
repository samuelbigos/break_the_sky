extends CanvasLayer

export var ControlsScreen: PackedScene

onready var _vBox = get_node("CenterContainer/VBoxContainer")

var _text = []
var _time: float
var _index := 0


func _ready():
	for child in _vBox.get_children():
		_text.append(child)
		child.visible = false
	
	MusicPlayer.playMenu()
		
func _process(delta):
	_time -= delta
	if _index < _text.size():
		if _time < 0.0:
			_text[_index].visible = true
			_time = 2.0
			_index += 1
	elif _time < 0.0:
		$Button.visible = true
		
	if _time > 0.0 and $Button.visible == false:
		_text[_index - 1].modulate.a = lerp(0.0, 1.0, 1.0 - (_time / 2.0))
	
	if Input.is_action_just_released("shoot"):
		for label in _text:
			label.visible = true
			label.modulate.a = 1.0
			_index = _text.size()
			$Button.visible = true

func _on_Button_pressed():
	get_tree().change_scene_to(ControlsScreen)
