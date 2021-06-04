extends BoidBase
class_name BoidEnemyBase

export var PickupDropRate = 0.25
export var Points = 10
export var MaxHealth = 1.0
export var HitFlashTime = 1.0 / 30.0
export var DestroyTime = 3.0
export var DestroyTrauma = 0.1
export var HitTrauma = 0.05
export var MinVelocity = 0.0
export var MaxAngularVelocity = 1000.0

onready var _sprite = get_node("Sprite")
onready var _damagedParticles = get_node("Damaged")
onready var _trail = get_node("Trail")
onready var _hitSfx2 = load("res://assets/sfx/hit2.wav")
onready var _hitSfx3 = load("res://assets/sfx/hit3.wav")

var _sfxHit: AudioStreamPlayer2D
var _sfxHitMicro: AudioStreamPlayer2D
var _target: Node2D
var _game: Object
var _health: float
var _hitFlashTimer: float
var _destroyed = false
var _destroyedTimer: float
var _baseScale: Vector2
var _destroyRot: float
var _move: bool = true
var _destroyScore: bool

func isDestroyed(): return _destroyed

func _ready():
	connect("area_entered", self, "_on_BoidBase_area_entered")
	_health = MaxHealth
	_baseScale = _sprite.scale
	_sfxHit = AudioStreamPlayer2D.new()
	_sfxHitMicro = AudioStreamPlayer2D.new()
	add_child(_sfxHit)
	add_child(_sfxHitMicro)
	_sfxHit.stream = _hitSfx2
	_sfxHitMicro.stream = _hitSfx3
	_sfxHitMicro.volume_db = -5
	if not is_instance_valid(_trail):
		_trail = null
	if _trail:
		_trail.boid = self

func init(game, target):
	_game = game
	setTarget(target)

func setTarget(target: Object):
	_target = target

func _process(delta: float):
	var steering = Vector2(0.0, 0.0)
	if not _destroyed:
		if _move:
			steering += _steeringPursuit(_target.global_position, _target._velocity)
			steering += _steeringEdgeRepulsion(_game.PlayRadius) * 2.0
		
		# limit angular velocity
		if _velocity.length_squared() > 0:
			var linearComp = _velocity.normalized() * steering.length() * steering.normalized().dot(_velocity.normalized())
			var tangent = Vector2(_velocity.y, -_velocity.x)
			var angularComp = tangent.normalized() * steering.length() * steering.normalized().dot(tangent.normalized())
			steering = linearComp + angularComp.normalized() * clamp(angularComp.length(), 0.0, MaxAngularVelocity)
		
		steering = clampVec(steering, MinVelocity, MaxVelocity)
		
		if MinVelocity > 0.0:
			_velocity = clampVec(_velocity + steering * delta, MinVelocity, MaxVelocity)
		else:
			_velocity = truncate(_velocity + steering * delta, MaxVelocity)
		
	global_position += _velocity * delta
	update()
	
	if _trail and TrailLength > 0:
		_trail.update()
	
	# damping
	_velocity *= pow(1.0 - clamp(Damping, 0.0, 1.0), delta * 60.0)
	
	# hit flash
	_hitFlashTimer -= delta
	if _hitFlashTimer < 0.0:
		if not _destroyed:
			_sprite.modulate = Colours.Secondary
		
	if _destroyed:
		_destroyedTimer -= delta
		var t = 1.0 - clamp(_destroyedTimer / DestroyTime, 0.0, 1.0)
		_sprite.scale = lerp(_baseScale, Vector2(0.0, 0.0), t)
		if _trail:
			_trail.alpha = lerp(1.0, 0.0, t)
		_destroyRot += PI * 2.0 * delta
		if _destroyedTimer < 0.0:
			queue_free()
			
	if _health < 0.0 and not _destroyed:
		destroy(_destroyScore)
	
func destroy(score: bool):
	if not _destroyed:
		if score and not _destroyed:
			_game.addScore(Points, global_position, true)
			if rand_range(0.0, 1.0) < PickupDropRate:
				_game.spawnPickupAdd(global_position, false)
				
	_destroyed = true
	_sprite.modulate = Colours.White
	_destroyedTimer = DestroyTime
	_sprite.z_index = -1
	if _damagedParticles:
		_damagedParticles.emitting = false
	GlobalCamera.addTrauma(DestroyTrauma)
	
func onHit(damage: float, score: bool, bulletVel: Vector2, microbullet: bool, pos: Vector2):
	_health -= damage
	_sprite.modulate = Colours.White
	_hitFlashTimer = HitFlashTime
	var hitParticles = HitParticles.instance()
	hitParticles.position = pos
	hitParticles.emitting = true
	_game.add_child(hitParticles)
	if _damagedParticles:
		_damagedParticles.emitting = true
	if not microbullet:
		PauseManager.pauseFlash()
		GlobalCamera.addTrauma(HitTrauma)
		_sfxHit.play()
	else:
		_sfxHitMicro.play()
	_destroyScore = score

func _on_BoidBase_area_entered(area):
	if area.is_in_group("bullet") and area.getAlignment() == 0 and not _destroyed:
		onHit(area._damage, true, area._velocity, area._microbullet, area.global_position)
		area.queue_free()
