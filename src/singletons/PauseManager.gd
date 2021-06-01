extends Node

var _game = null
var _pauseTimer: float
var _paused = false
var _pauseFlashTime = 1.0 / 60.0


func pauseFlash():
	get_tree().paused = true
	_pauseTimer = _pauseFlashTime
	_paused = true
	
func _process(delta):
	if _paused and not _game._gui.showingPerks():
		_pauseTimer -= delta
		if _pauseTimer < 0.0:
			get_tree().paused = false
			_paused = false
