extends Node2D
class_name Leader

export var ForwardAccel = 1.0
export var StrafeAccel = 1.0
export var Damping = 0.5

var _game = null

var _velocity: Vector2

func _ready():
	pass

func _process(delta):
	
	var mousePos = get_global_mouse_position()
	var lookAt = mousePos - global_position
	rotation = -atan2(lookAt.x, lookAt.y);
	
	var forward = Vector2(0.0, -1.0)
	var left = Vector2(-1.0, 0.0)
	if Input.is_action_pressed("w"):
		_velocity += forward * ForwardAccel * delta
	if Input.is_action_pressed("s"):
		_velocity += -forward * ForwardAccel * delta
	if Input.is_action_pressed("a"):
		_velocity += left * ForwardAccel * delta
	if Input.is_action_pressed("d"):
		_velocity += -left * ForwardAccel * delta

	if (global_position + _velocity).length() > _game.PlayRadius - 5.0:
		_velocity *= 0.0
		
	global_position += _velocity
	_velocity *= pow(1.0 - clamp(Damping, 0.0, 1.0), delta * 60.0)
	
	if Input.is_action_just_pressed("formation_1"):
		_game.changeFormation(0, false)
	if Input.is_action_just_pressed("formation_2"):
		_game.changeFormation(1, false)
	if Input.is_action_just_pressed("formation_3"):
		_game.changeFormation(2, false)
		
func addBoid(pos: Vector2):
	_game.addBoid(pos);

func _draw():
	draw_circle(Vector2(0.0, 0.0), 5.0, Color.white)

func _on_Leader_area_entered(area):
	if area.is_in_group("pickupAdd"):
		area.queue_free()
		addBoid(area.global_position)
		
	if area.is_in_group("enemy"):
		_game.lose()
