using Godot;

public class MusicPlayer : Node
{
    public static MusicPlayer Instance;
    
    public AudioStreamPlayer2D Player;
    public bool MusicEnabled = true;

    public override void _Ready()
    {
        Instance = this;
        
        Player = new AudioStreamPlayer2D();
        AddChild(Player);
        PauseMode = PauseModeEnum.Process;
    }

    public void PlayMenu()
    {
        if (MusicEnabled)
        {
            Player.Stream = (AudioStream) GD.Load("res://assets/music/Visager - The Great Tree [Loop].mp3");
            Player.VolumeDb = -5;
            Player.Play();
        }
    }

    public void PlayGame()
    {
        if (MusicEnabled)
        {
            Player.Stream = (AudioStream) GD.Load("res://assets/music/Metre - Taranis.mp3");
            Player.VolumeDb = 5;
            Player.Play();
        }
    }

    public void SetMusicEnabled(bool enabled, bool game)
    {
        if (MusicEnabled == enabled)
        {
            return;
        }

        if (!enabled)
        {
            Player.Stop();
        }

        MusicEnabled = enabled;
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