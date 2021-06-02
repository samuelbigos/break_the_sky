extends BoidBase
class_name BoidEnemyBase

export var PickupDropRate = 0.25
export var Points = 10
export var MaxHealth = 1.0
export var HitFlashTime = 1.0 / 60.0
export var DestroyTime = 3.0
export var DestroyTrauma = 0.1
export var HitTrauma = 0.05

onready var _sprite = get_node("Sprite")
onready var _damagedParticles = get_node("Damaged")
onready var _shockwave = get_node("Shockwave")

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

func isDestroyed(): return _destroyed

func _ready():
	connect("area_entered", self, "_on_BoidBase_area_entered")
	_health = MaxHealth
	_baseScale = _sprite.scale
	_sfxHit = AudioStreamPlayer2D.new()
	_sfxHitMicro = AudioStreamPlayer2D.new()
	add_child(_sfxHit)
	add_child(_sfxHitMicro)
	_sfxHit.stream = load("res://assets/sfx/hit2.wav")
	_sfxHitMicro.stream = load("res://assets/sfx/hit3.wav")
	_sfxHitMicro.volume_db = -5

func init(game, target):
	_game = game
	setTarget(target)

func setTarget(target: Object):
	_target = target

func _process(delta: float):
	var steering = Vector2(0.0, 0.0)
	if not _destroyed:
		steering += _steeringPursuit(_target.global_position, _target._velocity)
		steering += _steeringEdgeRepulsion(_game.PlayRadius) * 2.0
		
		steering = truncate(steering, MaxVelocity)
		_velocity = truncate(_velocity + steering * delta, MaxVelocity)
		
	global_position += _velocity * delta
	update()
	
	# damping
	_velocity *= pow(1.0 - clamp(Damping, 0.0, 1.0), delta * 60.0)
	
	# hit flash
	_hitFlashTimer -= delta
	if _hitFlashTimer < 0.0:
		if not _destroyed:
			_sprite.modulate = Colours.Secondary
		_shockwave.visible = false
		
	if _destroyed:
		_destroyedTimer -= delta
		var t = 1.0 - clamp(_destroyedTimer / DestroyTime, 0.0, 1.0)
		_sprite.scale = lerp(_baseScale, Vector2(0.0, 0.0), t)
		_destroyRot += PI * 2.0 * delta
		if _destroyedTimer < 0.0:
			queue_free()
	
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
	
func onHit(damage: float, score: bool, bulletVel: Vector2, microbullet: bool):
	_health -= damage
	_sprite.modulate = Colours.White
	#_shockwave.visible = true
	#_shockwave.rotation = atan2(bulletVel.y, bulletVel.x) + rotation + PI * 0.5
	_hitFlashTimer = HitFlashTime
	var hitParticles = HitParticles.instance()
	hitParticles.position = global_position
	hitParticles.emitting = true
	_game.add_child(hitParticles)
	if _damagedParticles:
		_damagedParticles.emitting = true
	if _health <= 0.0:
		destroy(score)
	if not microbullet:
		PauseManager.pauseFlash()
		GlobalCamera.addTrauma(HitTrauma)
		_sfxHit.play()
	else:
		_sfxHitMicro.play()

func _on_BoidBase_area_entered(area):
	if area.is_in_group("bullet") and area.getAlignment() == 0 and not _destroyed:
		onHit(area._damage, true, area._velocity, area._microbullet)
		area.queue_free()
