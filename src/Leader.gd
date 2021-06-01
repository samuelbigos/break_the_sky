extends Node2D
class_name Leader

export var Damping = 0.5

onready var _sfxPickup = get_node("SFXPickup")
onready var _damagedParticles = get_node("Damaged")

var _game = null
var _colour: Color
var _destroyed = false
var _velocity: Vector2


func _ready():
	_colour = Colours.Secondary

func _process(delta):	
	if not _destroyed:
		var mousePos = get_global_mouse_position()
		var lookAt = mousePos - global_position
		rotation = -atan2(lookAt.x, lookAt.y);
		
		var forward = Vector2(0.0, -1.0)
		var left = Vector2(-1.0, 0.0)
		var accel = _game.BasePlayerSpeed
		
		var dir = Vector2(0.0, 0.0)
		if Input.is_action_pressed("w"):
			dir += forward
		if Input.is_action_pressed("s"):
			dir += -forward
		if Input.is_action_pressed("a"):
			dir += left 
		if Input.is_action_pressed("d"):
			dir += -left
		
		if dir != Vector2(0.0, 0.0):
			dir = dir.normalized()
			dir *= 100.0 * accel * delta
			_velocity += dir

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
	
func destroy():
	_damagedParticles.emitting = true
	_colour = Colours.White
	_destroyed = true
	update()

func _draw():
	draw_circle(Vector2(0.0, 0.0), 6.0, _colour)
	draw_circle(Vector2(0.0, 0.0), 2.0, Colours.Primary)

func _on_Leader_area_entered(area):
	if area.is_in_group("pickupAdd"):
		area.queue_free()
		addBoids(area.global_position)
		_sfxPickup.play()
		
	#if area.is_in_group("enemy") and not area.isDestroyed():
	#	_game.lose()
		
	#if area.is_in_group("bullet") and area._alignment == 1:
	#	_game.lose()
