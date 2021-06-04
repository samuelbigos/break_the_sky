extends BoidEnemyBase
class_name BoidEnemyCarrier

export var BulletScene: PackedScene
export var DroneScene: PackedScene

export var TargetDist = 400.0
export var DronePulseCooldown = 2.0
export var DroneSpawnInterval = 0.33
export var DronePulseCount = 10
export var DroneSpawnRange = 750.0

onready var _sfxBeaconFire = get_node("SFXBeaconFire")
onready var _sfxDestroy = get_node("SFXDestroy")
onready var _rocochetSfx = load("res://assets/sfx/ricochet.wav")

var _rotorguns = []
var _beaconCooldown: float
var _beaconCharge: float
var _beaconDuration: float
var _pulses: int
var _firstFrame = true
var _droneSpawnTimer: float
var _spawningDrones = false
var _dronePulseTimer: float
var _dronePulseSpawned: int
var _droneSpawnSide: int


func _ready():
	$Sprite.modulate = Colours.Secondary
	
	_rotorguns.append(get_node("Rotorgun1"))
	get_node("Rotorgun1").lock = $Lock1
	_rotorguns.append(get_node("Rotorgun2"))
	get_node("Rotorgun2").lock = $Lock2
	_rotorguns.append(get_node("Rotorgun3"))
	get_node("Rotorgun3").lock = $Lock3
	_rotorguns.append(get_node("Rotorgun4"))
	get_node("Rotorgun4").lock = $Lock4
	for rotorgun in _rotorguns:
		rotorgun.init(_game, _target)	

func _process(delta: float):
	rotation = -atan2(_velocity.x, _velocity.y)
	if _firstFrame:
		_firstFrame = false
		_sfxHit.stream = _rocochetSfx
		
	var count = 0
	for r in _rotorguns:
		if is_instance_valid(r) and not r.isDestroyed():
			count += 1
			
	if count == 0 and not isDestroyed():
		destroy(Points)
		
	var dist = (_target.global_position - global_position).length()
	if dist < TargetDist:
		_move = false
	else:
		_move = true
		
	# drone spawn
	if not isDestroyed():
		_dronePulseTimer -= delta
		if _dronePulseTimer < 0.0 and dist < DroneSpawnRange and not _spawningDrones:
			_spawningDrones = true
			_dronePulseSpawned = 0
			
		if _spawningDrones:
			_droneSpawnTimer -= delta
			if _droneSpawnTimer < 0.0:
				_spawnDrone()
				_droneSpawnTimer = DroneSpawnInterval
				_dronePulseSpawned += 1
			
			if _dronePulseSpawned >= DronePulseCount:
				_spawningDrones = false
				_dronePulseTimer = DronePulseCooldown
			
func _spawnDrone():
	var spawnPos: Vector2
	var enemy = DroneScene.instance()
	_droneSpawnSide = (_droneSpawnSide + 1) % 2
	if _droneSpawnSide == 0:
		enemy.global_position = $SpawnLeft.global_position
	else:
		enemy.global_position = $SpawnRight.global_position
		
	enemy.init(_game, _target)
	_game.add_child(enemy)
	_game._enemies.append(enemy)
	enemy._velocity = enemy.MaxVelocity * (enemy.global_position - global_position).normalized()
	
func onHit(damage: float, score: bool, bulletVel: Vector2, microbullet: bool, pos):
	_sfxHit.play()
	
func destroy(score: bool):
	.destroy(score)
	_sfxDestroy.play()
