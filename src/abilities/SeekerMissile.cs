using Godot;
using GodotOnReady.Attributes;

public partial class SeekerMissile : Area
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

    [OnReadyGet] private MultiViewportMeshInstance _mesh;
    [OnReadyGet] private AudioStreamPlayer2D _launchSfx;
    [OnReadyGet] private BoidTrail _trail;
    [OnReadyGet] private CollisionShape _collisionShape;
    
    private BoidBase.BoidAlignment _alignment;
    private float _damage;
    private int _steeringId;
    private float _deactivationTimer;
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
        _mesh.Transform = new Transform(_mesh.Transform.basis, _mesh.Transform.origin + Vector3.Up * position.y);
        _mesh.SetMeshTransform(_mesh.Transform);
        
        Connect("area_entered", this, nameof(_OnAreaEntered));
        
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
        
        _mesh.AltShaders[0].SetShaderParam("u_outline_colour", _alignment == BoidBase.BoidAlignment.Ally ? 
            ColourManager.Instance.AllyOutline : ColourManager.Instance.EnemyOutline);
    }

    public override void _Process(float delta)
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
                GlobalTransform = new Transform(basis, steeringBoid.PositionG.To3D());

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
                    Particles p = ParticleManager.Instance.AddOneShotParticles(Resources.Instance.DustCloudVFX, GlobalTransform.origin);
                    ParticleManager.Instance.AddOneShotParticles(Resources.Instance.ExplodeVFX, GlobalTransform.origin);
                    QueueFree();
                }

                break;
            }
        }
    }

    private void Deactivate()
    {
        // convert to rigid body for 'ragdoll' death physics.
        Vector3 pos = GlobalTransform.origin;
        
        // TODO: pool RigidBodies.
        RigidBody rb = new RigidBody();
        GetParent().AddChild(rb);
        rb.GlobalTransform = GlobalTransform;
        GetParent().RemoveChild(this);  
        rb.AddChild(this);
        CollisionShape shape = _collisionShape;
        rb.AddChild(shape.Duplicate());

        rb.GlobalTransform = new Transform(Basis.Identity, pos);
        GlobalTransform = new Transform(GlobalTransform.basis, pos);
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        rb.ApplyCentralImpulse(steeringBoid.Velocity.ToGodot().To3D());
        rb.ApplyTorqueImpulse(new Vector3(Utils.Rng.Randf(), Utils.Rng.Randf(), Utils.Rng.Randf()) * 1.0f);
        
        rb.Connect("body_entered", this, nameof(_WhenTheBodyHitsTheFloor));
        rb.ContactMonitor = true;
        rb.ContactsReported = 2;

        SteeringManager.Instance.Unregister<SteeringManager.Boid>(_steeringId);
        _trail.QueueFree();
        _state = State.Deactivated;
        Monitoring = false;
    }

    private void Explode()
    {
        _state = State.Deactivated;
        SteeringManager.Instance.Unregister<SteeringManager.Boid>(_steeringId);
        ParticleManager.Instance.AddOneShotParticles(Resources.Instance.ExplodeVFX, GlobalTransform.origin);
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

    public virtual void _OnAreaEntered(Area area)
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
