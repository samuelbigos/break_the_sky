extends BoidEnemyBase
class_name BoidEnemyBeacon

export var BulletScene: PackedScene

export var TargetBeaconDist = 350.0
export var BeaconCooldown = 5.0
export var BeaconCharge = 1.0
export var BeaconPulseDuration = 0.25
export var Pulses = 5
export var BulletsPerPulse = 18
export var BulletSpeed = 300.0

enum BeaconState {
	Inactive,
	Charging,
	Firing
}

var _beaconState = BeaconState.Inactive
var _beaconCooldown: float
var _beaconCharge: float
var _beaconDuration: float
var _pulses: int


func _ready():
	$Sprite.modulate = Colours.Secondary

func _process(delta: float):
	var distToTarget = (global_position - _target.global_position).length()

	# firin' mah lazor
	if _beaconState == BeaconState.Inactive:
		_beaconCooldown -= delta
		if distToTarget < TargetBeaconDist and _beaconCooldown < 0.0:
			_beaconState = BeaconState.Charging
			_beaconCharge = BeaconCharge
			
	if _beaconState == BeaconState.Charging:
		_beaconCharge -= delta
		if _beaconCharge < 0.0:
			_beaconState = BeaconState.Firing
			_beaconDuration = 0.0
			_pulses = Pulses
			
	if _beaconState == BeaconState.Firing:
		_beaconDuration -= delta
		if _beaconDuration < 0.0:
			_pulses -= 1
			if _pulses == 0:
				_beaconState = BeaconState.Inactive
				_beaconCooldown = BeaconCooldown
			else:
				firePulse()
				_beaconDuration = BeaconPulseDuration
				
	rotation = -atan2(_velocity.x, _velocity.y)
				
func firePulse():
	for i in range(0, BulletsPerPulse):
		var bullet = BulletScene.instance()
		var f = float(i) * PI * 2.0 / float(BulletsPerPulse)
		var dir = Vector2(sin(f), -cos(f)).normalized()
		bullet.init(dir * BulletSpeed, 1, _game.PlayRadius)
		bullet.global_position = global_position + dir * 32.0
		_game.add_child(bullet)
	
func destroy(score: bool):
	.destroy(score)
