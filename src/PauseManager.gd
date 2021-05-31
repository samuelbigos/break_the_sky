extends Node

var _pauseTimer: float
var _paused = true
var _pauseFlashTime = 1.0 / 25.0


func pauseFlash():
	get_tree().paused = true
	_pauseTimer = _pauseFlashTime
	_paused = true
	
func _process(delta):
	if _paused:
		_pauseTimer -= delta
		if _pauseTimer < 0.0:
			get_tree().paused = false
