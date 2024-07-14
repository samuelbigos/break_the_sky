using Godot;
using System;
using System.Diagnostics;
using Vector3 = Godot.Vector3;

public partial class BoidTrail : Node3D
{
    // TODO: Burst trail should emit in the direction of steering forces if attached to a boid.
    
    public enum TrailType
    {
        Smooth,
        Burst
    }

    [Export(PropertyHint.Flags, "Smooth,Burst")] private TrailType _type = TrailType.Smooth;
    [Export] private int _linePoints = 100;
    [Export] private float _lineInterval = 1.0f / 60.0f;
    [Export] private float _lineWidth = 10.0f;
    [Export] private Curve _lineWidthCurve;
    [Export] private NodePath _burstParticlesPath;

    public int LinePoints => _linePoints;
    public Vector3[] TrailPositions => _trailPositions;
    public int TrailIdx => _trailIdx;
    public Curve LineWidthCurve => _lineWidthCurve;
    public float LineWidth => _lineWidth;
    public Vector2 Thrust { set => _thrust = value; }
    
    private int _trailIdx;
    private Vector3[] _trailPositions;
    private double _updateTimer;
    private GpuParticles3D _burstParticles;
    private ParticleProcessMaterial _burstMaterial;
    private bool _initialised;
    private bool _registered;
    private Vector2 _thrust;

    public override void _Ready()
    {
        base._Ready();

        _burstParticles = GetNode<GpuParticles3D>(_burstParticlesPath);
        _burstMaterial = _burstParticles.ProcessMaterial as ParticleProcessMaterial;;

        _initialised = false;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (_type == TrailType.Smooth && _registered)
        {
            TrailRenderer.Instance.UnRegister(this);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!_initialised)
        {
            switch (_type)
            {
                case TrailType.Smooth:
                    _trailPositions = new Vector3[_linePoints];
                    for (int i = 0; i < _linePoints; i++)
                    {
                        _trailPositions[i] = GlobalTransform.Origin;
                    }
                    _burstParticles.Visible = false;
                    if (Visible)
                    {
                        TrailRenderer.Instance.Register(this);
                        _registered = true;
                    }
                    break;
                case TrailType.Burst:
                    _burstParticles.Visible = true;
                    ParticleProcessMaterial processMaterial = _burstParticles.ProcessMaterial as ParticleProcessMaterial;
                    DebugUtils.Assert(processMaterial != null, "processMaterial != null");
                    processMaterial.Color = ColourManager.Instance.White;
                    _burstParticles.Emitting = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _initialised = true;
        }

        switch (_type)
        {
            case TrailType.Smooth:
                ProcessSmooth(delta);
                break;
            case TrailType.Burst:
                ProcessBurst(delta);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ProcessBurst(double delta)
    {
        // TODO: take parent velocity into account.
        _burstMaterial.InitialVelocityMin = _thrust.Length() * 50.0f;
        _burstMaterial.InitialVelocityMax = _thrust.Length() * 50.0f;
    }

    private void ProcessSmooth(double delta)
    {
        _updateTimer -= delta;
        if (_updateTimer < 0.0f)
        {
            _trailIdx = (_trailIdx + 1) % _linePoints;
            _trailPositions[_trailIdx] = GlobalTransform.Origin;
            _updateTimer = _lineInterval;
        }
    }
}
