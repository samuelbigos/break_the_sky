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
    [Export] private float _maxZoom = 500.0f;
    [Export] private float _minZoom = 100.0f;

    [Export] private float _tacticalMapHeight = 1000.0f;

    public static Action OnPostCameraTransformed;
    
    public Transform BaseTransform;
    public Vector2 MousePosition => _cachedMousePos;

    private OpenSimplexNoise _noise = new OpenSimplexNoise();
    private float _trauma = 0.0f;
    private BoidPlayer _player = null;
    private float _noiseY = 0.0f;
    private float _stateTransitionTimer;
    private Transform _initialTrans;
    private Vector2 _cachedMousePos;
    private Vector2 _cachedMousePosOnTacticalPauseEnter;
    
    public void AddTrauma(float trauma)
    {
        _trauma = Mathf.Min(_trauma + trauma, MaxTrauma);
    }

    public void Init(BoidPlayer player)
    {
        _player = player;
    }

    private void UpdateMousePosition()
    {
        Vector2 pos = GetViewport().GetMousePosition();
        Vector3 origin = ProjectRayOrigin(pos);
        Vector3 normal = ProjectRayNormal(pos);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * GlobalTransform.origin.y;
        _cachedMousePos = new Vector2(hit.x, hit.z);
    }

    public Vector3 ProjectToZero(Vector2 screen)
    {
        return ProjectToY(screen, 0.0f);
    }
    
    public Vector3 ProjectToY(Vector2 screen, float y)
    {
        return ProjectPosition(screen, GlobalTransform.origin.y - y);
    }
    
    public Vector2 ScreenPosition(Vector2 screen)
    {
        Vector3 origin = ProjectRayOrigin(screen);
        Vector3 normal = ProjectRayNormal(screen);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * GlobalTransform.origin.y;
        return new Vector2(hit.x, hit.z);
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

        _initialTrans = GlobalTransform;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        
        Debug.Assert(Instance == null, "Attempting to create multiple GlobalCamera instances!");
        Instance = this;
        
        if (Game.Instance != null)
            StateMachine_Game.OnGameStateChanged += _OnGameStateChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        Instance = null;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        UpdateMousePosition();
        
        // ignore timescale
        delta = TimeSystem.UnscaledDelta;
        
        // for clouds
        Debug.Assert(_player != null);
        Vector3 basePos = _player.GlobalTransform.origin;
        basePos.y = GlobalTransform.origin.y;
        BaseTransform = new Transform(GlobalTransform.basis, basePos);

        // set camera transform based on state/transitions
        _stateTransitionTimer -= delta;
        if (_stateTransitionTimer > 0.0f)
        {
            float t = Utils.Ease_CubicInOut(_stateTransitionTimer / _stateTransitionTime);
            GlobalTransform = GetTransform(StateMachine_Game.CurrentState, delta).InterpolateWith(GetTransform(StateMachine_Game.PrevState, delta), t);
        }
        else
        {
            GlobalTransform = GetTransform(StateMachine_Game.CurrentState, delta);
        }

        Vector3 zoom = _initialTrans.origin;
        if (Input.IsActionJustReleased("zoom_in"))
        {
            zoom += Vector3.Down * 5.0f;
        }
        if (Input.IsActionJustReleased("zoom_out"))
        {
            zoom += Vector3.Up * 5.0f;
        }

        zoom.y = Mathf.Clamp(zoom.y, _minZoom, _maxZoom);
        _initialTrans = new Transform(_initialTrans.basis, zoom);
        
        OnPostCameraTransformed?.Invoke();
    }

    private Transform GetTransform(StateMachine_Game.States state, float delta)
    {
        switch (state)
        {
            case StateMachine_Game.States.TacticalPause:
            {
                Transform cameraTransform = new Transform(GlobalTransform.basis, new Vector3(_player.GlobalTransform.origin));
                cameraTransform.origin.y = _tacticalMapHeight;

                return cameraTransform;
            }
            case StateMachine_Game.States.Play:
            {
                Vector2 cameraMouseOffset = MousePosition - _player.GlobalPosition;
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
            case StateMachine_Game.States.Construct:
            {
                Vector2 cameraMouseOffset = MousePosition - _player.GlobalPosition;
                Vector2 cameraOffset = cameraMouseOffset * 0.1f + Vector2.Right * 30.0f;
                Transform cameraTransform = new Transform(GlobalTransform.basis, new Vector3(_player.GlobalTransform.origin + cameraOffset.To3D()));
                cameraTransform.origin.y = _initialTrans.origin.y * 0.5f;

                return cameraTransform;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void _OnGameStateChanged(StateMachine_Game.States state, StateMachine_Game.States prevState)
    {
        _stateTransitionTimer = _stateTransitionTime;

        if (state == StateMachine_Game.States.TacticalPause)
        {
            _cachedMousePosOnTacticalPauseEnter = MousePosition;
        }
    }
}