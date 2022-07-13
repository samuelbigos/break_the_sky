using Godot;
using System;
using GodotOnReady.Attributes;

public partial class SeekerMissile : Area
{
    [Export] private float _steeringRadius = 5.0f;
    [Export] private float _maxSpeed = 200.0f;
    [Export] private float _maxForce = 50.0f;
    [Export] private float _selfDestructTime = 5.0f;
    
    [OnReadyGet] private Particles _explodeVfx;
    [OnReadyGet] private AudioStreamPlayer2D _launchSfx;

    private BoidBase.BoidAlignment _alignment;
    private float _damage;
    private int _steeringId;
    private float _selfDestructTimer;
    
    public void Init(Vector2 spawnPos, BoidBase.BoidAlignment alignment, BoidBase target)
    {
        _selfDestructTimer = _selfDestructTime;
        _alignment = alignment;
        
        Connect("area_entered", this, nameof(_OnAreaEntered));
        
        int behaviours = 0;
        behaviours |= (1 << (int) SteeringManager.Behaviours.Pursuit);
        
        SteeringManager.Boid boid = new()
        {
            Alignment = (byte)alignment,
            Radius = _steeringRadius,
            Position = spawnPos.ToNumerics(),
            Velocity = System.Numerics.Vector2.Zero,
            Heading = System.Numerics.Vector2.UnitY,
            Target = System.Numerics.Vector2.Zero,
            TargetIndex = target.SteeringId,
            Behaviours = behaviours,
            MaxSpeed = _maxSpeed,
            MinSpeed = 0.0f,
            MaxForce = _maxForce,
            DesiredSpeed = 0.0f,
            LookAhead = 1.0f,
            ViewRange = 50.0f,
            ViewAngle = 360.0f,
            WanderCircleDist = 25.0f,
            WanderCircleRadius = 5.0f,
            WanderVariance = 50.0f,
            Ignore = true
        };
        
        target.OnBoidDestroyed += _OnTargetBoidDestroyed;
        
        _steeringId = SteeringManager.Instance.RegisterBoid(boid);
        
        _launchSfx.Play();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        Basis basis = new Basis(Vector3.Down, steeringBoid.Heading.Angle() + Mathf.Pi * 0.5f);
        GlobalTransform = new Transform(basis, steeringBoid.Position.ToGodot().To3D());

        _selfDestructTimer -= delta;
        if (_selfDestructTimer < 0.0f)
        {
            Destroy();
        }
    }

    private void Destroy()
    {
        SteeringManager.Instance.RemoveBoid(_steeringId);
        QueueFree();
    }

    public virtual void _OnAreaEntered(Area area)
    {
        if (area is BoidBase boid && boid.Alignment != _alignment)
        {
            ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
            boid.SendHitMessage(_damage, steeringBoid.VelocityG, steeringBoid.PositionG, _alignment);

            _selfDestructTimer = 0.0f;
        }
    }

    private void _OnTargetBoidDestroyed(BoidBase target)
    {
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        steeringBoid.TargetIndex = -1;
    }
}
