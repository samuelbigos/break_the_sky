extends Sprite

export var MaxVelocity = 5.0
export var ForwardAccel = 1.0
export var StrafeAccel = 1.0
export var Damping = 0.5
export var Mass = 1.0
export var SlowingRadius = 100.0
export var AlignmentRadius = 20.0
export var SeparationRadius = 5.0
export var CohesionRadius = 50.0

export var BulletScene: PackedScene
export var ShootCooldown = 2.0
export var BulletSpeed = 200.0
export var BulletSpread = 0.1

var _game: Object = null
var _velocity: Vector2
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
	var shootDir = (get_global_mouse_position() - _game.getPlayer().global_position).normalized()
	
	# arrive
	var steering = Vector2(0.0, 0.0)
	steering = _steeringArrive(targetPos, SlowingRadius)
	#steering += _steeringAlignment(_game.getBoids(), AlignmentRadius) * 0.5
	#steering += _steeringCohesion(_game.getBoids(), CohesionRadius) * 0.5
	steering += _steeringSeparation(_game.getBoids(), SeparationRadius)
	#steering += _steeringShoot(shootDir)
	
	steering = truncate(steering, MaxVelocity)
	steering /= Mass
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
	bullet.init(dir * BulletSpeed, 0)
	bullet.global_position = global_position
	_game.add_child(bullet)
	_game.pushBack(self)
	
func _canShoot(dir: Vector2):
	# can shoot if there are no other boids in the shoot direction
	var blocked = false
	for boid in _game.getBoids():
		if boid == self:
			continue
			
		if (boid.global_position - global_position).normalized().dot(dir.normalized()) > 0.25:
			blocked = true
			break
			
	return not blocked
	
func _steeringShoot(dir: Vector2):
	var desiredVelocity = Vector2(0.0, 0.0)
	if _shootCooldown > 0.0:
		desiredVelocity = -dir.normalized() * MaxVelocity
		var steering = desiredVelocity - _velocity
		return steering
	else:
		return desiredVelocity
	
func _steeringFollow(target: Vector2, delta: float):
	var desiredVelocity = (target - global_position).normalized() * MaxVelocity
	var steering = desiredVelocity - _velocity
	return steering
	
func _steeringArrive(target: Vector2, slowingRadius: float):
	var desiredVelocity = (target - global_position).normalized() * MaxVelocity
	var distance = (target - global_position).length()
	if distance < slowingRadius:
		desiredVelocity = desiredVelocity.normalized() * MaxVelocity * (distance / slowingRadius)
	var steering = desiredVelocity - _velocity
	return steering
	
func _steeringAlignment(boids, alignmentRadius: float):
	var nCount = 0
	var desiredVelocity = Vector2(0.0, 0.0)
	
	for boid in boids:
		if boid == self:
			continue
			
		var distance = (boid.global_position - global_position).length()
		if distance < alignmentRadius:
			desiredVelocity += boid.getVelocity()
			nCount += 1
	if nCount == 0:
		return desiredVelocity
	
	desiredVelocity = desiredVelocity.normalized() * MaxVelocity
	var steering = desiredVelocity - _velocity
	return steering
	
func _steeringCohesion(boids, cohesionRadius):
	var nCount = 0
	var desiredVelocity = Vector2(0.0, 0.0)
	
	for boid in boids:
		if boid == self:
			continue
			
		var distance = (boid.global_position - global_position).length()
		if distance < cohesionRadius:
			desiredVelocity += boid.global_position
			nCount += 1
	if nCount == 0:
		return desiredVelocity
	
	desiredVelocity /= nCount
	desiredVelocity = desiredVelocity - global_position
	desiredVelocity = desiredVelocity.normalized() * MaxVelocity
	var steering = desiredVelocity - _velocity
	return steering
	
func _steeringSeparation(boids, separationRadius):
	var nCount = 0
	var desiredVelocity = Vector2(0.0, 0.0)
	
	for boid in boids:
		if boid == self:
			continue
			
		var distance = (boid.global_position - global_position).length()
		if distance < separationRadius:
			desiredVelocity += boid.global_position - global_position
			nCount += 1
	if nCount == 0:
		return desiredVelocity
	
	desiredVelocity = desiredVelocity.normalized() * MaxVelocity * -1
	var steering = desiredVelocity - _velocity
	return steering
			
func truncate(vector: Vector2, v_max: float):
	var length = vector.length()
	if length == 0.0:
		return vector
	var i = v_max / vector.length()
	i = min(i, 1.0)
	return vector * i
	
func _draw():
	#draw_line(Vector2(0.0, 0.0), _velocity * 0.025, _colour)
	pass
