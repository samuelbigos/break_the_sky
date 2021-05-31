extends BoidEnemyBase
class_name BoidEnemyLaser

export var TargetLaserDist = 250.0
export var LaserCooldown = 5.0
export var LaserCharge = 1.0
export var LaserDuration = 2.0

enum LaserState {
	Inactive,
	Charging,
	Firing
}

var _laserState = LaserState.Inactive
var _laserCooldown: float
var _laserCharge: float
var _laserDuration: float

var _maxVelBase: float


func _ready():
	_maxVelBase = MaxVelocity
	$LaserArea.monitorable = false

func _process(delta: float):
	var distToTarget = (global_position - _target.global_position).length()
	if distToTarget < TargetLaserDist and _laserState == LaserState.Inactive:
		MaxVelocity = 50.0
	else:
		MaxVelocity = _maxVelBase

	# firin' mah lazor
	if _laserState == LaserState.Inactive:
		_laserCooldown -= delta
		if distToTarget < TargetLaserDist and _laserCooldown < 0.0:
			_laserState = LaserState.Charging
			_laserCharge = LaserCharge
			laserCharging()
			
	if _laserState == LaserState.Charging:
		_laserCharge -= delta
		if _laserCharge < 0.0:
			_laserState = LaserState.Firing
			_laserDuration = LaserDuration
			laserFiring()
			
	if _laserState == LaserState.Firing:
		_laserDuration -= delta
		if _laserDuration < 0.0:
			_laserState = LaserState.Inactive
			_laserCooldown = LaserCooldown
			laserInactive()
			
	$LaserArea.state = _laserState
	
func laserCharging():
	$LaserArea.update()
	
func laserFiring():
	$LaserArea.update()
	$LaserArea.monitorable = true
	
func laserInactive():
	$LaserArea.update()
	$LaserArea.monitorable = false
	
func _steeringPursuit(targetPos: Vector2, targetVel: Vector2):
	if _laserState == LaserState.Charging or _laserState == LaserState.Firing:
		return Vector2(0.0, 0.0)
		
	var desiredVelocity = (targetPos - global_position).normalized() * MaxVelocity
	var steering = desiredVelocity - _velocity
	return steering
	
func destroy(score: bool):
	.destroy(score)
	
func _draw():
	var s = 7.0
	var points = PoolVector2Array()
	points.push_back(Vector2(-1.0, -2.0) * s)
	points.push_back(Vector2(0.0, 2.0) * s)
	points.push_back(Vector2(1.0, -2.0) * s)
	var colours = PoolColorArray()
	var col = Colours.Secondary
	colours.push_back(col)
	colours.push_back(col)
	colours.push_back(col)
	draw_polygon(points, colours)
