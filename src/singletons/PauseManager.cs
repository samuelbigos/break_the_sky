using System.Diagnostics;
using Godot;

public class PauseManager : Node
{
    public static PauseManager Instance;

    private Node _game = null;
    public float _pauseTimer;
    private bool _paused = false;
    private float _pauseFlashTime = 1.0f / 60.0f;

    public PauseManager()
    {
        Debug.Assert(Instance == null, "Attempting to create multiple PauseManager instances!");
        Instance = this;
    }

    public void Init(Game game)
    {
        _game = game;
    }

    public void PauseFlash()
    {
        GetTree().Paused = true;
        _pauseTimer = _pauseFlashTime;
        _paused = true;
    }

    public override void _Process(float delta)
    {
        //if(_paused && !_game.GUI.ShowingPerks())
        {
            _pauseTimer -= delta;
            if (_pauseTimer < 0.0f)
            {
                GetTree().Paused = false;
                _paused = false;
            }
        }
    }
}