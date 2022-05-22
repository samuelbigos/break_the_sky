using System;
using System.Diagnostics;
using Godot;

public class GameCamera : Camera
{
    public static GameCamera Instance;

    [Export] public float Decay = 1.0f; // How quickly the shaking stops [0, 1].
    [Export] public Vector2 MaxOffset = new Vector2(0.02f, 0.02f); // Maximum hor/ver shake in pixels.
    [Export] public float MaxRoll = 0.175f; // Maximum rotation in Radians (use sparingly).
    [Export] public int TraumaPower = 2; // Trauma exponent. Use [2, 3].
    [Export] public float MaxTrauma = 0.75f;
    [Export] private float _stateTransitionTime = 0.5f;

    public Transform BaseTransform;

    private OpenSimplexNoise _noise = new OpenSimplexNoise();
    private float _trauma = 0.0f;
    private BoidPlayer _player = null;
    private float _noiseY = 0.0f;
    private float _stateTransitionTimer;
    private Transform _initialTrans;
    
    public void AddTrauma(float trauma)
    {
        _trauma = Mathf.Min(_trauma + trauma, MaxTrauma);
    }

    public void Init(BoidPlayer player)
    {
        _player = player;
    }

    public Vector2 MousePosition()
    {
        Vector2 pos = GetViewport().GetMousePosition();
        Vector3 origin = ProjectRayOrigin(pos);
        Vector3 normal = ProjectRayNormal(pos);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * GlobalTransform.origin.y;
        return new Vector2(hit.x, hit.z);
    }

    public Vector3 ProjectToZero(Vector2 screen)
    {
        return ProjectPosition(screen, GlobalTransform.origin.y);
    }
    
    public override void _Ready()
    {
        base._Ready();

        _noise = new OpenSimplexNoise();

        MaxOffset = MaxOffset * GetViewport().Size;
        GD.Randomize();
        _noise.Seed = (int) GD.Randi();
        _noise.Period = 4;
        _noise.Octaves = 2;

        VisualServer.SetDefaultClearColor(ColourManager.Instance.Primary);

        _initialTrans = GlobalTransform;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        
        Debug.Assert(Instance == null, "Attempting to create multiple GlobalCamera instances!");
        Instance = this;
        
        if (Game.Instance != null)
            Game.Instance.OnGameStateChanged += _OnGameStateChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        Instance = null;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        // ignore timescale
        delta = TimeSystem.UnscaledDelta;
        
        VisualServer.SetDefaultClearColor(ColourManager.Instance.Primary);
        
        Debug.Assert(_player != null);
        
        // for clouds
        Vector3 basePos = _player.GlobalTransform.origin;
        basePos.y = GlobalTransform.origin.y;
        BaseTransform = new Transform(GlobalTransform.basis, basePos);

        // set camera transform based on state/transitions
        _stateTransitionTimer -= delta;
        if (_stateTransitionTimer > 0.0f)
        {
            float t = Utils.Ease_CubicInOut(_stateTransitionTimer / _stateTransitionTime);
            GlobalTransform = GetTransform(Game.Instance.CurrentState, delta).InterpolateWith(GetTransform(Game.Instance.PrevState, delta), t);
        }
        else
        {
            GlobalTransform = GetTransform(Game.Instance.CurrentState, delta);
        }
    }

    private Transform GetTransform(Game.State state, float delta)
    {
        switch (state)
        {
            case Game.State.Play:
            {
                Vector2 cameraMouseOffset = MousePosition() - _player.GlobalPosition;
                Vector2 cameraOffset = cameraMouseOffset * 0.33f;
                Transform cameraTransform = new Transform(GlobalTransform.basis, new Vector3(_player.GlobalTransform.origin + cameraOffset.To3D()));
                cameraTransform.origin.y = _initialTrans.origin.y;
                
                if (_trauma > 0.0f)
                {
                    _trauma = Mathf.Max(_trauma - Decay * delta, 0.0f);
                    float amount = Mathf.Pow(_trauma, TraumaPower);
                    float rot = MaxRoll * amount * _noise.GetNoise2d(_noise.Seed, _noiseY);
                    Vector2 offset = new Vector2(0.0f, 0.0f);
                    offset.x = MaxOffset.x * amount * _noise.GetNoise2d(_noise.Seed * 2.0f, _noiseY);
                    offset.y = MaxOffset.y * amount * _noise.GetNoise2d(_noise.Seed * 3.0f, _noiseY);
                    _noiseY += delta * 100.0f;
        
                    cameraTransform = cameraTransform.Translated(offset.To3D());
                }

                return cameraTransform;
            }
            case Game.State.Construct:
            {
                Vector2 cameraMouseOffset = MousePosition() - _player.GlobalPosition;
                Vector2 cameraOffset = cameraMouseOffset * 0.1f + Vector2.Right * 30.0f;
                Transform cameraTransform = new Transform(GlobalTransform.basis, new Vector3(_player.GlobalTransform.origin + cameraOffset.To3D()));
                cameraTransform.origin.y = _initialTrans.origin.y * 0.5f;

                return cameraTransform;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void _OnGameStateChanged(Game.State state, Game.State prevState)
    {
        switch (state)
        {
            case Game.State.Play:
                _stateTransitionTimer = _stateTransitionTime;
                break;
            case Game.State.Construct:
                _stateTransitionTimer = _stateTransitionTime;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}