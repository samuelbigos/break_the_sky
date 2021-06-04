extends Node

var player: AudioStreamPlayer
var _musicEnabled = true

func _ready():
	player = AudioStreamPlayer.new()
	add_child(player)
	pause_mode = PAUSE_MODE_PROCESS
	
func playMenu():
	if _musicEnabled:
		player.stream = load("res://assets/music/Visager - The Great Tree [Loop].mp3")
		player.volume_db = -5
		player.play()
	
func playGame():
	if _musicEnabled:
		player.stream = load("res://assets/music/Metre - Taranis.mp3")
		player.volume_db = 5
		player.play()

func setMusicEnabled(enabled: bool, game: bool):
	if _musicEnabled == enabled:
		return
		
	if not enabled:
		player.stop()
		
	_musicEnabled = enabled
	if enabled:
		if game:
			playGame()
		else:
			playMenu()
