extends BoidBase
class_name BoidEnemyBase

export var PickupDropRate = 0.25
export var Points = 10
export var MaxHealth = 1.0

var _target: Node2D
var _game: Object
var _health


func _ready():
	connect("area_entered", self, "_on_BoidBase_area_entered")
	_health = MaxHealth

func init(game, target):
	_game = game
	setTarget(target)

func setTarget(target: Object):
	_target = target

func _process(delta: float):
	var steering = Vector2(0.0, 0.0)
	steering += _steeringPursuit(_target.global_position, _target._velocity)
	steering += _steeringEdgeRepulsion(_game.PlayRadius) * 2.0
	
	steering = truncate(steering, MaxVelocity)
	_velocity = truncate(_velocity + steering * delta, MaxVelocity)
		
	global_position += _velocity * delta
	update()
	
	# damping
	_velocity *= pow(1.0 - clamp(Damping, 0.0, 1.0), delta * 60.0)
	
	rotation = -atan2(_velocity.x, _velocity.y)
	
func destroy(score: bool):
	if is_queued_for_deletion():
		return
	if score:
		_game.addScore(Points)
		if rand_range(0.0, 1.0) < PickupDropRate:
			_game.spawnPickupAdd(global_position, false)	
	queue_free()
	
func onHit(damage: float):
	_health -= damage
	if _health <= 0.0:
		destroy(true)

func _on_BoidBase_area_entered(area):
	if area.is_in_group("bullet") and area.getAlignment() == 0:
		onHit(area._damage)
		area.queue_free()
