using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private struct HitMessage
    {
        public float Damage;
        public Vector2 Position;
        public Vector2 Velocity;
        public BoidAlignment Alignment;
    }

    #region Export

    [Export(PropertyHint.Flags, "DesiredVelocityOverride,Separation,AvoidObstacles,AvoidAllies,AvoidEnemies,Flee,MaintainSpeed,Cohesion,Alignment,Arrive,Pursuit,Wander,FlowFieldFollow")] protected int _behaviours;
    [Export] protected float _steeringRadius = 5.0f;
    
    [Export] public float MaxVelocity = 500.0f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float MaxForce = 150.0f;
    [Export] public float FieldOfView = 360.0f;
    [Export] public bool Bank360 = false;
    [Export] public float BankingRate = 2.5f;
    [Export] public float BankingAmount = 2.5f;
    
    [Export] public float DestroyTime = 3.0f;
    [Export] public float HitFlashTime = 1.0f / 30.0f;

    [Export] private ResourceStats _baseResourceStats;
    
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
    [OnReadyGet] protected CollisionShape _collisionShape;

    #endregion

    #region Signals

    public Action<BoidBase> OnBoidDestroyed;

    #endregion

    #region Public

    public virtual BoidAlignment Alignment => BoidAlignment.Ally;
    public bool Destroyed => _state == State.Destroyed;
    public int SteeringId => _steeringId;
    public Vector2 Heading => _cachedHeading;
    
    public Vector2 GlobalPosition
    {
        get => new(GlobalTransform.origin.x, GlobalTransform.origin.z);
        protected set
        {
            if (SteeringManager.Instance.HasObject<SteeringManager.Boid>(_steeringId))
            {
                ref SteeringManager.Boid boid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
                boid.Position = value.ToNumerics();
                GlobalTransform = new Transform(new Basis(Vector3.Down, boid.Heading.AngleToY()), value.To3D());
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

    protected ResourceStats _resourceStats;
    protected ResourceBoid _data;
    protected int _steeringId;
    protected TargetType _targetType = TargetType.None;
    protected BoidBase _targetBoid;
    protected Vector2 _targetPos;
    protected Vector3 _baseScale;
    protected bool _acceptInput = true;
    protected AudioStreamPlayer2D _sfxOnHit;
    protected Vector2 _cachedVelocity;
    protected Vector2 _cachedHeading;
    protected State _state;
    protected ShaderMaterial _meshMaterial;

    protected ref SteeringManager.Boid SteeringBoid => ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);

    #endregion

    #region Private

    private float _health;
    private float _hitFlashTimer;
    private List<HitMessage> _hitMessages = new();
    private List<Particles> _damagedParticles = new();
    private ShaderMaterial _altMaterial;
    private List<Particles> _hitParticles = new();
    private AudioStreamPlayer2D _sfxOnDestroy;
    private Vector2 _cachedLastHitDir;
    private float _cachedLastHitDamage;
    private bool _selected;
    private Vector2 _smoothSteering;
    private float[] _steeringWeights = new float[(int) SteeringManager.Behaviours.COUNT];
    private int _sharedPropertiesId;

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

    public virtual void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        _data = data;
        _state = State.Alive;
        OnBoidDestroyed += onDestroy;
        RegisterSteeringBoid(velocity);
        GlobalPosition = position;
    }

    [OnReady] private void Ready()
    {
        DebugUtils.Assert(_baseResourceStats != null, "_baseStats != null");
        _resourceStats = _baseResourceStats.Duplicate() as ResourceStats;
        //_OnSkillsChanged(SaveDataPlayer.GetActiveSkills(Id));
        
        _health = _resourceStats.MaxHealth;

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
        Debug.Assert(SteeringManager.Instance.HasObject<SteeringManager.Boid>(_steeringId));
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);

        _cachedVelocity = steeringBoid.Velocity.ToGodot();
        _cachedHeading = steeringBoid.Heading.ToGodot();
        
        Basis basis = new Basis(Vector3.Up, _cachedHeading.AngleToY());

        // banking
        if (delta > 0.0f)
        {
            System.Numerics.Vector2 right = new(-_cachedHeading.y, _cachedHeading.x);
            System.Numerics.Vector2 localSteering = Utils.LocaliseDirection(steeringBoid.Steering, _cachedHeading.ToNumerics(), right);
            localSteering /= delta;
            localSteering /= 100.0f;
            _smoothSteering = _smoothSteering.LinearInterpolate(localSteering.ToGodot(), Mathf.Clamp(delta * BankingRate, 0.0f, 1.0f));
            float bankX = Mathf.Clamp(_smoothSteering.Dot(Vector2.Down) * BankingAmount, -Mathf.Pi * 0.25f, Mathf.Pi * 0.25f);
            float bankZ = Mathf.Clamp(_smoothSteering.Dot(Vector2.Right) * BankingAmount, -Mathf.Pi * 0.25f, Mathf.Pi * 0.25f);
            basis = basis.Rotated(basis.z, bankZ);
            if (Bank360)
                basis = basis.Rotated(basis.x, bankX);
        }
        
        // update position and cache velocity from steering boid
        GlobalTransform = new Transform(basis, steeringBoid.PositionG.To3D());

        // process hits
        foreach (HitMessage hit in _hitMessages)
        {
            if (hit.Alignment != Alignment)
            {
                _OnHit(hit.Damage, hit.Velocity, hit.Position);
            }
        }
        _hitMessages.Clear();

        // hit flash
        if (_hitFlashTimer > 0.0 && _hitFlashTimer - delta < 0.0f)
        {
            SetMeshColour();
        }
        _hitFlashTimer -= delta;

        if (_health <= 0.0f)
        {
            _OnDestroy(_cachedLastHitDir, _cachedLastHitDamage);
            return;
        }
        _cachedLastHitDir = Vector2.Zero;
        _cachedLastHitDamage = 0.0f;
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

    private void RegisterSteeringBoid(Vector2 velocity)
    {
        SteeringManager.Boid boid = new()
        {   
            Alignment = (byte)(this is BoidEnemyBase ? 0 : 1),
            Radius = _steeringRadius,
            Position = GlobalPosition.ToNumerics(),
            Velocity = velocity.ToNumerics(),
            Heading = System.Numerics.Vector2.UnitY,
            Target = System.Numerics.Vector2.Zero,
            TargetIndex = -1,
            Behaviours = _behaviours,
            MaxSpeed = MaxVelocity * _resourceStats.MoveSpeed,
            MinSpeed = MinVelocity,
            MaxForce = MaxForce * _resourceStats.MoveSpeed,
            DesiredSpeed = 0.0f,
            LookAhead = 1.0f,
            ViewRange = 50.0f,
            ViewAngle = 240.0f,
            WanderCircleDist = 25.0f,
            WanderCircleRadius = 5.0f,
            WanderVariance = 50.0f,
        };
        
        _steeringId = SteeringManager.Instance.Register(boid);
    }

    public void SendHitMessage(float damage, Vector2 vel, Vector2 pos, BoidAlignment alignment)
    {
        _hitMessages.Add(new HitMessage()
        {
            Damage = damage, Alignment = alignment, Position = pos, Velocity = vel
        });
    }

    protected virtual void _OnHit(float damage, Vector2 bulletVel, Vector2 pos)
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
            Vector3 fromCentre = pos.To3D() - GlobalTransform.origin;
            mat.Direction = -bulletVel.Reflect(fromCentre.Normalized().To2D()).To3D();
            hitParticles.GlobalPosition(pos.To3D() + Vector3.Up * 5.0f);
            hitParticles.Emitting = true;
            _hitParticles.Add(hitParticles);
        }

        // does this cause us to go past a new damage threshold?
        if (_damagedParticles.Count < _damageVfxCount)
        {
            float damageVfxThresholds = 1.0f / (_damageVfxCount + 1.0f);
            float nextThreshold = 1.0f - (_damagedParticles.Count + 1.0f) * damageVfxThresholds;
            if (_health / _resourceStats.MaxHealth < nextThreshold)
            {
                Particles particles = _damagedParticlesScene.Instance<Particles>();
                _damagedParticles.Add(particles);
                AddChild(particles);
                particles.GlobalPosition(pos.To3D() + Vector3.Up * 5.0f);
            }
        }

        _cachedLastHitDir = bulletVel.Normalized();
        _cachedLastHitDamage = damage;
    }
    
    protected virtual void _OnDestroy(Vector2 hitDir, float hitStrength)
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
            SteeringManager.Instance.Unregister<SteeringManager.Boid>(_steeringId);

            // convert to rigid body for 'ragdoll' death physics.
            Vector3 pos = GlobalTransform.origin;
            RigidBody rb = new RigidBody();
            GetParent().AddChild(rb);
            rb.GlobalTransform = GlobalTransform;
            GetParent().RemoveChild(this);  
            rb.AddChild(this);
            rb.AddChild(_collisionShape.Duplicate());

            rb.GlobalTransform = new Transform(Basis.Identity, pos);
            GlobalTransform = new Transform(GlobalTransform.basis, pos);

            if (hitDir.To3D() == Vector3.Zero) // random impulse
            {
                Vector3 randVec =
                    new Vector3(Utils.Rng.Randf() * 10.0f, Utils.Rng.Randf() * 10.0f, Utils.Rng.Randf() * 10.0f)
                        .Normalized();
                rb.ApplyImpulse(GlobalTransform.origin, randVec);
            }
            else // impulse from hit direction
            {
                rb.ApplyCentralImpulse(hitDir.To3D() * hitStrength);
                rb.ApplyTorqueImpulse(new Vector3(Utils.Rng.Randf(), Utils.Rng.Randf(), Utils.Rng.Randf()) * 100.0f);
            }

            Monitorable = false;
            Monitoring = false;
            
            OnBoidDestroyed?.Invoke(this);
        }
    }

    protected void SetSteeringBehaviourEnabled(SteeringManager.Behaviours behaviour, bool enabled, float weight = -1.0f)
    {
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        if (enabled)
        {
            steeringBoid.Behaviours |= (1 << (int) behaviour);
        }
        else
        {
            steeringBoid.Behaviours &= ~ (1 << (int) behaviour);
        }
    }

    protected virtual void SetTarget(TargetType type, BoidBase boid = null, Vector2 pos = new Vector2(), Vector2 offset = new Vector2())
    {
        Debug.Assert(SteeringManager.Instance.HasObject<SteeringManager.Boid>(_steeringId));

        if (_targetBoid != null)
        {
            _targetBoid.OnBoidDestroyed -= _OnTargetBoidDestroyed;
        }
        
        _targetType = type;
        _targetBoid = boid;
        _targetPos = pos;
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        switch (type)
        {
            case TargetType.Ally:
            case TargetType.Enemy:
                Debug.Assert(boid != null);
                steeringBoid.TargetIndex = boid.SteeringId;
                steeringBoid.TargetOffset = offset.ToNumerics();
                _targetBoid.OnBoidDestroyed += _OnTargetBoidDestroyed;
                break;
            case TargetType.Position:
                steeringBoid.Target = pos.ToNumerics();
                steeringBoid.TargetOffset = offset.ToNumerics();
                steeringBoid.TargetIndex = -1;
                break;
            case TargetType.None:
                steeringBoid.TargetIndex = -1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    protected void ResetSteeringBehaviours()
    {
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        steeringBoid.Behaviours = _behaviours;
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
            boid.SendHitMessage(_resourceStats.CollisionDamage, _cachedVelocity, GlobalTransform.origin.To2D(), Alignment);
            return;
        }
        
        if (area.IsInGroup("laser") && Alignment != BoidAlignment.Enemy)
        {
            SendHitMessage(_health,Vector2.Zero, GlobalPosition, BoidAlignment.Enemy);
            return;
        }
    }
    
    protected virtual void _OnGameStateChanged(StateMachine_Game.States state, StateMachine_Game.States prevState)
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

    protected virtual void _OnSkillsChanged(List<ResourceSkillNode> skillNodes)
    {
        _resourceStats = _baseResourceStats.Duplicate() as ResourceStats;
        Debug.Assert(_resourceStats != null, "_stats != null");
        foreach (ResourceSkillNode skill in skillNodes)
        {
            _resourceStats.AttackDamage *= skill.AttackDamage;
            _resourceStats.AttackCooldown *= skill.AttackCooldown;
            _resourceStats.AttackSpread *= skill.AttackSpread;
            _resourceStats.AttackVelocity *= skill.AttackVelocity;
            _resourceStats.MoveSpeed *= skill.MoveSpeed;
            _resourceStats.MaxHealth *= skill.MaxHealth;
            _resourceStats.Regeneration += skill.Regeneration;
            _resourceStats.MicroTurrets |= skill.MicroTurrets;
            _resourceStats.MicroTurretRange *= skill.MicroTurretRange;
            _resourceStats.MicroTurretBallistics |= skill.MicroTurretBallistics;
            _resourceStats.MicroTurretDamage *= skill.MicroTurretDamage;
            _resourceStats.MicroTurretVelocity *= skill.MicroTurretVelocity;
        }
    }
}