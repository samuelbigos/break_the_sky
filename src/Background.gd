extends Node2D

export var Clouds = []
export var CloudCount = 200
export var CloudSpeed = 50.0
export var Radius = 1000.0
export var Centre = Vector2(0.0, 0.0)
export var HighCloudZ = 2

var _noise = OpenSimplexNoise.new()
var _farClouds = []
var _farCloudSpeeds = []
var _closeClouds = []
var _closeCloudSpeeds = []

func _ready():
	_noise.seed = randi()
	_noise.octaves = 4
	_noise.period = 20.0
	_noise.persistence = 0.8
	
	# clouds
	var bounds = Radius + get_viewport().size.x * 0.5
	for i in range(0, CloudCount):
		var cloud = Sprite.new()
		cloud.texture = Clouds[randi() % Clouds.size()]
		cloud.position = Vector2(rand_range(-bounds, bounds), rand_range(-bounds, bounds))
		#cloud.rotation = rand_range(0.0, PI * 2.0)
		if randi() % 3 == 1:
			cloud.modulate = Colours.White
			var s = rand_range(0.5, 0.75)
			cloud.scale = Vector2(s, s)
			cloud.z_as_relative = false
			cloud.z_index = HighCloudZ
			_closeClouds.append(cloud)
			_closeCloudSpeeds.append(rand_range(CloudSpeed - CloudSpeed * 0.25, CloudSpeed + CloudSpeed * 0.25))
		else:
			cloud.modulate = Colours.Tertiary
			var s = rand_range(1.0, 1.5)
			cloud.scale = Vector2(s, s)
			cloud.z_as_relative = false
			cloud.z_index = -1
			_farClouds.append(cloud)
			_farCloudSpeeds.append(rand_range(CloudSpeed - CloudSpeed * 0.25, CloudSpeed + CloudSpeed * 0.25) * 0.66)
			
		add_child(cloud)
		
func _process(delta):
	for i in range(0, _farClouds.size()):
		var cloud = _farClouds[i]
		cloud.position.y += _farCloudSpeeds[i] * 0.5 * delta
		_reposition(cloud)
		
	for i in range(0, _closeClouds.size()):
		var cloud = _closeClouds[i]
		cloud.position.y += _closeCloudSpeeds[i] * delta
		_reposition(cloud)
		
func _reposition(cloud: Sprite):
	if cloud.global_position.y > Radius + get_viewport().size.y * 0.5:
		cloud.global_position.y = -Radius - get_viewport().size.y * 0.5
		var x = (Radius + get_viewport().size.x * 0.5)
		cloud.global_position.x = rand_range(-x, x)

func _draw():
	var bounds = Radius * 2.0 + get_viewport().size.x * 2.0
	drawArc(Centre, Radius, 0.0, 360.0, Colours.Secondary, 3.0, 128)

func drawArc(center, radius, angleTo, angleFrom, color, thickness, segments):
	var pointNum = segments
	var points = PoolVector2Array()
	for i in range(pointNum + 1):
		var angle = deg2rad(angleFrom + i * (angleTo - angleFrom) / pointNum - 90)
		points.push_back(center + Vector2(cos(angle), sin(angle)) * radius)
	draw_polyline(points, color, thickness)
