extends BoidEnemyBase
class_name BoidEnemyDriller


func _process(delta: float):
	pass	
	
func destroy(score: bool):
	.destroy(score)
	
func _draw():
	var s = 3.0
	var points = PoolVector2Array()
	points.push_back(Vector2(-1.0, -2.0) * s)
	points.push_back(Vector2(0.0, 2.0) * s)
	points.push_back(Vector2(1.0, -2.0) * s)
	var colours = PoolColorArray()
	colours.push_back(Color.white)
	colours.push_back(Color.white)
	colours.push_back(Color.white)
	draw_polygon(points, colours)
