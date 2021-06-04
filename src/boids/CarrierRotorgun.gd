extends BoidEnemyBase
class_name CarrierRotorgun

export var BulletScene: PackedScene
export var BulletSpeed: float = 200.0
export var BulletRange = 500.0
export var BulletCooldown = 1.0

onready var _sfxDestroy = get_node("SFXDestroy")
onready var _blade = get_node("Blade")
onready var _sfxFire = get_node("SFXFire")

var lock: Node2D
var _rotVel = PI * 2.0
var _shotCooldown: float


func _ready():
	_sprite.modulate = Colours.Secondary
	_blade.modulate = Colours.Secondary	
	$Damaged.process_material = $Damaged.process_material.duplicate(true)
	
func _process(delta):
	_blade.scale = _sprite.scale
	_blade.rotation = fmod(_blade.rotation + 50.0 * delta, PI * 2.0)
	
#	var toTarget = (_target.global_position - global_position).normalized()
#	var awayParent = (lock.global_position - global_position).normalized()
#	var rotAwayParent = -atan2(awayParent.x, awayParent.y) - get_parent().rotation + PI
#	var rot = -atan2(toTarget.x, toTarget.y) - get_parent().rotation + PI
#	rot = clamp(rot + PI * 2.0, rotAwayParent - PI * 0.5 + PI * 2.0, rotAwayParent + PI * 0.5 + PI * 2.0)
#	rotation = rot

	var s = clamp(1.0 - _health / MaxHealth, 0.2, 1.0)
	$Damaged.scale = Vector2(s * 10.0, s * 10.0)
	$Damaged.process_material.scale = s * 5.0

	if not _destroyed:
		#var awayLock = (lock.global_position - global_position).normalized()
		var toTarget = (_target.global_position - global_position).normalized()
		var rot = -atan2(toTarget.x, toTarget.y) - get_parent().rotation + PI
		rotation = rot
		
		var awayParent = (lock.global_position - global_position).normalized()
		var dist = (_target.global_position - global_position).length()
		_shotCooldown -= delta
		if toTarget.dot(awayParent) > 0.0 and _shotCooldown < 0.0 and dist < BulletRange:
			_shoot()
			_shotCooldown = BulletCooldown
		
func _shoot():
	var bullet = BulletScene.instance()
	var dir = (_target.global_position - global_position).normalized()
	bullet.init(dir * BulletSpeed, 1, _game.PlayRadius)
	bullet.global_position = global_position + dir * 80.0
	_game.add_child(bullet)
	_sfxFire.play()

func destroy(score: bool):
	.destroy(score)
	_sfxDestroy.play()
	var pos = global_position
	get_parent().remove_child(self)
	_game.add_child(self)
	global_position = pos
	_blade.visible = false
