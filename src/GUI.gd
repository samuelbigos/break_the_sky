extends CanvasLayer

onready var _perks = get_node("Perks")
onready var _perkLabel1 = get_node("Perks/VBoxContainer/Perk1/Outline/Label")
onready var _perkLabel2 = get_node("Perks/VBoxContainer/Perk2/Outline/Label")
onready var _perkLabel3 = get_node("Perks/VBoxContainer/Perk3/Outline/Label")
onready var _perkLabel1Desc = get_node("Perks/VBoxContainer/Perk1/Outline/Desc")
onready var _perkLabel2Desc = get_node("Perks/VBoxContainer/Perk2/Outline/Desc")
onready var _perkLabel3Desc = get_node("Perks/VBoxContainer/Perk3/Outline/Desc")
onready var _perkButton1 = get_node("Perks/VBoxContainer/Perk1")
onready var _perkButton2 = get_node("Perks/VBoxContainer/Perk2")
onready var _perkButton3 = get_node("Perks/VBoxContainer/Perk3")

onready var _perkBG1 = get_node("Perks/VBoxContainer/Perk1/TextureRect")
onready var _perkOutline1 = get_node("Perks/VBoxContainer/Perk1/Outline")
onready var _perkBG2 = get_node("Perks/VBoxContainer/Perk2/TextureRect")
onready var _perkOutline2 = get_node("Perks/VBoxContainer/Perk2/Outline")
onready var _perkBG3 = get_node("Perks/VBoxContainer/Perk3/TextureRect")
onready var _perkOutline3 = get_node("Perks/VBoxContainer/Perk3/Outline")

onready var _perkAt = get_node("PerkIn")
onready var _score = get_node("Score")
onready var _scoreMulti = get_node("ScoreMulti")
onready var _nextPerk = get_node("NextPerk")

var _perkSelections = []
var _perkButtons = []
var _buttonSelected = null
var _animTime: float

signal onPerkSelected


func showingPerks(): return _perks.visible

func _ready():
	var fontCol = Colours.White
	_perkLabel1.add_color_override("font_color", fontCol)
	_perkLabel2.add_color_override("font_color", fontCol)
	_perkLabel3.add_color_override("font_color", fontCol)
	_perkLabel1Desc.add_color_override("font_color", fontCol)
	_perkLabel2Desc.add_color_override("font_color", fontCol)
	_perkLabel3Desc.add_color_override("font_color", fontCol)
	
	fontCol = Colours.Secondary
	_score.add_color_override("font_color", fontCol)
	_scoreMulti.add_color_override("font_color", fontCol)
	_nextPerk.add_color_override("font_color", fontCol)
	_perkAt.add_color_override("font_color", fontCol)
	
	var bgCol = Colours.Secondary
	var outlineCol = Colours.Tertiary
	_perkBG1.modulate = bgCol
	_perkOutline1.modulate = outlineCol
	_perkBG2.modulate = bgCol
	_perkOutline2.modulate = outlineCol
	_perkBG3.modulate = bgCol
	_perkOutline3.modulate = outlineCol
	
	_perkButtons.append(_perkButton1)
	_perkButtons.append(_perkButton2)
	_perkButtons.append(_perkButton3)
	
func _process(delta):
	_animTime += delta
	for button in _perkButtons:
		var bg = button.get_node("TextureRect")
		var outline = button.get_node("Outline")
		if button == _buttonSelected:
			var time = _animTime / 0.1
			var s = 1.0 + cos(time) * 0.05
			var r = sin(time) * 0.5
			bg.rect_pivot_offset = bg.rect_size / 2.0
			bg.rect_scale = Vector2(s, s)
			bg.rect_rotation = r
			outline.rect_pivot_offset = outline.rect_size / 2.0
			outline.rect_scale = Vector2(s, s)
			outline.rect_rotation = r
		else:
			bg.rect_scale = Vector2(1.0, 1.0)
			bg.rect_rotation = 0.0
			outline.rect_scale = Vector2(1.0, 1.0)
			outline.rect_rotation = 0.0
			
func setScore(var score: int, var multi: float, threshold: int):
	$Score.text = "%06d" % score
	$ScoreMulti.text = "x" + "%.1f" % multi
	$NextPerk.text = "%d" % threshold

func showPerks(perks):
	_perks.visible = true
	_perkSelections = perks
	_perkLabel1.text = _perkSelections[0].displayName
	_perkLabel2.text = _perkSelections[1].displayName
	_perkLabel3.text = _perkSelections[2].displayName
	_perkLabel1Desc.text = _perkSelections[0].displayDesc
	_perkLabel2Desc.text = _perkSelections[1].displayDesc
	_perkLabel3Desc.text = _perkSelections[2].displayDesc

func _on_Perk1_button_up():
	_perks.visible = false
	emit_signal("onPerkSelected", _perkSelections[0])

func _on_Perk2_button_up():
	_perks.visible = false
	emit_signal("onPerkSelected", _perkSelections[1])

func _on_Perk3_button_up():
	_perks.visible = false
	emit_signal("onPerkSelected", _perkSelections[2])

func _on_Perk1_mouse_entered():
	_animTime = 0.0
	_buttonSelected = _perkButton1

func _on_Perk1_mouse_exited():
	_buttonSelected = null

func _on_Perk2_mouse_entered():
	_animTime = 0.0
	_buttonSelected = _perkButton2

func _on_Perk2_mouse_exited():
	_buttonSelected = null

func _on_Perk3_mouse_entered():
	_animTime = 0.0
	_buttonSelected = _perkButton3

func _on_Perk3_mouse_exited():
	_buttonSelected = null
