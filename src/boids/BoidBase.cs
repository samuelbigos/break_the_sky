using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Godot;
using GodotOnReady.Attributes;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

public partial class BoidBase : Area
{
    public enum BoidAlignment
    {
        Ally,
        Enemy
    };

    public enum TargetType
    {
        None,
        Ally,
        Enemy,
        Position
    }

    protected enum State
    {
        Alive,
        Destroyed
    }

    #region Export

    [Export(PropertyHint.Flags, "Separation,AvoidObstacles,AvoidAllies,AvoidEnemies,MaintainSpeed,Cohesion,Alignment,Arrive,Pursuit,Flee,Wander,FlowFieldFollow")] protected int _behaviours;
    [Export] private float _steeringRadius = 5.0f;
    
    [Export] public float MaxVelocity = 500.0f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float MaxForce = 150.0f;
    [Export] public float FieldOfView = 360.0f;
    [Export] public bool Bank360 = false;
    [Export] public float BankingRate = 2.5f;
    [Export] public float BankingAmount = 2.5f;
    
    [Export] public float Damping = 0.05f;
    [Export] public float DestroyTime = 3.0f;
    [Export] public float HitDamage = 3.0f;
    [Export] public float MaxHealth = 1.0f;
    [Export] public float HitFlashTime = 1.0f / 30.0f;
    [Export] public int Points = 10;
    
    [Export] private int _damageVfxCount = 2;
    
    [Export] private List<AudioStream> _hitSfx;

    [Export] private NodePath _meshPath;
    [Export] private NodePath _sfxDestroyPath;
    [Export] private NodePath _sfxHitPlayerPath;

    [Export] private PackedScene _hitParticlesScene;
    [Export] private PackedScene _damagedParticlesScene;
    [Export] private PackedScene _destroyParticlesScene;
    
    [Export] protected PackedScene _pickupMaterialScene;
    
    [OnReadyGet] protected MeshInstance _selectedIndicator;
    [OnReadyGet] protected MultiViewportMeshInstance _mesh;

    #endregion

    #region Signals

    public Action<BoidBase> OnBoidDestroyed;

    #endregion

    #region Public

    protected virtual BoidAlignment Alignment => BoidAlignment.Ally;
    public bool Destroyed => _state == State.Destroyed;    
    public string Id = "";
    public int SteeringId => _steeringId;
    
    public Vector2 GlobalPosition
    {
        get => new(GlobalTransform.origin.x, GlobalTransform.origin.z);
        protected set
        {
            if (SteeringManager.Instance.HasBoid(_steeringId))
            {
                ref SteeringManager.Boid boid = ref SteeringManager.Instance.GetBoid(_steeringId);
                boid.Position = value;
            }
        }
    }

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            _selectedIndicator.Visible = _selected;
        }
    }

    public Vector2 TargetPos
    {
        get
        {
            switch (_targetType)
            {
                case TargetType.Ally:
                case TargetType.Enemy:
                    return _targetBoid.GlobalPosition;
                case TargetType.Position:
                    return _targetPos;
            }
            return GlobalPosition;
        }
    }

    #endregion

    #region Protected

    protected int _steeringId;
    protected TargetType _targetType = TargetType.None;
    protected BoidBase _targetBoid;
    protected Vector2 _targetPos;
    protected Vector3 _baseScale;
    protected bool _acceptInput = true;
    protected AudioStreamPlayer2D _sfxOnHit;
    protected Vector2 _cachedVelocity;
    protected State _state;

    #endregion

    #region Private

    private float _health;
    private float _hitFlashTimer;
    private List<Particles> _damagedParticles = new();
    protected ShaderMaterial _meshMaterial;
    private ShaderMaterial _altMaterial;
    private List<Particles> _hitParticles = new();
    private AudioStreamPlayer2D _sfxOnDestroy;
    private Vector3 _cachedLastHitDir;
    private float _cachedLastHitDamage;
    private bool _selected;
    private Vector2 _smoothSteering;
    
    private float[] _steeringWeights = new float[(int) SteeringManager.Behaviours.COUNT];

    protected virtual void SetMeshColour()
    {
        _meshMaterial?.SetShaderParam("u_primary_colour", ColourManager.Instance.Secondary);
        _meshMaterial?.SetShaderParam("u_secondary_colour", ColourManager.Instance.Ally);
    }
    private void SetMeshColour(Color col)
    {
        _meshMaterial?.SetShaderParam("u_primary_colour", col);
    }

    #endregion

    public virtual void Init(string id, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        _state = State.Alive;
        Id = id;
        OnBoidDestroyed += onDestroy;
        RegisterSteeringBoid(velocity);
        GlobalPosition = position;
    }

    protected void RegisterSteeringBoid(Vector2 velocity)
    {
        _steeringWeights[(int) SteeringManager.Behaviours.Separation] = 2.0f;
        _steeringWeights[(int) SteeringManager.Behaviours.AvoidObstacles] = 2.0f;
        _steeringWeights[(int) SteeringManager.Behaviours.AvoidAllies] = 2.0f;
        _steeringWeights[(int) SteeringManager.Behaviours.AvoidEnemies] = 2.0f;
        _steeringWeights[(int) SteeringManager.Behaviours.MaintainSpeed] = 0.1f;
        _steeringWeights[(int) SteeringManager.Behaviours.Cohesion] = 0.1f;
        _steeringWeights[(int) SteeringManager.Behaviours.Alignment] = 0.1f;
        _steeringWeights[(int) SteeringManager.Behaviours.Arrive] = 2.0f;
        _steeringWeights[(int) SteeringManager.Behaviours.Pursuit] = 1.0f;
        _steeringWeights[(int) SteeringManager.Behaviours.Flee] = 1.0f;
        _steeringWeights[(int) SteeringManager.Behaviours.EdgeRepulsion] = 1.0f;
        _steeringWeights[(int) SteeringManager.Behaviours.Wander] = 0.1f;
        _steeringWeights[(int) SteeringManager.Behaviours.FlowFieldFollow] = 1.0f;

        SteeringManager.Boid boid = new()
        {
            Alignment = 1,
            Position = GlobalPosition,
            Velocity = velocity,
            Radius = _steeringRadius,
            Heading = Vector2.Up,
            MaxSpeed = MaxVelocity,
            MinSpeed = MinVelocity,
            MaxForce = MaxForce,
            DesiredSpeed = 75.0f,
            LookAhead = 1.0f,
            Behaviours = _behaviours,
            Weights = _steeringWeights,
            Target = Vector2.Zero,
            ViewRange = 50.0f,
            ViewAngle = 240.0f,
            TargetIndex = -1,
        };
        _steeringId = SteeringManager.Instance.RegisterBoid(boid);
    }

    [OnReady] private void Ready()
    {
        _health = MaxHealth;

        //_mesh = GetNode<MultiViewportMeshInstance>(_meshPath);
        _sfxOnDestroy = GetNode<AudioStreamPlayer2D>(_sfxDestroyPath);
        _sfxOnHit = GetNode<AudioStreamPlayer2D>(_sfxHitPlayerPath);

        _sfxOnHit.Stream = _hitSfx[0];
        _baseScale = _mesh.Scale;
        
        List<MeshInstance> altMeshes = _mesh.AltMeshes;
        Debug.Assert(altMeshes.Count > 0);
        _meshMaterial = _mesh.MaterialOverride as ShaderMaterial;
        _mesh.SetSurfaceMaterial(0, _meshMaterial);
        _altMaterial = altMeshes[0].GetActiveMaterial(0) as ShaderMaterial;
        
        SetMeshColour();
        
        Connect("area_entered", this, nameof(_OnBoidAreaEntered));
        
        if (Game.Instance != null)
            StateMachine_Game.OnGameStateChanged += _OnGameStateChanged;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        switch (_state)
        {
            case State.Alive:
                ProcessAlive(delta);
                break;
            case State.Destroyed:
                ProcessDestroyed(delta);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //_altMaterial.SetShaderParam("u_velocity", _cachedVelocity);
    }

    protected virtual void ProcessAlive(float delta)
    {
        // update position and cache velocity from steering boid
        Debug.Assert(SteeringManager.Instance.HasBoid(_steeringId));
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        GlobalTransform = new Transform(new Basis(Vector3.Down, steeringBoid.Heading.Angle() + Mathf.Pi * 0.5f), steeringBoid.Position.To3D());
        _cachedVelocity = steeringBoid.Velocity;

        // hit flash
        if (_hitFlashTimer > 0.0 && _hitFlashTimer - delta < 0.0f)
        {
            SetMeshColour();
        }
        _hitFlashTimer -= delta;

        if (_health < 0.0f)
        {
            _Destroy(Points > 0, _cachedLastHitDir, _cachedLastHitDamage);
            return;
        }
        _cachedLastHitDir = Vector3.Zero;
        _cachedLastHitDamage = 0.0f;
        
        // banking
        Vector2 right = new(steeringBoid.Heading.y, -steeringBoid.Heading.x);
        Vector2 localSteering = Utils.LocaliseDirection(steeringBoid.Steering, steeringBoid.Heading, right);
        _smoothSteering = _smoothSteering.LinearInterpolate(localSteering, Mathf.Clamp(delta * BankingRate, 0.0f, 1.0f));
        _mesh.Rotation = Bank360 ? 
            new Vector3(_smoothSteering.Dot(Vector2.Up) * BankingAmount, 0.0f,_smoothSteering.Dot(Vector2.Right) * BankingAmount) : 
            new Vector3(0.0f, 0.0f, _smoothSteering.Dot(Vector2.Right) * BankingAmount);
    }
    
    protected virtual void ProcessDestroyed(float delta)
    {
        if (GlobalTransform.origin.y < -100.0f)
        {
            foreach (Particles p in _hitParticles)
            {
                p.QueueFree();
            }
            BoidFactory.Instance.FreeBoid(this);
        }
    }

    protected virtual void _OnHit(float damage, bool score, Vector2 bulletVel, Vector3 pos)
    {
        _health -= damage;

        SetMeshColour(ColourManager.Instance.White);
        _hitFlashTimer = HitFlashTime;
        
        _sfxOnHit.Play();

        if (bulletVel != Vector2.Zero)
        {
            Particles hitParticles = _hitParticlesScene.Instance<Particles>();
            Game.Instance.AddChild(hitParticles);
            ParticlesMaterial mat = hitParticles.ProcessMaterial as ParticlesMaterial;
            Debug.Assert(mat != null);
            Vector3 fromCentre = pos - GlobalTransform.origin;
            mat.Direction = -bulletVel.Reflect(fromCentre.Normalized().To2D()).To3D();
            hitParticles.GlobalPosition(pos + Vector3.Up * 5.0f);
            hitParticles.Emitting = true;
            _hitParticles.Add(hitParticles);
        }

        // does this cause us to go past a new damage threshold?
        if (_damagedParticles.Count < _damageVfxCount)
        {
            float damageVfxThresholds = 1.0f / (_damageVfxCount + 1.0f);
            float nextThreshold = 1.0f - (_damagedParticles.Count + 1.0f) * damageVfxThresholds;
            if (_health / MaxHealth < nextThreshold)
            {
                Particles particles = _damagedParticlesScene.Instance<Particles>();
                _damagedParticles.Add(particles);
                AddChild(particles);
                particles.GlobalPosition(pos + Vector3.Up * 5.0f);
            }
        }

        _cachedLastHitDir = bulletVel.To3D().Normalized();
        _cachedLastHitDamage = damage;
    }
    
    protected virtual void _Destroy(bool score, Vector3 hitDir, float hitStrength)
    {
        Debug.Assert(_state != State.Destroyed, "_state != State.Destroyed");
        if (_state != State.Destroyed)
        {
            _state = State.Destroyed;
            
            SetMeshColour();
            
            _sfxOnDestroy.Play();
            Disconnect("area_entered", this, nameof(_OnBoidAreaEntered));
            
            // clear target
            SetTarget(TargetType.None);
            
            // unregister steering boid
            SteeringManager.Instance.RemoveBoid(_steeringId);

            // convert to rigid body for 'ragdoll' death physics.
            Vector3 pos = GlobalTransform.origin;
            RigidBody rb = new RigidBody();
            GetParent().AddChild(rb);
            rb.GlobalTransform = GlobalTransform;
            GetParent().RemoveChild(this);  
            rb.AddChild(this);
            CollisionShape shape = GetNode<CollisionShape>("CollisionShape");
            rb.AddChild(shape.Duplicate());

            rb.GlobalTransform = new Transform(Basis.Identity, pos);
            GlobalTransform = new Transform(GlobalTransform.basis, pos);

            if (hitDir == Vector3.Zero) // random impulse
            {
                Vector3 randVec =
                    new Vector3(Utils.Rng.Randf() * 10.0f, Utils.Rng.Randf() * 10.0f, Utils.Rng.Randf() * 10.0f)
                        .Normalized();
                rb.ApplyImpulse(GlobalTransform.origin, randVec);
            }
            else // impulse from hit direction
            {
                rb.ApplyCentralImpulse(hitDir * hitStrength);
                rb.ApplyTorqueImpulse(new Vector3(Utils.Rng.Randf(), Utils.Rng.Randf(), Utils.Rng.Randf()) * 100.0f);
            }
            
            OnBoidDestroyed?.Invoke(this);
        }
    }

    protected void SetSteeringBehaviourEnabled(SteeringManager.Behaviours behaviour, bool enabled, float weight = 1.0f)
    {
        if (enabled)
        {
            _behaviours |= (1 << (int) behaviour);
        }
        else
        {
            _behaviours &= ~ (1 << (int) behaviour);
        }

        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        steeringBoid.Behaviours = _behaviours;
    }

    public void SetTarget(TargetType type, BoidBase boid = null, Vector2 pos = new Vector2(), Vector2 offset = new Vector2())
    {
        Debug.Assert(SteeringManager.Instance.HasBoid(_steeringId));

        if (_targetBoid != null)
        {
            _targetBoid.OnBoidDestroyed -= _OnTargetBoidDestroyed;
        }
        
        _targetType = type;
        _targetBoid = boid;
        _targetPos = pos;
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        switch (type)
        {
            case TargetType.Ally:
            case TargetType.Enemy:
                Debug.Assert(boid != null);
                steeringBoid.TargetIndex = boid.SteeringId;
                steeringBoid.TargetOffset = offset;
                _targetBoid.OnBoidDestroyed += _OnTargetBoidDestroyed;
                break;
            case TargetType.Position:
                steeringBoid.Position = pos;
                steeringBoid.TargetOffset = offset;
                steeringBoid.TargetIndex = -1;
                break;
            case TargetType.None:
                steeringBoid.TargetIndex = -1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public virtual void _OnBoidAreaEntered(Area area)
    {
        if (!IsInstanceValid(area))
            return;
        
        BoidBase boid = area as BoidBase;
        if (boid != null)
        {
            if (boid.Alignment == Alignment)
            {
                return;
            }
        }
        
        if (boid != null && !boid.Destroyed)
        {
            boid._OnHit(HitDamage, false, _cachedVelocity, GlobalTransform.origin);
            return;
        }
        
        if (area.IsInGroup("laser") && Alignment != BoidAlignment.Enemy)
        {
            _OnHit(_health, Alignment == BoidAlignment.Enemy, Vector2.Zero, Vector3.Zero);
            return;
        }

        if (area is Bullet bullet && bullet.Alignment != Alignment)
        {
            bullet.OnHit();
            _OnHit(bullet.Damage, Alignment == BoidAlignment.Enemy, bullet.Velocity, bullet.GlobalTransform.origin);
        }
    }
    
    private void _OnGameStateChanged(StateMachine_Game.States state, StateMachine_Game.States prevState)
    {
        switch (state)
        {
            case StateMachine_Game.States.Play:
                _acceptInput = true;
                break;
            case StateMachine_Game.States.TacticalPause:
            case StateMachine_Game.States.Construct:
                _acceptInput = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    protected virtual void _OnTargetBoidDestroyed(BoidBase boid)
    {
        if (_targetType is TargetType.Ally or TargetType.Enemy)
        {
            boid.OnBoidDestroyed -= _OnTargetBoidDestroyed;
            SetTarget(TargetType.None);
        }
    }
}