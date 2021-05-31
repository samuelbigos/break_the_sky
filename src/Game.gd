extends Node2D
class_name Game

enum Formation {
	Balanced,
	Wide,
	Narrow,	
}

export var Debug = false

export var BoidScene : PackedScene
export var PickupAddScene: PackedScene
export var InitialPickupAddCount := 20
export var InitialBoidCount = 100
export var PlayRadius = 1000.0

export var BaseBoidReload = 1.75
export var BaseBoidReinforce = 3
export var BaseBoidGrouping = 25.0
export var BaseBoidDamage = 1.0
export var BaseSlowmoCD = 60.0
export var BaseNukeCD = 120.0
export var BaseBoidSpeed = 1500.0
export var BasePlayerSpeed = 4.0
export var BaseBoidSpread = 0.1
export var BaseBulletSpeed = 500.0

export var EnemyDrillerScene : PackedScene
export var DrillerFirstSpawn = 0.0
export var DrillerSpawnRate = 2.0
export var DrlllerSpawnRateMulti = 0.99

export var EnemyLaserScene: PackedScene
export var LaserSpawnScore = 300.0
export var LaserSpawnRate = 8.0
export var LaserSpawnRateMulti = 0.97

export var EnemyBeaconScene: PackedScene
export var BeaconSpawnScore = 1500.0
export var BeaconSpawnRate = 20.0
export var BeaconSpawnRateMulti = 0.95

export var ScoreMultiTimeout = 10.0
export var ScoreMultiMax = 25

var _boidColCount: int
var _boidColumns = []
var _allBoids = []
var _player = null
var _formation: int = Formation.Balanced
var _pickups = []
var _spawnPickups = 0
var _started = false
var _score = 0
var _scoreMulti = 0.8
var _scoreMultiTimer: float

var _hasSlowmo = false
var _hasNuke = false

var _time := 0.0
var _drillerSpawn: float
var _laserSpawn: float
var _beaconSpawn: float

onready var _gui = get_node("CanvasLayer")
onready var _perks = get_node("PerkManager")

func getBoids(): return _allBoids
func getPlayer(): return _player

func _ready():
	_player = get_node("Leader")
	_player._game = self
		
	for i in range(0, InitialBoidCount):
		var boid = BoidScene.instance()
		add_child(boid)
		_allBoids.append(boid)
		boid.init($Leader, self)
	
	if _allBoids.size() > 0:
		changeFormation(Formation.Balanced, true)
	
	# spawn pickups
	for i in range(0, InitialPickupAddCount):
		var f = float(i) * PI * 2.0 / float(InitialPickupAddCount)
		spawnPickupAdd(Vector2(sin(f), -cos(f)).normalized() * 80.0, true)
		
	_drillerSpawn = DrillerFirstSpawn
	if Debug:
		_started = true
		for pickup in _pickups:
			pickup.queue_free()
			addBoids(Vector2(0.0, 0.0))
		#DrillerFirstSpawn = 999.0
		#LaserSpawnScore = 0.0
		#BeaconSpawnScore = 0.0
		
	addScore(0)
	randomize()
	
	$Background._game = self
	$Background.init()
	GlobalCamera._player = _player
	PauseManager._game = self
		
func changeFormation(formation: int, setPos: bool):
	if _allBoids.size() == 0:
		return
		
	if (formation == Formation.Balanced):
		setColumns(int(sqrt(_allBoids.size()) + 0.5), setPos)
	if (formation == Formation.Wide):
		setColumns(int(sqrt(_allBoids.size()) + 0.5) * 2, setPos)
	if (formation == Formation.Narrow):
		setColumns(int(sqrt(_allBoids.size() + 0.5) * 0.5), setPos)
	_formation = formation
		
func setColumns(numCols: int, setPos: bool):
	_boidColCount = clamp(numCols, 0, _allBoids.size())
	_boidColumns = []
	for i in range(0, _boidColCount):
		_boidColumns.append([])
		
	var perCol = _allBoids.size() / _boidColCount
	for i in range(0, _allBoids.size()):
		var boid = _allBoids[i]
		var column = int(i) / int(perCol)
		var colIdx = column
		if colIdx >= _boidColumns.size():
			colIdx = i % _boidColCount
		_boidColumns[colIdx].append(boid)
		var columnIndex = _boidColumns[colIdx].find(boid)
		var offset = getOffset(colIdx, columnIndex)
		boid.setOffset(offset)
		
		if setPos:
			boid.global_position = _player.global_position + offset
			
func addBoids(pos: Vector2):
	for i in range(0, BaseBoidReinforce):
		var boid = BoidScene.instance()
		add_child(boid)
		_allBoids.append(boid)
		boid.init($Leader, self)
		boid.global_position = pos
		
	InitialPickupAddCount -= 1
	if InitialPickupAddCount == 0:
		start()
		
	changeFormation(_formation, false)
		
func removeBoid(boid: Object):
	_allBoids.erase(boid)
	if _allBoids.size() == 0:
		lose()
		
	changeFormation(_formation, false)
	
func spawnPickupAdd(pos: Vector2, persistent: bool):
	var pickup = PickupAddScene.instance()
	pickup.global_position = pos
	pickup._player = _player
	if persistent:
		pickup.Lifetime = 9999999.0
	add_child(pickup)
	_pickups.append(pickup)
		
func start():
	_started = true
	
func getOffset(column: int, columnIndex: int):
	column -= _boidColumns.size() * 0.5 - (_boidColumns.size() % 2 * 0.5)
	var perCol = int(_allBoids.size() / _boidColumns.size())
	columnIndex -= perCol * 0.5 - (perCol % 2 * 0.5)
	var offset = Vector2(column * BaseBoidGrouping, columnIndex * BaseBoidGrouping)
	offset += Vector2(0.5 * ((_boidColumns.size() + 1) % 2), 0.5 * ((perCol + 1) % 2)) * BaseBoidGrouping
	return offset
	
func _process(delta: float):
	_scoreMultiTimer -= delta
	if _scoreMultiTimer < 0.0:
		_scoreMulti = 1
			
	# enemy spawn
	_time += delta
	if _started:
		# driller
		if _time > DrillerFirstSpawn:
			_drillerSpawn -= delta
			if _drillerSpawn < 0.0:
				var driller = EnemyDrillerScene.instance()
				var f = rand_range(0.0, PI * 2.0)
				driller.global_position = Vector2(sin(f), -cos(f)).normalized() * PlayRadius
				add_child(driller)
				driller.init(self, _player)
				_drillerSpawn = DrillerSpawnRate
				DrillerSpawnRate *= DrlllerSpawnRateMulti
		
		# laser
		if _score >= LaserSpawnScore:
			_laserSpawn -= delta
			if _laserSpawn < 0.0:
				var laser = EnemyLaserScene.instance()
				var f = rand_range(0.0, PI * 2.0)
				laser.global_position = Vector2(sin(f), -cos(f)).normalized() * PlayRadius
				add_child(laser)
				laser.init(self, _player)
				_laserSpawn = LaserSpawnRate
				LaserSpawnRate *= LaserSpawnRateMulti
				
		# beacon
		if _score >= BeaconSpawnScore:
			_beaconSpawn -= delta
			if _beaconSpawn < 0.0:
				var beacon = EnemyBeaconScene.instance()
				var f = rand_range(0.0, PI * 2.0)
				beacon.global_position = Vector2(sin(f), -cos(f)).normalized() * PlayRadius
				add_child(beacon)
				beacon.init(self, _player)
				_beaconSpawn = BeaconSpawnRate
				BeaconSpawnRate *= BeaconSpawnRateMulti
				
func addScore(var score: int):
	_score += score * _scoreMulti
	_scoreMulti = clamp(_scoreMulti + 0.1, 0, ScoreMultiMax)
	_scoreMultiTimer = ScoreMultiTimeout
	
	if _perks.thresholdReached(_score):
		doPerk()
		
	_gui.setScore(_score, _scoreMulti, _perks.getNextThreshold())
	
func doPerk():
	get_tree().paused = true
	_gui.showPerks(_perks.getRandomPerks(3))
	_gui.connect("onPerkSelected", self, "onPerkSelected")
	
func onPerkSelected(perk):
	get_tree().paused = false
	BaseBoidReload *= perk.reloadMod
	BaseBoidReinforce += perk.reinforceMod
	BaseBoidGrouping += perk.groupingMod
	BaseBoidDamage += perk.damageMod
	BaseSlowmoCD *= perk.slowmoMod
	BaseNukeCD *= perk.nukeMod
	BaseBoidSpeed += perk.boidSpeedMod
	BasePlayerSpeed += perk.playerSpeedMod
	BaseBoidSpread *= perk.spreadMod
	BaseBulletSpeed += perk.bulletSpeedMod
	changeFormation(_formation, false)
	
func pushBack(boid: Object):
	for i in range(0, _boidColCount):
		if _boidColumns[i].has(boid):
			_boidColumns[i].erase(boid)
			_boidColumns[i].insert(0, boid)
			for j in range(0, _boidColumns[i].size()):
				_boidColumns[i][j].setOffset(getOffset(i, j))
			break

func lose():
	get_tree().reload_current_scene()
