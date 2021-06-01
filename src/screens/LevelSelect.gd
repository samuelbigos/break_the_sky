extends CanvasLayer

export var LevelWidgetScene: PackedScene


func _ready():
	for i in range(0, Levels.Levels.size()):
		var widget = LevelWidgetScene.instance()
		widget.setTitle(Levels.Levels[i]["title"])
		widget.connect("onSelected", self, "_onAct" + "%d" % (i + 1) + "Selected")
		
		if i > PlayerData.getCurrentLevel():
			widget.setDisabled()
		else:
			if PlayerData.hasLevelRecord(i) and i != 0:
				var record = PlayerData.getLevelRecord(i)
				widget.setScore("%06d" % record["score"])
				widget.setPerks(["P1", "P2", "P3"])
			else:
				widget.setScore("-")
				widget.setPerks(["-", "-", "-"])
			
		$CenterContainer/GridContainer.add_child(widget)
		
		if PlayerData.getCurrentLevel() == i:
			widget.setCurrent()

	
func _onAct1Selected():
	PlayerData.startLevel(0)
	
func _onAct2Selected():
	pass
	
func _onAct3Selected():
	pass
