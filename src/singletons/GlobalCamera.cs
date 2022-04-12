using Godot;

public class GlobalCamera : Node2D
{
    public static GlobalCamera Instance;

    [Export] public float Decay = 1.0f; // How quickly the shaking stops [0, 1].
    [Export] public Vector2 MaxOffset = new Vector2(0.02f, 0.02f); // Maximum hor/ver shake in pixels.
    [Export] public float MaxRoll = 0.175f; // Maximum rotation in Radians (use sparingly).
    [Export] public int TraumaPower = 2; // Trauma exponent. Use [2, 3].
    [Export] public float MaxTrauma = 0.75f;

    public OpenSimplexNoise _noise = new OpenSimplexNoise();

    private float _trauma = 0.0f;
    private Node2D _player = null;
    private float _noiseY = 0.0f;

    enum WindowScale
    {
        Medium,
        Large,
        Full
    }

    private WindowScale _windowScale = WindowScale.Medium;


    public void AddTrauma(float trauma)
    {
        _trauma = Mathf.Min(_trauma + trauma, MaxTrauma);
    }

    public void Init(Node2D player)
    {
        _player = player;
    }

    public override void _Ready()
    {
        Instance = this;

        _noise = new OpenSimplexNoise();

        MaxOffset = MaxOffset * GetViewportRect().Size;
        GD.Randomize();
        _noise.Seed = (int) GD.Randi();
        _noise.Period = 4;
        _noise.Octaves = 2;

        GetViewport().CanvasTransform =
            new Transform2D(new Vector2(1.0f, 0.0f), new Vector2(0.0f, 1.0f), GetViewport().Size * 0.5f);
    }

    public override void _Process(float delta)
    {
        if (_player != null)
        {
            var cameraMouseOffset = GetGlobalMousePosition() - _player.GlobalPosition;
            var camerOffset = -_player.GlobalPosition + GetViewport().Size * 0.5f - cameraMouseOffset * 0.33f;
            Transform2D cameraTransform =
                new Transform2D(new Vector2(1.0f, 0.0f), new Vector2(0.0f, 1.0f), camerOffset);

            if (_trauma > 0.0f)
            {
                _trauma = Mathf.Max(_trauma - Decay * delta, 0.0f);
                var amount = Mathf.Pow(_trauma, TraumaPower);
                var rot = MaxRoll * amount * _noise.GetNoise2d(_noise.Seed, _noiseY);
                Vector2 offset = new Vector2(0.0f, 0.0f);
                offset.x = MaxOffset.x * amount * _noise.GetNoise2d(_noise.Seed * 2.0f, _noiseY);
                offset.y = MaxOffset.y * amount * _noise.GetNoise2d(_noise.Seed * 3.0f, _noiseY);
                _noiseY += delta * 100.0f;

                cameraTransform = cameraTransform.Rotated(rot);
                cameraTransform = cameraTransform.Translated(offset);
            }

            GetViewport().CanvasTransform = cameraTransform;
        }

        (GetNode("CanvasLayer/Label") as Label).Text = $"{_trauma}";

        if (Input.IsActionJustReleased("fullscreen"))
        {
            switch (_windowScale)
            {
                case WindowScale.Medium:
                    _windowScale = WindowScale.Large;
                    OS.WindowBorderless = false;
                    OS.SetWindowSize(new Vector2(1920, 1080));
                    break;
                case WindowScale.Large:
                    _windowScale = WindowScale.Full;
                    OS.WindowBorderless = true;
                    OS.SetWindowSize(OS.GetScreenSize());
                    OS.SetWindowPosition(new Vector2(0, 0));
                    break;
                case WindowScale.Full:
                    _windowScale = WindowScale.Medium;
                    OS.WindowBorderless = false;
                    OS.SetWindowSize(new Vector2(960, 540));
                    break;
            }
        }
    }
}