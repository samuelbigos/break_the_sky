extends Node

export var PerkThreshold = 350
export var PerkThresholdMulti = 1.33

var _perkLevel = 1
var _perks = []


func _ready():
	for perk in get_children():
		if perk.enabled:
			_perks.append(perk)
		
func thresholdReached(score: int):
	if score >= getNextThreshold():
		_perkLevel += 1
		return true
	return false
	
func getNextThreshold():
	var threshold = 0
	for i in range(0, _perkLevel):
		threshold += PerkThreshold * pow(PerkThresholdMulti, i)
	return threshold
		
func getRandomPerks(count: int):
	var perks = _perks.duplicate()
	var ret = []
	while (ret.size() < count):
		var rand = randi() % perks.size()
		ret.append(perks[rand])
		perks.remove(rand)
	return ret
