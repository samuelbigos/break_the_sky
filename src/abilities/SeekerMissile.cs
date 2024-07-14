using Godot;

public partial class SeekerMissile : Area3D
{
    protected enum State
    {
        Alive,
        Deactivated
    }
    
    [Export] private float _steeringRadius = 5.0f;
    [Export] private float _maxSpeed = 200.0f;
    [Export] private float _maxForce = 50.0f;
    [Export] private float _lifetime = 5.0f;

    [Export] private MultiViewportMeshInstance _mesh;
    [Export] private AudioStreamPlayer2D _launchSfx;
    [Export] private BoidTrail _trail;
    [Export] private CollisionShape3D _collisionShape;
    
    private BoidBase.BoidAlignment _alignment;
    private float _damage;
    private int _steeringId;
    private double _deactivationTimer;
    private State _state = State.Alive;
    private System.Numerics.Vector2 _smoothHeading;
    private bool _queueExplode;
    private bool _hitGround;
    
    public void Init(float damage, Vector3 position, Vector2 velocity, BoidBase.BoidAlignment alignment, BoidBase target)
    {
        this.GlobalPosition(position.To2D());
        _damage = damage;
        _deactivationTimer = _lifetime;
        _alignment = alignment;
        _mesh.Transform = new Transform3D(_mesh.Transform.Basis, _mesh.Transform.Origin + Vector3.Up * position.Y);
        _mesh.SetMeshTransform(_mesh.Transform);
        
        Connect("area_entered", new Callable(this, nameof(_OnAreaEntered)));
        
        int behaviours = 0;
        behaviours |= (1 << (int) SteeringManager.Behaviours.Pursuit);
        
        SteeringManager.Boid boid = new()
        {
            Alignment = (byte)alignment,
            Radius = _steeringRadius,
            Position = position.To2D().ToNumerics(),
            Velocity = velocity.ToNumerics(),
            Heading = velocity.ToNumerics().NormalizeSafe(),
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

        if (!SteeringManager.Instance.Register(boid, out _steeringId))
        {
            QueueFree();
            return;
        }
        
        _launchSfx.Play();
        
        _mesh.AltShaders[0].SetShaderParameter("u_outline_colour", _alignment == BoidBase.BoidAlignment.Ally ? 
            ColourManager.Instance.AllyOutline : ColourManager.Instance.EnemyOutline);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        switch (_state)
        {
            case State.Alive:
            {
                ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
            
                // head the missile in the steering direction, but as steering strength approaches zero, mix in more heading (velocity) because steering direction is
                // less accurate at lower strengths.
                float steeringStrength = Mathf.Clamp(steeringBoid.Steering.Length() * 2.5f, 0.0f, 1.0f);
                float smoothFactor = 0.95f;
                _smoothHeading = _smoothHeading * smoothFactor + (1.0f - smoothFactor) * 
                    System.Numerics.Vector2.Lerp(steeringBoid.Heading, steeringBoid.Steering.NormalizeSafe(), steeringStrength);

                Basis basis = new(Vector3.Up, _smoothHeading.AngleToY());
                GlobalTransform = new Transform3D(basis, steeringBoid.PositionG.To3D());

                _trail.Thrust = -_smoothHeading.ToGodot();

                _deactivationTimer -= delta;
                if (_deactivationTimer < 0.0f)
                {
                    Deactivate();
                }

                if (_queueExplode)
                {
                    _queueExplode = false;
                    Explode();
                }

                break;
            }
            case State.Deactivated:
            {
                if (_hitGround)
                {
                    ParticleManager.Instance.AddOneShotParticles(Resources.Instance.DustCloudVFX, GlobalTransform.Origin, out _);
                    ParticleManager.Instance.AddOneShotParticles(Resources.Instance.ExplodeVFX, GlobalTransform.Origin, out _);
                    QueueFree();
                }

                break;
            }
        }
    }

    private void Deactivate()
    {
        // convert to rigid body for 'ragdoll' death physics.
        Vector3 pos = GlobalTransform.Origin;
        
        // TODO: pool RigidBodies.
        RigidBody3D rb = new RigidBody3D();
        GetParent().AddChild(rb);
        rb.GlobalTransform = GlobalTransform;
        GetParent().RemoveChild(this);  
        rb.AddChild(this);
        CollisionShape3D shape = _collisionShape;
        rb.AddChild(shape.Duplicate());

        rb.GlobalTransform = new Transform3D(Basis.Identity, pos);
        GlobalTransform = new Transform3D(GlobalTransform.Basis, pos);
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        rb.ApplyCentralImpulse(steeringBoid.Velocity.ToGodot().To3D());
        rb.ApplyTorqueImpulse(new Vector3(Utils.Rng.Randf(), Utils.Rng.Randf(), Utils.Rng.Randf()) * 1.0f);
        
        rb.Connect("body_entered", new Callable(this, nameof(_WhenTheBodyHitsTheFloor)));
        rb.ContactMonitor = true;
        rb.MaxContactsReported = 2;

        SteeringManager.Instance.Unregister<SteeringManager.Boid>(_steeringId);
        _trail.QueueFree();
        _state = State.Deactivated;
        Monitoring = false;
    }

    private void Explode()
    {
        _state = State.Deactivated;
        SteeringManager.Instance.Unregister<SteeringManager.Boid>(_steeringId);
        ParticleManager.Instance.AddOneShotParticles(Resources.Instance.ExplodeVFX, GlobalTransform.Origin, out _);
        QueueFree();

        Monitorable = false;
        Monitoring = false;
    }
    
    protected void _WhenTheBodyHitsTheFloor(Node body)
    {
        if (body.IsInGroup("ground"))
        {
            _hitGround = true;
        }
    }

    public virtual void _OnAreaEntered(Area3D area)
    {
        if (_state == State.Deactivated)
            return;
        
        if (area is BoidBase boid && boid.Alignment != _alignment)
        {
            ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
            boid.SendHitMessage(_damage, steeringBoid.VelocityG, steeringBoid.PositionG, _alignment);

            _queueExplode = true;
        }
    }

    private void _OnTargetBoidDestroyed(BoidBase target)
    {
        if (_state == State.Deactivated)
            return;
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        steeringBoid.TargetIndex = -1;
    }
}
