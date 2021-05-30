extends Node2D

export var Lifetime = 5.0


func _ready():
	pass
	
func _process(delta):
	var time = OS.get_system_time_msecs() / 500.0
	var s = 1.0 + sin(time) * 0.2
	scale = Vector2(s, s)
	rotation = sin(time) * 1.0
	
	Lifetime -= delta
	if Lifetime < 0.0:
		queue_free()
	
func _draw():
	drawArc(Vector2(0.0, 0.0), 8.0, 0.0, 360.0, Color.white, 2.0)
	draw_line(Vector2(-3.0, 0.0), Vector2(3.0, 0.0), Color.white, 2.0)
	draw_line(Vector2(0.0, -3.0), Vector2(0.0, 3.0), Color.white, 2.0)
	
func drawArc(center, radius, angleTo, angleFrom, color, thickness):
	var pointNum = 8
	var points = PoolVector2Array()
	for i in range(pointNum + 1):
		var angle = deg2rad(angleFrom + i * (angleTo - angleFrom) / pointNum - 90)
		points.push_back(center + Vector2(cos(angle), sin(angle)) * radius)
	for i in range(pointNum):
		draw_line(points[i], points[i + 1], color, thickness)
