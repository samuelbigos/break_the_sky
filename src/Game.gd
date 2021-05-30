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
export var EnemyDrillerScene : PackedScene
export var InitialPickupAddCount := 20
export var InitialBoidCount = 100
export var BoidSpacing = 20
export var PlayRadius = 1000.0

export var DrillerFirstSpawn = 0.0
export var DrillerSpawnRate = 2.0
export var DrlllerSpawnRateMulti = 0.99

export var ScoreMultiTimeout = 10.0

var _boidColCount: int
var _boidColumns = []
var _allBoids = []
var _player = null
var _formation: int = Formation.Balanced
var _pickups = []
var _spawnPickups = 0
var _started = false
var _score = 0
var _scoreMulti = 1
var _scoreMultiTimer: float

var _time := 0.0
var _drillerSpawn: float

onready var _gui = get_node("CanvasLayer")

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
		spawnPickupAdd(Vector2(sin(f), -cos(f)).normalized() * 60.0, true)
		
	_drillerSpawn = DrillerFirstSpawn
	if Debug:
		_started = true
		for pickup in _pickups:
			pickup.queue_free()
			addBoid(Vector2(0.0, 0.0))
		
func changeFormation(formation: int, setPos: bool):
	if (formation == Formation.Balanced):
		setColumns(int(sqrt(_allBoids.size()) + 0.5), setPos)
	if (formation == Formation.Wide):
		setColumns(int(sqrt(_allBoids.size()) + 0.5) * 2, setPos)
	if (formation == Formation.Narrow):
		setColumns(int(sqrt(_allBoids.size() + 0.5) * 0.5), setPos)
	_formation = formation
		
func setColumns(numCols: int, setPos: bool):
	_boidColCount = numCols
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
		boid._colour = _indexToCol(colIdx)
		
		if setPos:
			boid.global_position = _player.global_position + offset
			
func addBoid(pos: Vector2):
	var boid = BoidScene.instance()
	add_child(boid)
	_allBoids.append(boid)
	boid.init($Leader, self)
	boid.global_position = pos
	changeFormation(_formation, false)
	InitialPickupAddCount -= 1
	if InitialPickupAddCount == 0:
		start()
		
func removeBoid(boid: Object):
	_allBoids.erase(boid)
	changeFormation(_formation, false)
	
func spawnPickupAdd(pos: Vector2, persistent: bool):
	var pickup = PickupAddScene.instance()
	pickup.global_position = pos
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
	var offset = Vector2(column * BoidSpacing, columnIndex * BoidSpacing)
	offset += Vector2(0.5 * ((_boidColumns.size() + 1) % 2), 0.5 * ((perCol + 1) % 2)) * BoidSpacing
	return offset
	
func _process(delta: float):
	# camera
	var camera_mouse_offset = get_global_mouse_position() - _player.global_position
	var camera_offset = -_player.global_position + get_viewport().size * 0.5 - camera_mouse_offset * 0.25	
	var camera_transform = Transform2D(Vector2(1.0, 0.0), Vector2(0.0, 1.0), camera_offset)
	get_viewport().canvas_transform = camera_transform
	
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
				
func addScore(var score: int):
	_score += score * _scoreMulti
	_scoreMulti += 1
	_scoreMultiTimer = ScoreMultiTimeout
	_gui.setScore(_score, _scoreMulti)
	
func pushBack(boid: Object):
	for i in range(0, _boidColCount):
		if _boidColumns[i].has(boid):
			_boidColumns[i].erase(boid)
			_boidColumns[i].insert(0, boid)
			for j in range(0, _boidColumns[i].size()):
				_boidColumns[i][j].setOffset(getOffset(i, j))
			break

func _indexToCol(i: int):
	if i == 0: return Color.red
	if i == 1: return Color.blue
	if i == 2: return Color.green
	if i == 3: return Color.aqua
	if i == 4: return Color.pink
	if i == 5: return Color.maroon
	if i == 6: return Color.magenta
	if i == 7: return Color.black
	if i == 8: return Color.yellow
	if i == 9: return Color.chocolate
	return Color.white

func _draw():
	drawArc(Vector2(0.0, 0.0), PlayRadius, 0.0, 360.0, Color.white, 3.0, 128)
	
func drawArc(center, radius, angleTo, angleFrom, color, thickness, segments):
	var pointNum = segments
	var points = PoolVector2Array()
	for i in range(pointNum + 1):
		var angle = deg2rad(angleFrom + i * (angleTo - angleFrom) / pointNum - 90)
		points.push_back(center + Vector2(cos(angle), sin(angle)) * radius)
	for i in range(pointNum):
		draw_line(points[i], points[i + 1], color, thickness)

func lose():
	get_tree().reload_current_scene()
