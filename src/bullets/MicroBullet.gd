extends Node2D

var _velocity: Vector2
var _alignment: int
var _playRadius: float
var _length = 5.0
var _damage = 0.25
var _microbullet = true

func getAlignment(): return _alignment

func _ready():
	pass
	
func init(velocity: Vector2, alignment: int, playRadius: float):
	_velocity = velocity
	_alignment = alignment
	_playRadius = playRadius
	$CollisionShape2D.shape.radius =  _damage * 2.0 + 1.0
	
	rotation = -atan2(_velocity.x, _velocity.y)
	
func _process(delta):
	global_position += _velocity * delta	
	if global_position.length() + _length > _playRadius:
		queue_free()

func _draw():
	# outer
	draw_line(Vector2(0.0, -_length * 0.5), Vector2(0.0, _length * 0.5), Colours.Secondary, _damage * 8.0)
