using Godot;

public class ControlsScreen : CanvasLayer
{
    [Export] public PackedScene GameScene;
    [Export] public NodePath ContinueButtonPath;
    [Export] public NodePath MusicTogglePath;

    public TextureRect MusicToggle;

    public override void _Ready()
    {
        MusicToggle = GetNode<TextureRect>("MusicToggle/TextureRect");
        GetNode<MenuButton>(ContinueButtonPath).Connect("pressed", this, nameof(OnContinuePressed));
        GetNode<MenuButton>(MusicTogglePath).Connect("pressed", this, nameof(OnMusicTogglePressed));
    }

    public void OnContinuePressed()
    {
        GetTree().ChangeSceneTo(GameScene);
    }

    public void OnMusicTogglePressed()
    {
        MusicPlayer.Instance.SetMusicEnabled(!MusicPlayer.Instance.MusicEnabled, false);
        MusicToggle.Visible = !MusicPlayer.Instance.MusicEnabled;
    }
}