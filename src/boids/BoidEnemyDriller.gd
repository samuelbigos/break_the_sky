extends BoidEnemyBase
class_name BoidEnemyDriller

onready var _sfxDestroy = get_node("SFXDestroy")

func _ready():
	$Sprite.modulate = Colours.Secondary
	$Trail.boid = self

func _process(delta: float):
	# update trail
	$Trail.update()
	$Sprite.rotation = -atan2(_velocity.x, _velocity.y)
	
func destroy(score: bool):
	.destroy(score)
	_sfxDestroy.play()