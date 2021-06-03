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
onready var _wave = get_node("Wave")
onready var _subWave = get_node("Subwave")

onready var _loseScreen = get_node("LoseScreen")
onready var _loseButton = get_node("LoseScreen/VBoxContainer/MenuButton")
onready var _loseLabel = get_node("LoseScreen/VBoxContainer/MenuButton/Outline/Label")
onready var _loseOutline = get_node("LoseScreen/VBoxContainer/MenuButton/Outline")
onready var _loseBG = get_node("LoseScreen/VBoxContainer/MenuButton/TextureRect")

onready var _star1 = get_node("LoseScreen/Stars/Star/StarInner")
onready var _star2 = get_node("LoseScreen/Stars/Star2/StarInner")
onready var _star3 = get_node("LoseScreen/Stars/Star3/StarInner")
onready var _star4 = get_node("LoseScreen/Stars/Star4/StarInner")
onready var _star5 = get_node("LoseScreen/Stars/Star5/StarInner")

onready var _scoreLabel = get_node("LoseScreen/VBoxContainer/Score")

export var FloatingScoreScene: PackedScene
export var ScoreCountTime = 5.0
export var StarThreshold1 = 1000
export var StarThreshold2 = 3000
export var StarThreshold3 = 8000
export var StarThreshold4 = 15000
export var StarThreshold5 = 25000

var _perkSelections = []
var _buttons = []
var _buttonSelected = null
var _animTime: float
var _game = null
var _waveShowTimer: float
var _resultsScore: int
var _scoreCountTimer: float

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
	_loseLabel.add_color_override("font_color", fontCol)
	
	fontCol = Colours.Secondary
	_score.add_color_override("font_color", fontCol)
	_scoreMulti.add_color_override("font_color", fontCol)
	_wave.add_color_override("font_color", Colours.White)
	_subWave.add_color_override("font_color", fontCol)
	
	var bgCol = Colours.Secondary
	var outlineCol = Colours.Tertiary
	_perkBG1.modulate = bgCol
	_perkOutline1.modulate = outlineCol
	_perkBG2.modulate = bgCol
	_perkOutline2.modulate = outlineCol
	_perkBG3.modulate = bgCol
	_perkOutline3.modulate = outlineCol
	
	_loseBG.modulate = bgCol
	_loseOutline.modulate = outlineCol
	
	_buttons.append(_perkButton1)
	_buttons.append(_perkButton2)
	_buttons.append(_perkButton3)
	_buttons.append(_loseButton)
	
func _process(delta):
	_animTime += delta
	for button in _buttons:
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
			
	if _loseScreen.visible:
		var time = _animTime / 0.1
		var s = 1.0 + cos(time) * 0.25
		var r = sin(time) * 2.0
		#_scoreLabel.rect_pivot_offset = _scoreLabel.rect_size / 2.0
		#_scoreLabel.rect_rotation = r
		#_scoreLabel.rect_scale = Vector2(s, s)
		
		_scoreCountTimer -= delta
		var score = clamp(lerp(_resultsScore, 0, _scoreCountTimer / ScoreCountTime), 0, _resultsScore)
		_scoreLabel.text = "%06d" % score
		
		if not _star1.visible and score > StarThreshold1:
			$PerkSelect.play()
			_star1.visible = true
			
		if not _star2.visible and score > StarThreshold2:
			$PerkSelect.play()
			_star2.visible = true
			
		if not _star3.visible and score > StarThreshold3:
			$PerkSelect.play()
			_star3.visible = true
			
		if not _star4.visible and score > StarThreshold4:
			$PerkSelect.play()
			_star4.visible = true
			
		if not _star5.visible and score > StarThreshold5:
			$PerkSelect.play()
			_star5.visible = true
		
	_waveShowTimer -= delta
	if _waveShowTimer < 0.0:
		_wave.visible = false
		
func showFloatingScore(score: int, worldPos: Vector2, game):
	var floatingScore = FloatingScoreScene.instance()
	floatingScore.setScore(score)
	floatingScore.worldPos = worldPos
	game.add_child(floatingScore)
			
func setScore(var score: int, var multi: float, threshold: int, isMax: bool):
	if isMax:
		$ScoreMulti.text = "MAX x" + "%.1f" % multi
	else:
		$ScoreMulti.text = "x" + "%.1f" % multi
	$Score.text = "%06d" % score
	_resultsScore = score
	
func setWave(wave: int, subwave: int):
	_wave.text = "Wave %d" % (wave + 1)
	_waveShowTimer = 2.0
	_wave.visible = true
	#_subWave.text = "SubWave %d" % subwave	

func showPerks(perks):
	_perks.visible = true
	_perkSelections = perks
	_perkLabel1.text = _perkSelections[0].displayName
	_perkLabel2.text = _perkSelections[1].displayName
	_perkLabel3.text = _perkSelections[2].displayName
	_perkLabel1Desc.text = _perkSelections[0].displayDesc
	_perkLabel2Desc.text = _perkSelections[1].displayDesc
	_perkLabel3Desc.text = _perkSelections[2].displayDesc
	
func showLoseScreen():
	_loseScreen.visible = true
	_scoreLabel.text = "%06d" % 000000
	_scoreCountTimer = ScoreCountTime

func _on_Perk1_button_up():
	_perks.visible = false
	emit_signal("onPerkSelected", _perkSelections[0])
	$PerkSelect.play()

func _on_Perk2_button_up():
	_perks.visible = false
	emit_signal("onPerkSelected", _perkSelections[1])
	$PerkSelect.play()

func _on_Perk3_button_up():
	_perks.visible = false
	emit_signal("onPerkSelected", _perkSelections[2])
	$PerkSelect.play()

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

func _on_MenuButton_button_up():
	get_tree().paused = false
	get_tree().reload_current_scene()
	
func _on_MenuButton_mouse_entered():
	_buttonSelected = _loseButton

func _on_MenuButton_mouse_exited():
	_buttonSelected = null
