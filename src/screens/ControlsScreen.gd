extends CanvasLayer

export var GameScene: PackedScene

onready var MusicToggle = get_node("MusicToggle/TextureRect")


func _ready():
	pass
	
func _on_MusicToggle_pressed():
	MusicPlayer.setMusicEnabled(not MusicPlayer._musicEnabled, false)
	MusicToggle.visible = not MusicPlayer._musicEnabled


func _on_Continue_pressed():
	get_tree().change_scene_to(GameScene)
