extends Node2D

export var Width = 2.0

var alpha = 1.0
var boid = null

func _ready():
	z_index = -1


func _draw():
	var pointArray = PoolVector2Array()
	var colourArray = PoolColorArray()
	var numPoints = boid._trailPoints.size()
	for i in range(0, numPoints):
		pointArray.push_back(boid._trailPoints[i] - global_position)
		var col = Colours.White
		col.a = 0.25 + (float(i) / numPoints) * 0.75
		col.a *= alpha
		colourArray.push_back(col)
	pointArray.push_back(Vector2(0.0, 0.0))
	colourArray.push_back(Colours.White)
	draw_polyline_colors(pointArray, colourArray, Width)
