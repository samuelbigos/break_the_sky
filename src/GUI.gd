extends CanvasLayer

onready var _perks = get_node("Perks")
onready var _perkLabel1 = get_node("Perks/VBoxContainer/Perk1/TextureRect/Label")
onready var _perkLabel2 = get_node("Perks/VBoxContainer/Perk2/TextureRect/Label")
onready var _perkLabel3 = get_node("Perks/VBoxContainer/Perk3/TextureRect/Label")
onready var _perkLabel1Desc = get_node("Perks/VBoxContainer/Perk1/TextureRect/Desc")
onready var _perkLabel2Desc = get_node("Perks/VBoxContainer/Perk2/TextureRect/Desc")
onready var _perkLabel3Desc = get_node("Perks/VBoxContainer/Perk3/TextureRect/Desc")
onready var _perkButton1 = get_node("Perks/VBoxContainer/Perk1")
onready var _perkButton2 = get_node("Perks/VBoxContainer/Perk2")
onready var _perkButton3 = get_node("Perks/VBoxContainer/Perk3")

onready var _perkAt = get_node("PerkIn")
onready var _score = get_node("Score")
onready var _scoreMulti = get_node("ScoreMulti")
onready var _nextPerk = get_node("NextPerk")

var _perkSelections = []

signal onPerkSelected


func _ready():
	var fontCol = Colours.Secondary
	_perkLabel1.add_color_override("font_color", fontCol)
	_perkLabel2.add_color_override("font_color", fontCol)
	_perkLabel3.add_color_override("font_color", fontCol)
	_perkLabel1Desc.add_color_override("font_color", fontCol)
	_perkLabel2Desc.add_color_override("font_color", fontCol)
	_perkLabel3Desc.add_color_override("font_color", fontCol)
	_score.add_color_override("font_color", fontCol)
	_scoreMulti.add_color_override("font_color", fontCol)
	_nextPerk.add_color_override("font_color", fontCol)
	_perkAt.add_color_override("font_color", fontCol)

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
