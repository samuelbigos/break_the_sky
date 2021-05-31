extends Node2D

export var Lifetime = 5.0
export var AttractRange = 75.0

var _player = null
var _lifetime: float
var _colour: Color


func _ready():
	_lifetime = Lifetime
	_colour = Colours.Secondary
	
func _process(delta):
	var time = OS.get_system_time_msecs() / 500.0
	var s = 1.0 + sin(time) * 0.2
	scale = Vector2(s, s)
	rotation = sin(time) * 1.0
	
	_lifetime -= delta
	if _lifetime < Lifetime * 0.5:
		if int(_lifetime * lerp(100.0, 1.0, _lifetime	 / Lifetime)) % 10 < 7:
			_colour = Colours.Secondary
		else:
			_colour = Colours.White
		update()
		
	if _lifetime < 0.0:
		queue_free()
		
	# attract
	var dist = (_player.global_position - global_position).length()
	if dist < AttractRange:
		global_position += (_player.global_position - global_position).normalized() * (1.0 - (dist / AttractRange)) * delta * 150.0
	
func _draw():
	drawArc(Vector2(0.0, 0.0), 10.0, 0.0, 360.0, _colour, 3.0)
	draw_line(Vector2(-5.0, 0.0), Vector2(5.0, 0.0), _colour, 3.0)
	draw_line(Vector2(0.0, -5.0), Vector2(0.0, 5.0), _colour, 3.0)
	
func drawArc(center, radius, angleTo, angleFrom, color, thickness):
	var pointNum = 8
	var points = PoolVector2Array()
	for i in range(pointNum + 1):
		var angle = deg2rad(angleFrom + i * (angleTo - angleFrom) / pointNum - 90)
		points.push_back(center + Vector2(cos(angle), sin(angle)) * radius)
	for i in range(pointNum):
		draw_line(points[i], points[i + 1], color, thickness)
