extends Sprite
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

	global_position += _velocity
	_velocity *= pow(1.0 - clamp(Damping, 0.0, 1.0), delta * 60.0)
	
	if Input.is_action_just_pressed("formation_1"):
		_game.changeFormation(0, false)
	if Input.is_action_just_pressed("formation_2"):
		_game.changeFormation(1, false)
	if Input.is_action_just_pressed("formation_3"):
		_game.changeFormation(2, false)
