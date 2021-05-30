extends Sprite

var _velocity: Vector2
var _alignment: int


func _ready():
	pass
	
func init(velocity: Vector2, alignment: int):
	_velocity = velocity
	_alignment = alignment
	
func _process(delta):
	global_position += _velocity * delta
	
	var screenBound = get_viewport_rect()
	if not screenBound.has_point(global_position):
		queue_free()
