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
	
func pickPerk(perk):
	perk.maximum -= 1
		
func getRandomPerks(count: int):
	var perks = []
	for perk in _perks:
		if perk.maximum > 0:
			perks.append(perk)
			
	var ret = []
	while (ret.size() < count):
		var rand = randi() % perks.size()
		ret.append(perks[rand])
		perks.remove(rand)
	return ret
