extends Node2D
class_name Leader

export var Damping = 0.5

var _game = null

var _velocity: Vector2

func _ready():
	pass

func _process(delta):
	
	var mousePos = get_global_mouse_position()
	var lookAt = mousePos - global_position
	rotation = -atan2(lookAt.x, lookAt.y);
	
	var forward = Vector2(0.0, -1.0) * 120.0
	var left = Vector2(-1.0, 0.0) * 120.0
	var accel = _game.BasePlayerSpeed
	if Input.is_action_pressed("w"):
		_velocity += forward * accel * delta
	if Input.is_action_pressed("s"):
		_velocity += -forward * accel * delta
	if Input.is_action_pressed("a"):
		_velocity += left * accel * delta
	if Input.is_action_pressed("d"):
		_velocity += -left * accel * delta

	if (global_position + _velocity * delta).length() > _game.PlayRadius - 5.0:
		_velocity *= 0.0
		
	global_position += _velocity * delta
	_velocity *= pow(1.0 - clamp(Damping, 0.0, 1.0), delta * 60.0)
	
#	if Input.is_action_just_pressed("formation_1"):
#		_game.changeFormation(0, false)
#	if Input.is_action_just_pressed("formation_2"):
#		_game.changeFormation(1, false)
#	if Input.is_action_just_pressed("formation_3"):
#		_game.changeFormation(2, false)

	if Input.is_action_just_pressed("boids_align"):
		_game.changeFormation(1, false)
	if Input.is_action_just_released("boids_align"):
		_game.changeFormation(0, false)
		
func addBoids(pos: Vector2):
	_game.addBoids(pos);

func _draw():
	draw_circle(Vector2(0.0, 0.0), 6.0, Colours.Secondary)
	draw_circle(Vector2(0.0, 0.0), 2.0, Colours.Primary)

func _on_Leader_area_entered(area):
	if area.is_in_group("pickupAdd"):
		area.queue_free()
		addBoids(area.global_position)
		
	if area.is_in_group("enemy"):
		_game.lose()
		
	#if area.is_in_group("bullet") and area._alignment == 1:
	#	_game.lose()
