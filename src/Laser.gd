extends Area2D

var state = BoidEnemyLaser.LaserState.Inactive


func _draw():
	if state == BoidEnemyLaser.LaserState.Inactive:
		return
			
	var size = Vector2($CollisionShape2D.shape.extents.x * 2.0, $CollisionShape2D.shape.extents.y * 2.0)
	var rect = Rect2(-size * 0.5, size)
		
	if state == BoidEnemyLaser.LaserState.Charging:
		draw_rect(rect, Color.red, false, 2.0)
			
	if state == BoidEnemyLaser.LaserState.Firing:
		draw_rect(rect, Color.white, true, 2.0)
