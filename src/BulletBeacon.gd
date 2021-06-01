extends Node2D

var _velocity: Vector2
var _alignment: int
var _playRadius: float
var _radius = 7.0
var _damage = 1.0
var _health = 1.0

func getAlignment(): return _alignment

func _ready():
	pass
	
func init(velocity: Vector2, alignment: int, playRadius: float):
	_velocity = velocity
	_alignment = alignment
	_playRadius = playRadius
	
	rotation = -atan2(_velocity.x, _velocity.y)
	
func _process(delta):
	global_position += _velocity * delta
	if global_position.length() > _playRadius:
		queue_free()
		
func onHit():
	_health -= 1.0
	if (_health <= 0.0):
		queue_free()

func _draw():
	draw_circle(Vector2(0.0, 0.0), _radius, Colours.Secondary)
	draw_circle(Vector2(0.0, 0.0), _radius - 3.0, Colours.White)