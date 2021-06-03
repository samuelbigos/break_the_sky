extends Node

var player: AudioStreamPlayer


func _ready():
	player = AudioStreamPlayer.new()
	add_child(player)
	pause_mode = PAUSE_MODE_PROCESS
	
func playMenu():
	player.stream = load("res://assets/music/Visager - The Great Tree [Loop].mp3")
	player.volume_db = -5
	player.play()
	
func playGame():
	player.stream = load("res://assets/music/Metre - Taranis.mp3")
	player.volume_db = 5
	player.play()
