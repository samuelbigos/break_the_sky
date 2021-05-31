extends Area2D

var state = BoidEnemyLaser.LaserState.Inactive


func _draw():
	if state == BoidEnemyLaser.LaserState.Inactive:
		return
		
	if state == BoidEnemyLaser.LaserState.Charging:
		if OS.get_system_time_msecs() % 100 > 50:
			var size = Vector2($CollisionShape2D.shape.extents.x * 1.0, $CollisionShape2D.shape.extents.y * 2.0)
			var rect = Rect2(-size * 0.5, size)
			draw_rect(rect, Colours.Accent, false, 4.0)
			
	if state == BoidEnemyLaser.LaserState.Firing:
		var size = Vector2($CollisionShape2D.shape.extents.x * 2.0, $CollisionShape2D.shape.extents.y * 2.0)
		var rect = Rect2(-size * 0.5, size)
		draw_rect(rect, Colours.White, true, 2.0)
