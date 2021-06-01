extends NinePatchRect

export var PerkFont: DynamicFont

var selected = false
var _time = 0.0
var _enabled = true

signal onSelected


func setTitle(title: String):
	$MarginContainer/VBoxContainer/Title.text = title
	
func setScore(score: String):
	$MarginContainer/VBoxContainer/HBoxContainer/Score.text = score
	
func setCurrent():
	pass
	
func setDisabled():
	$MarginContainer.visible = false
	_enabled = false
	
func setPerks(perks):
	for perk in perks:
		var label = Label.new()
		label.align = Label.ALIGN_RIGHT
		label.text = perk
		label.set("custom_fonts/font", PerkFont)
		$MarginContainer/VBoxContainer/Perks.add_child(label)

func _process(delta):
	_time += delta * 10.0
	if selected and _enabled:
		var s = 1.0 + sin(_time) * 0.05
		var r = cos(_time) * 0.5
		rect_pivot_offset = rect_size / 2.0
		rect_rotation = r
		rect_scale = Vector2(s, s)
	else:
		rect_rotation = 0.0
		rect_scale = Vector2(1.0, 1.0)
		
	if get_global_rect().has_point(get_global_mouse_position()) and Input.is_action_just_released("shoot"):
		emit_signal("onSelected")

func _on_NinePatchRect_mouse_entered():
	selected = true
	_time = 0.0

func _on_NinePatchRect_mouse_exited():
	selected = false
