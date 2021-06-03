extends Button

var _hover = false
var _time = 0.0


func _ready():
	pass

func _process(delta):
	_time += delta
	if _hover:
		var time = _time / 0.1
		var s = 1.0 + cos(time) * 0.05
		var r = sin(time) * 0.5
		$TextureRect.rect_pivot_offset = $TextureRect.rect_size / 2.0
		$TextureRect.rect_scale = Vector2(s, s)
		$TextureRect.rect_rotation = r
	else:
		$TextureRect.rect_scale = Vector2(1.0, 1.0)
		$TextureRect.rect_rotation = 0.0


func _on_Button_mouse_entered():
	_hover = true
	_time = 0.0

func _on_Button_mouse_exited():
	_hover = false
