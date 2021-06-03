extends Area2D
class_name BoidBase

export var MaxVelocity = 1000.0
export var Damping = 0.05
export var TrailLength = 5
export var TrailPeriod = 0.05
export var HitParticles: PackedScene

var _velocity: Vector2
var _trailPoints = []
var _trailTimer = 0.0


func _process(delta):
	if TrailLength > 0:
		_trailTimer -= delta
		if _trailTimer < 0.0:
			_trailTimer = TrailPeriod
			_trailPoints.append(global_position)
			if _trailPoints.size() > TrailLength:
				_trailPoints.remove(0)

func _steeringPursuit(targetPos: Vector2, targetVel: Vector2):
	var desiredVelocity = (targetPos - global_position).normalized() * MaxVelocity
	var steering = desiredVelocity - _velocity
	return steering

func _steeringEdgeRepulsion(radius: float):
	var edgeThreshold = 50.0
	var edgeDist = clamp(global_position.length() - (radius - edgeThreshold), 0.0, edgeThreshold) / edgeThreshold
	var desiredVelocity = edgeDist * -global_position.normalized() * MaxVelocity
	var steering = desiredVelocity - _velocity
	return steering
	
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
	
func clampVec(vector: Vector2, v_min: float, v_max: float):
	var length = vector.length()
	if length == 0.0:
		return vector
	var i = vector.length()
	i = clamp(i, v_min, v_max)
	return vector.normalized() * i
