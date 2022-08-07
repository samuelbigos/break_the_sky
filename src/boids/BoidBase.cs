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

    [Export] public float _mass = 1.0f;
    [Export] public float MaxVelocity = 500.0f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float MaxForce = 150.0f;
    [Export] public float FieldOfView = 360.0f;
    [Export] public bool Bank360 = false;
    [Export] public float BankingRate = 2.5f;
    [Export] public float BankingAmount = 2.5f;
    
    [Export] private float _hitVfxDuration = 1.0f;
    [Export] private float _flashVfxDuration = 1.0f / 30.0f;
    
    [Export] private ResourceStats _baseResourceStats;
    
    [Export] private int _damageVfxCount = 2;
    
    [Export] private List<AudioStream> _hitSfx;

    [Export] protected PackedScene _pickupMaterialScene;
    
    [OnReadyGet] protected MeshInstance _selectedIndicator;
    [OnReadyGet] protected MultiViewportMeshInstance _mesh;
    [OnReadyGet] protected CollisionShape _shipCollider;
    [OnReadyGet] protected CollisionShape _rbCollider;
    [OnReadyGet] private AudioStreamPlayer2D _sfxOnDestroy;
    [OnReadyGet] protected AudioStreamPlayer2D _sfxOnHit;
    
    #endregion

    #region Signals

    public Action<BoidBase> OnBoidDestroyed;

    #endregion

    #region Public

    public virtual BoidAlignment Alignment => BoidAlignment.Ally;
    public bool Destroyed => _state == State.Destroyed;
    public int SteeringId => _steeringId;
    public Vector2 Heading => _cachedHeading;
    public Vector2 Velocity => _cachedVelocity;
    
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

    public TargetType CurrentTargetType => _targetType;
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
    protected bool _acceptInput = true;
    protected Vector2 _cachedVelocity;
    protected Vector2 _cachedHeading;
    protected Vector2 _visualHeadingOverride;
    protected State _state;
    protected ShaderMaterial _meshMaterial;

    protected ref SteeringManager.Boid SteeringBoid => ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);

    #endregion

    #region Private

    private float _health;
    private float _hitFlashTimer;
    private List<HitMessage> _hitMessages = new();
    private List<Particles> _damagedParticles = new();
    private List<Particles> _hitParticles = new();
    private Vector2 _cachedLastHitDir;
    private float _cachedLastHitDamage;
    private Vector2 _cachedLastHitPos;
    private bool _selected;
    private Vector2 _smoothSteering;
    private int _sharedPropertiesId;
    private RigidBody _destroyedRb;
    private bool _beginHitGround;
    private bool _hasHitGround;
    private bool _hitGround;
    private float _hitGroundTimer;

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

        _sfxOnHit.Stream = _hitSfx[0];
        
        List<MeshInstance> altMeshes = _mesh.AltMeshes;
        Debug.Assert(altMeshes.Count > 0);
        _meshMaterial = _mesh.MaterialOverride.Duplicate() as ShaderMaterial;
        _mesh.MaterialOverride = _meshMaterial;

        Connect("area_entered", this, nameof(_OnBoidAreaEntered));

        if (!Game.Instance.Null())
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
        
        // Hit VFX.
        if (_hitFlashTimer > 0.0f)
        {
            _meshMaterial.SetShaderParam("u_hit_time", _hitVfxDuration - _hitFlashTimer);
            _hitFlashTimer -= delta;
            if (_hitFlashTimer <= 0.0f)
            {
                _meshMaterial.SetShaderParam("u_hits", 0);
            }
        }
    }

    protected virtual void ProcessAlive(float delta)
    {
        Debug.Assert(SteeringManager.Instance.HasObject<SteeringManager.Boid>(_steeringId));
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);

        _cachedVelocity = steeringBoid.Velocity.ToGodot();
        _cachedHeading = steeringBoid.Heading.ToGodot();

        if (_visualHeadingOverride != Vector2.Zero)
        {
            _cachedHeading = _visualHeadingOverride;
        }
        Basis basis = new Basis(Vector3.Up, _cachedHeading.AngleToY());

        // Banking.
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
        
        // Update position and cache velocity from steering boid.
        GlobalTransform = new Transform(basis, steeringBoid.PositionG.To3D());

        // Process hits.
        foreach (HitMessage hit in _hitMessages)
        {
            if (hit.Alignment != Alignment)
            {
                _OnHit(hit.Damage, hit.Velocity, hit.Position);
            }
        }
        _hitMessages.Clear();

        if (_health <= 0.0f)
        {
            _OnDestroy(_cachedLastHitDir, _cachedLastHitDamage);
            return;
        }
        _cachedLastHitDir = Vector2.Zero;
        _cachedLastHitDamage = 0.0f;
    }

    private void ProcessDestroyed(float delta)
    {
        if (_beginHitGround)
        {
            foreach (Particles particles in _damagedParticles)
            {
                particles.QueueFree();
            }
            _damagedParticles.Clear();
            _damageVfxCount = 0;
            _beginHitGround = false;
            _hitGroundTimer = 0.0f;
        }

        if (_hasHitGround)
        {
            _hitGroundTimer += delta;
            float rbSleepVel = 15.0f;
            if ((_destroyedRb.LinearVelocity.Length() <= rbSleepVel && _hitGroundTimer > 1.0f) || _hitGroundTimer > 8.0f)
            {
                _destroyedRb.Sleeping = true;
                _hitGround = false;
            
                HuskRenderer.Instance.AddHusk(_mesh.Mesh, _mesh.GlobalTransform);
                BoidFactory.Instance.FreeBoid(this);
            }
            
            // Occurs every time physics reports a collision between ground and ship.
            if (_hitGround)
            {
                _hitGround = false;
            
                Particles p = ParticleManager.Instance.AddOneShotParticles(Resources.Instance.DustCloudVFX, GlobalTransform.origin);
                ParticlesMaterial pMat = p.ProcessMaterial.Duplicate() as ParticlesMaterial;
                float vel = _destroyedRb.LinearVelocity.Length();
                float t = 1.0f - Utils.Ease_CubicOut(Mathf.Clamp(_hitGroundTimer / 5.0f, 0.0f, 1.0f));
                t *= _mass;
                pMat.InitialVelocity *= t;
                pMat.Scale *= t;
                pMat.Direction = _destroyedRb.LinearVelocity.To2D().Normalized().To3D();
                p.ProcessMaterial = pMat;
            }
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
        
        if (!SteeringManager.Instance.Register(boid, out _steeringId))
        {
            QueueFree();
            return;
        }
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

        _cachedLastHitPos = pos;
        _hitFlashTimer = _hitVfxDuration;
        _meshMaterial.SetShaderParam("u_hit_duration", _hitVfxDuration);
        _meshMaterial.SetShaderParam("u_flash_duration", _flashVfxDuration);
        _meshMaterial.SetShaderParam("u_hits", 1);
        _meshMaterial.SetShaderParam("u_centre", GlobalPosition.To3D());
        _meshMaterial.SetShaderParam("u_hit_pos", _cachedLastHitPos.To3D());
        
        _sfxOnHit.Play();

        if (bulletVel != Vector2.Zero)
        {
            Particles hitParticles = Resources.Instance.HitVFX.Instance<Particles>();
            Game.Instance.AddChild(hitParticles);
            ParticlesMaterial mat = hitParticles.ProcessMaterial as ParticlesMaterial;
            DebugUtils.Assert(mat != null, "mat != null");
            Vector3 fromCentre = pos.To3D() - GlobalTransform.origin;
            mat.Direction = -bulletVel.Reflect(fromCentre.Normalized().To2D()).To3D();
            hitParticles.GlobalPosition(pos.To3D() + Vector3.Up * 5.0f);
            hitParticles.Emitting = true;
            _hitParticles.Add(hitParticles);
        }

        // Does this cause us to go past a new damage threshold?
        if (_damagedParticles.Count < _damageVfxCount)
        {
            float damageVfxThresholds = 1.0f / (_damageVfxCount + 1.0f);
            float nextThreshold = 1.0f - (_damagedParticles.Count + 1.0f) * damageVfxThresholds;
            if (_health / _resourceStats.MaxHealth < nextThreshold)
            {
                Particles particles = Resources.Instance.DamagedVFX.Instance<Particles>();
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
            
            // Remove mesh from the outline buffer so it doesn't get outlines.
            _mesh.ClearAltMeshes();
            
            _sfxOnDestroy.Play();
            Disconnect("area_entered", this, nameof(_OnBoidAreaEntered));
            
            // Clear target.
            SetTarget(TargetType.None);

            // Convert to rigid body for 'ragdoll' death physics.
            Vector3 pos = GlobalTransform.origin;
            _destroyedRb = new RigidBody();
            GetParent().AddChild(_destroyedRb);
            _destroyedRb.GlobalTransform = GlobalTransform;
            GetParent().RemoveChild(this);  
            _destroyedRb.AddChild(this);
            _destroyedRb.AddChild(_rbCollider.Duplicate());
            _rbCollider.QueueFree();
            _shipCollider.QueueFree();

            SteeringManager.Boid boid = SteeringBoid;

            _destroyedRb.GlobalTransform = new Transform(Basis.Identity, pos);
            GlobalTransform = new Transform(GlobalTransform.basis, pos);

            _destroyedRb.LinearDamp = 0.1f;
            _destroyedRb.AngularDamp = 0.2f;

            // Apply impulse in direction of travel.
            _destroyedRb.ApplyCentralImpulse(boid.VelocityG.To3D());
            _destroyedRb.ApplyTorqueImpulse(new Vector3(Utils.Rng.Randf(), Utils.Rng.Randf(), Utils.Rng.Randf()) * 50.0f * (1.0f / _mass));
            
            _destroyedRb.Connect("body_entered", this, nameof(_WhenTheBodyHitsTheFloor));
            _destroyedRb.ContactMonitor = true;
            _destroyedRb.ContactsReported = 2;

            Monitorable = false;
            Monitoring = false;
            
            // Unregister steering boid.
            SteeringManager.Instance.Unregister<SteeringManager.Boid>(_steeringId);
            
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

        if (!_targetBoid.Null())
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
                DebugUtils.Assert(boid != null, "boid != null");
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

    protected virtual void _OnBoidAreaEntered(Area area)
    {
        if (!IsInstanceValid(area))
            return;
        
        BoidBase boid = area as BoidBase;
        if (!boid.Null())
        {
            if (boid.Alignment == Alignment)
            {
                return;
            }
        }
        
        if (!boid.Null() && !boid.Destroyed)
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

    protected void _WhenTheBodyHitsTheFloor(Node body)
    {
        if (body.IsInGroup("ground"))
        {
            if (!_hasHitGround)
            {
                _hasHitGround = true;
                _beginHitGround = true;
            }

            _hitGround = true;
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
        }
    }

    protected virtual void _OnSkillsChanged(List<ResourceSkillNode> skillNodes)
    {
        _resourceStats = _baseResourceStats.Duplicate() as ResourceStats;
        DebugUtils.Assert(_resourceStats != null, "_stats != null");
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
            _resourceStats.MicroTurretDamage *= skill.MicroTurretDamage;
            _resourceStats.MicroTurretVelocity *= skill.MicroTurretVelocity;
        }
    }
}