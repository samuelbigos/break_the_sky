extends Node2D
class_name Game

enum Formation {
	Balanced,
	Wide,
	Narrow,	
}

export var BoidScene : PackedScene
export var BoidCount = 100
export var BoidColumns = 10
export var BoidSpacing = 20

var _boidColumns = []
var _allBoids = []
var _player = null
var _formation: int = Formation.Balanced 

func getBoids(): return _allBoids
func getPlayer(): return _player

func _ready():
	_player = get_node("Leader")
	_player._game = self
	
	for i in range(0, BoidColumns):
		_boidColumns.append([])
		
	for i in range(0, BoidCount):
		var boid = BoidScene.instance()
		add_child(boid)
		_allBoids.append(boid)
		boid.init($Leader, self)
	
	changeFormation(Formation.Balanced, true)
		
func changeFormation(formation: int, setPos: bool):
	if (formation == Formation.Balanced):
		setColumns(int(sqrt(_allBoids.size())), setPos)
	if (formation == Formation.Wide):
		setColumns(int(sqrt(_allBoids.size())) * 2, setPos)
	if (formation == Formation.Narrow):
		setColumns(int(sqrt(_allBoids.size()) * 0.5), setPos)
		
func setColumns(numCols: int, setPos: bool):
	BoidColumns = numCols
	_boidColumns = []
	for i in range(0, BoidColumns):
		_boidColumns.append([])
		
	var perCol = _allBoids.size() / BoidColumns
	for i in range(0, _allBoids.size()):
		var boid = _allBoids[i]
		var column = int(i) / int(perCol)
		_boidColumns[column].append(boid)
		var columnIndex = _boidColumns[column].find(boid)
		var offset = getOffset(column, columnIndex)
		boid.setOffset(offset)
		boid._colour = _indexToCol(column)
		
		if setPos:
			boid.global_position = _player.global_position + offset
	
func getOffset(column: int, columnIndex: int):
	column -= _boidColumns.size() * 0.5 - (_boidColumns.size() % 2 * 0.5)
	var perCol = int(_allBoids.size() / _boidColumns.size())
	columnIndex -= perCol * 0.5 - (perCol % 2 * 0.5)
	var offset = Vector2(column * BoidSpacing, columnIndex * BoidSpacing)
	offset += Vector2(0.5 * ((_boidColumns.size() + 1) % 2), 0.5 * ((perCol + 1) % 2)) * BoidSpacing
	return offset
	
func _process(delta: float):
	pass

func pushBack(boid: Object):
	for i in range(0, BoidColumns):
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
