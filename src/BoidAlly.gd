extends BoidBase
class_name BoidAlly

export var SlowingRadius = 100.0
export var AlignmentRadius = 20.0
export var SeparationRadius = 5.0
export var CohesionRadius = 50.0

export var BulletScene: PackedScene
export var ShootCooldown = 2.0
export var BulletSpeed = 200.0
export var BulletSpread = 0.1

var _game: Object = null
var _target: Node2D
var _targetOffset: Vector2
var _colour: Color

var _shootCooldown: float


func _ready():
	pass
	
func init(target: Node2D, game: Object):
	_target = target;
	_game = game
	
func setOffset(targetOffset: Vector2):
	_targetOffset = targetOffset
	
func getVelocity():	return _velocity

func _process(delta: float):
	
	var targetPos = _target.global_position + (_target.transform.basis_xform(_targetOffset) / _target.scale)
	var shootDir = (get_global_mouse_position() - global_position).normalized()
	
	# steering
	var steering = Vector2(0.0, 0.0)
	steering = _steeringArrive(targetPos, SlowingRadius)
	#steering += _steeringAlignment(_game.getBoids(), AlignmentRadius) * 0.5
	#steering += _steeringCohesion(_game.getBoids(), CohesionRadius) * 0.5
	steering += _steeringSeparation(_game.getBoids(), SeparationRadius)
	steering += _steeringEdgeRepulsion(_game.PlayRadius) * 2.0
	
	steering = truncate(steering, MaxVelocity)
	_velocity = truncate(_velocity + steering * delta, MaxVelocity)
		
	global_position += _velocity * delta
	update()
	
	# damping
	_velocity *= pow(1.0 - clamp(Damping, 0.0, 1.0), delta * 60.0)
	
	_shootCooldown -= delta
	if Input.is_action_pressed("shoot") and _shootCooldown <= 0.0:
		if _canShoot(shootDir):
			_shoot(shootDir)
		
func _shoot(dir: Vector2):
	_shootCooldown = ShootCooldown
	var bullet = BulletScene.instance()
	dir += Vector2(-dir.y, dir.x) * rand_range(-BulletSpread, BulletSpread)
	bullet.init(dir * BulletSpeed, 0, _game.PlayRadius)
	bullet.global_position = global_position
	_game.add_child(bullet)
	_game.pushBack(self)
	
func _canShoot(dir: Vector2):
	# can shoot if there are no other boids in the shoot direction
	var blocked = false
	for boid in _game.getBoids():
		if boid == self:
			continue
			
		if (boid.global_position - global_position).normalized().dot(dir.normalized()) > 0.9:
			blocked = true
			break
			
	return not blocked
	
func _draw():
	draw_circle(Vector2(0.0, 0.0), 2.0, Color.white)
	
func _destroy():
	_game.removeBoid(self)
	queue_free()

func _on_BoidAlly_area_entered(area):
	if area.is_in_group("enemy"):
		area.destroy(false)
		_destroy()
