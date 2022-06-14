using Godot;
using System;
using System.Diagnostics;
using Vector3 = Godot.Vector3;

public class BoidTrail : Spatial
{
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
    
    private int _trailIdx;
    private Vector3[] _trailPositions;
    private Spatial _parent;
    private float _updateTimer;
    private Particles _burstParticles;
    private bool _initialised;
    private bool _registered;

    public override void _Ready()
    {
        base._Ready();

        _burstParticles = GetNode<Particles>(_burstParticlesPath);

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

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!_initialised)
        {
            switch (_type)
            {
                case TrailType.Smooth:
                    _trailPositions = new Vector3[_linePoints];
                    _parent = GetParent<Spatial>();
                    for (int i = 0; i < _linePoints; i++)
                    {
                        _trailPositions[i] = _parent.GlobalTransform.origin;
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
                    ParticlesMaterial processMaterial = _burstParticles.ProcessMaterial as ParticlesMaterial;
                    Debug.Assert(processMaterial != null);
                    processMaterial.Color = ColourManager.Instance.White;
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

    private void ProcessBurst(float delta)
    {
    }

    private void ProcessSmooth(float delta)
    {
        _updateTimer -= delta;
        if (_updateTimer < 0.0f)
        {
            _trailIdx = (_trailIdx + 1) % _linePoints;
            _trailPositions[_trailIdx] = _parent.GlobalTransform.origin;
            _updateTimer = _lineInterval;
        }
        GlobalTransform = new Transform(Basis.Identity, Vector3.Zero);
    }
}
