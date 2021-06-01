extends Node

var _SAVE_KEY = "player_data"
var _data = {}

var _currentLevel = 0 setget , getCurrentLevel
func getCurrentLevel(): return _data["current_level"]

func _init():
	add_to_group("persistent")
	_doCreateNew()
	
func _doCreateNew():
	_data["current_level"] = 0
	_data["level_record"] = {}
	_data["music"] = 5
	_data["effects"] = 5
	
func reset():
	_doCreateNew()
	
func set(key, val):
	if _data.has(key):
		_data[key] = val
		
func get(key):
	if _data.has(key):
		return _data[key]
		
func startLevel(level):
	_data["current_level"] = level
	
func completeLevel(level, score, time):
	if _data["current_level"] == level:
		_data["current_level"] += 1
		
	if not _data["level_record"].has(level):
		_data["level_record"][level] = {}
		_data["level_record"][level]["score"] = 0
		
	var record = _data["level_record"][level]
	record["score"] = max(record["score"], score)
	_data["level_record"][level] = record
	
	SaveManager.doSave()
	
func hasFinishedGame():
	return _data["current_level"] >= 3
		
func hasLevelRecord(level_id):
	return _data["level_record"].has(level_id)
		
func getLevelRecord(level_id):
	return _data["level_record"][level_id]
	
func doSave():
	var save_data = {}
	
	# save misc data
	save_data["data"] = _data.duplicate(true)
	return save_data
	
func doLoad(save_data : Dictionary):
	_doCreateNew()
	_data = save_data["data"].duplicate(true)
