using Godot;

public class MusicPlayer : Node
{
    public static MusicPlayer Instance;
    
    public AudioStreamPlayer player;
    private bool _musicEnabled = true;

    public override void _Ready()
    {
        Instance = this;
        
        player = new AudioStreamPlayer();
        AddChild(player);
        PauseMode = PauseModeEnum.Process;
    }

    public void PlayMenu()
    {
        if (_musicEnabled)
        {
            player.Stream = (AudioStream) GD.Load("res://assets/music/Visager - The Great Tree [Loop].mp3");
            player.VolumeDb = -5;
            player.Play();
        }
    }

    public void PlayGame()
    {
        if (_musicEnabled)
        {
            player.Stream = (AudioStream) GD.Load("res://assets/music/Metre - Taranis.mp3");
            player.VolumeDb = 5;
            player.Play();
        }
    }

    public void SetMusicEnabled(bool enabled, bool game)
    {
        if (_musicEnabled == enabled)
        {
            return;
        }

        if (!enabled)
        {
            player.Stop();
        }

        _musicEnabled = enabled;
        if (enabled)
        {
            if (game)
            {
                PlayGame();
            }
            else
            {
                PlayMenu();
            }
        }
    }
}