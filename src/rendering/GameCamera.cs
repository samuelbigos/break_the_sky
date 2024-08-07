using System;
using System.Diagnostics;
using Godot;

public partial class GameCamera : Camera3D
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
    
    public Transform3D BaseTransform;
    public Vector2 MousePosition => _cachedMousePos;

    private FastNoiseLite _noise = new FastNoiseLite();
    private float _trauma = 0.0f;
    private float _noiseY = 0.0f;
    private double _stateTransitionTimer;
    private Transform3D _initialTrans;
    private Vector2 _cachedMousePos;
    private Vector2 _cachedMousePosOnTacticalPauseEnter;
    
    public void AddTrauma(float trauma)
    {
        _trauma = Mathf.Min(_trauma + trauma, MaxTrauma);
    }

    public void Init()
    {
    }

    private void UpdateMousePosition()
    {
        Vector2 pos = GetViewport().GetMousePosition();
        Vector3 origin = ProjectRayOrigin(pos);
        Vector3 normal = ProjectRayNormal(pos);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * GlobalTransform.Origin.Y;
        _cachedMousePos = new Vector2(hit.X, hit.Z);
    }

    public Vector3 ProjectToZero(Vector2 screen)
    {
        return ProjectToY(screen, 0.0f);
    }
    
    public Vector3 ProjectToY(Vector2 screen, float y)
    {
        return ProjectPosition(screen, GlobalTransform.Origin.Y - y);
    }
    
    public Vector2 ScreenPosition(Vector2 screen)
    {
        Vector3 origin = ProjectRayOrigin(screen);
        Vector3 normal = ProjectRayNormal(screen);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * GlobalTransform.Origin.Y;
        return new Vector2(hit.X, hit.Z);
    }
    
    public override void _Ready()
    {
        base._Ready();

        _noise = new FastNoiseLite();

        MaxOffset = MaxOffset * DisplayServer.WindowGetSize();
        GD.Randomize();
        _noise.Seed = (int) GD.Randi();
        _noise.Frequency = 0.25f;
        _noise.FractalOctaves = 2;

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

    public override void _Process(double delta)
    {
        base._Process(delta);

        UpdateMousePosition();
        
        // ignore timescale
        delta = TimeSystem.UnscaledDelta;
        
        // for clouds
        Vector3 basePos = Game.Player.GlobalTransform.Origin;
        basePos.Y = GlobalTransform.Origin.Y;
        BaseTransform = new Transform3D(GlobalTransform.Basis, basePos);

        // set camera transform based on state/transitions
        _stateTransitionTimer -= delta;
        if (_stateTransitionTimer > 0.0f)
        {
            float t = (float) Utils.Ease_CubicInOut(_stateTransitionTimer / _stateTransitionTime);
            GlobalTransform = GetTransform(StateMachine_Game.CurrentState, delta).InterpolateWith(GetTransform(StateMachine_Game.PrevState, delta), t);
        }
        else
        {
            GlobalTransform = GetTransform(StateMachine_Game.CurrentState, delta);
        }

        Vector3 zoom = _initialTrans.Origin;
        if (Input.IsActionJustReleased("zoom_in"))
        {
            zoom += Vector3.Down * 5.0f;
        }
        if (Input.IsActionJustReleased("zoom_out"))
        {
            zoom += Vector3.Up * 5.0f;
        }

        zoom.Y = Mathf.Clamp(zoom.Y, _minZoom, _maxZoom);
        _initialTrans = new Transform3D(_initialTrans.Basis, zoom);
        
        OnPostCameraTransformed?.Invoke();
    }

    private Transform3D GetTransform(StateMachine_Game.States state, double delta)
    {
        BoidAllyBase player = Game.Player;
        switch (state)
        {
            case StateMachine_Game.States.TacticalPause:
            {
                Transform3D cameraTransform = new Transform3D(GlobalTransform.Basis, player.GlobalTransform.Origin);
                cameraTransform.Origin.Y = _tacticalMapHeight;

                return cameraTransform;
            }
            case StateMachine_Game.States.Play:
            {
                Vector2 cameraMouseOffset = MousePosition - player.GlobalPosition;
                Vector2 cameraOffset = cameraMouseOffset * 0.33f;
                Transform3D cameraTransform = new Transform3D(GlobalTransform.Basis, player.GlobalTransform.Origin + cameraOffset.To3D());
                cameraTransform.Origin.Y = _initialTrans.Origin.Y;
                
                if (_trauma > 0.0f)
                {
                    _trauma = (float) Mathf.Max(_trauma - Decay * delta, 0.0f);
                    float amount = Mathf.Pow(_trauma, TraumaPower);
                    float rot = MaxRoll * amount * _noise.GetNoise2D(_noise.Seed, _noiseY);
                    Vector2 offset = new Vector2(0.0f, 0.0f);
                    offset.X = MaxOffset.X * amount * _noise.GetNoise2D(_noise.Seed * 2.0f, _noiseY);
                    offset.Y = MaxOffset.Y * amount * _noise.GetNoise2D(_noise.Seed * 3.0f, _noiseY);
                    _noiseY += (float)delta * 100.0f;
        
                    cameraTransform = cameraTransform.Translated(offset.To3D());
                }

                return cameraTransform;
            }
            case StateMachine_Game.States.Construct:
            {
                Vector2 cameraMouseOffset = MousePosition - player.GlobalPosition;
                Vector2 cameraOffset = cameraMouseOffset * 0.1f + Vector2.Right * 30.0f;
                Transform3D cameraTransform = new Transform3D(GlobalTransform.Basis, player.GlobalTransform.Origin + cameraOffset.To3D());
                cameraTransform.Origin.Y = _initialTrans.Origin.Y * 0.5f;

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