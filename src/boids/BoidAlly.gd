extends BoidBase
class_name BoidAlly

export var SlowingRadius = 100.0
export var AlignmentRadius = 20.0
export var SeparationRadius = 10.0
export var CohesionRadius = 50.0
export var HitDamage = 3.0
export var DestroyTime = 3.0
export var ShootSize = 1.5
export var ShootTrauma = 0.05
export var DestroyTrauma = 0.1
export var MicroBulletCD = 1.0
export var MicroBulletRange = 400.0
export var MicroBulletDamageMod = 0.25
export var MaxAngularVelocity = 500.0

export var BulletScene: PackedScene
export var MicoBulletScene: PackedScene

onready var _sprite = get_node("Sprite")
onready var _sfxShot = get_node("SFXShot")
onready var _sfxHit = get_node("SFXHit")
onready var _sfxHitMicro = get_node("SFXHitMicro")
onready var _damagedParticles = get_node("Damaged")

var _game: Object = null
var _target: Node2D
var _targetOffset: Vector2
var _shootCooldown: float
var _destroyed = false
var _destroyedTimer: float
var _baseScale: Vector2
var _destroyRot: float
var _microBulletTargetSearchTimer: float
var _microBulletTarget = null
var _microBulletCD: float


func isDestroyed(): return _destroyed

func _ready():	
	$Trail.boid = self
	_sprite.modulate = Colours.Secondary
	_baseScale = _sprite.scale
	
func init(target: Node2D, game: Object):
	_target = target;
	_game = game
	
func setOffset(targetOffset: Vector2):
	_targetOffset = targetOffset
	
func getVelocity():	return _velocity

func _process(delta: float):
	
	MaxVelocity = _game.BaseBoidSpeed
	
	var targetPos = _target.global_position + (_target.transform.basis_xform(_targetOffset) / _target.scale)
	var shootDir = (get_global_mouse_position() - global_position).normalized()
	
	# steering
	var steering = Vector2(0.0, 0.0)
	if not _destroyed:
		steering = _steeringArrive(targetPos, SlowingRadius)
		steering += _steeringSeparation(_game.getBoids(), _game.BaseBoidGrouping * 0.66)
		steering += _steeringEdgeRepulsion(_game.PlayRadius) * 2.0
		
		# limit angular velocity
		if _velocity.length_squared() > 0:
			var linearComp = _velocity.normalized() * steering.length() * steering.normalized().dot(_velocity.normalized())
			var tangent = Vector2(_velocity.y, -_velocity.x)
			var angularComp = tangent.normalized() * steering.length() * steering.normalized().dot(tangent.normalized())
			steering = linearComp + angularComp.normalized() * clamp(angularComp.length(), 0.0, MaxAngularVelocity)

		steering = truncate(steering, MaxVelocity)
		_velocity = truncate(_velocity + steering * delta, MaxVelocity)
		
	global_position += _velocity * delta
	_sprite.rotation = -atan2(_velocity.x, _velocity.y)
	
	# damping
	_velocity *= pow(1.0 - clamp(Damping, 0.0, 1.0), delta * 60.0)
	
	_shootCooldown -= delta
	if Input.is_action_pressed("shoot") and _shootCooldown <= 0.0:
		if _canShoot(shootDir):
			_shoot(shootDir)
#	if _shootCooldown < 0.0:
#		_sprite.modulate = Colours.Secondary
	
	# shooting
	if _shootCooldown > 0.0:
		var t = _shootCooldown / _game.BaseBoidReload
		t = pow(clamp(t, 0.0, 1.0), 5.0)
		_sprite.scale = lerp(_baseScale * 2.0, _baseScale, 1.0 - t)
		
	# microbullets
	if _game.BaseMicroturrets and not _destroyed:
		if not is_instance_valid(_microBulletTarget) or _microBulletTarget.isDestroyed():
			_microBulletTarget = null
			
		_microBulletTargetSearchTimer -= delta
		if _microBulletTarget == null and _microBulletTargetSearchTimer < 0.0:
			_microBulletTargetSearchTimer = 0.1
			for enemy in _game._enemies:
				if (enemy.global_position - global_position).length() < MicroBulletRange:
					_microBulletTarget = enemy
					_microBulletCD = rand_range(MicroBulletCD * 0.5, MicroBulletCD * 1.5)
		
		if _microBulletTarget:
			if (_microBulletTarget.global_position - global_position).length() > MicroBulletRange:
				_microBulletTarget = null
				_microBulletTargetSearchTimer = 0.1
			else:
				_microBulletCD -= delta
				if _microBulletCD < 0.0:
					_microBulletCD = rand_range(MicroBulletCD * 0.5, MicroBulletCD * 1.5)
					var mb = MicoBulletScene.instance()
					var spread = _game.BaseBoidSpread
					var dir = (_microBulletTarget.global_position - global_position).normalized()
					dir += Vector2(-dir.y, dir.x) * rand_range(-spread, spread)
					mb.init(dir * _game.BaseBulletSpeed, 0, _game.PlayRadius)
					mb.global_position = global_position
					mb._damage = _game.BaseBoidDamage * MicroBulletDamageMod
					_game.add_child(mb)
					_sfxHitMicro.play()
			
	# update trail
	$Trail.update()
	
	if _destroyed:
		_destroyedTimer -= delta
		var t = 1.0 - clamp(_destroyedTimer / DestroyTime, 0.0, 1.0)
		_sprite.scale = lerp(_baseScale, Vector2(0.0, 0.0), t)
		_destroyRot += PI * 2.0 * delta
		if _destroyedTimer < 0.0:
			queue_free()
		
func _shoot(dir: Vector2):
	_shootCooldown = _game.BaseBoidReload
	var bullet = BulletScene.instance()
	var spread = _game.BaseBoidSpread
	dir += Vector2(-dir.y, dir.x) * rand_range(-spread, spread)
	bullet.init(dir * _game.BaseBulletSpeed, 0, _game.PlayRadius)
	bullet._damage = _game.BaseBoidDamage
	bullet.global_position = global_position
	_game.add_child(bullet)
	_game.pushBack(self)
	var traumaMod = 1.0 - clamp(_game.getNumBoids() / 100.0, 0.0, 0.5)
	GlobalCamera.addTrauma(ShootTrauma * traumaMod)
	_sfxShot.play()
	#_sprite.modulate = Colours.Grey
	
func _canShoot(dir: Vector2):
	if _destroyed: return false
	# can shoot if there are no other boids in the shoot direction
	var blocked = false
	for boid in _game.getBoids():
		if boid == self or boid.isDestroyed():
			continue
			
		if (boid.global_position - global_position).normalized().dot(dir.normalized()) > 0.9:
			blocked = true
			break
			
	return not blocked
	
func _destroy():
	if not _destroyed:
		_game.removeBoid(self)
		_destroyed = true
		_sprite.modulate = Colours.White
		_destroyedTimer = DestroyTime
		_sprite.z_index = -1
		GlobalCamera.addTrauma(DestroyTrauma)
		_sfxHit.play()
		_damagedParticles.emitting = true

func _on_BoidAlly_area_entered(area):
	if area.is_in_group("enemy") and not area.isDestroyed():
		area.onHit(HitDamage, false, _velocity, false)
		_destroy()
	
	if area.is_in_group("laser"):
		_destroy()
		
	if area.is_in_group("bullet") and area._alignment == 1:
		area.onHit()
		_destroy()
