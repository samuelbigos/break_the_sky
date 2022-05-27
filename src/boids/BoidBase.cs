using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;

public partial class BoidBase : Area
{
    public enum BoidAlignment
    {
        Ally,
        Enemy
    };

    #region Export

    [Export(PropertyHint.Flags, "Flock,Align,Separate,Arrive,Pursuit,Flee")] public int Behaviours;
    [Export] public float[] BehaviourWeights;

    [Export] public bool DebugBoid = true;
    [Export] public PackedScene DebugBoidScene;
    [Export] public float MaxVelocity = 500.0f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float TargetVectoringExponent = 1.0f;
    [Export] public float Damping = 0.05f;
    [Export] public float DestroyTime = 3.0f;
    [Export] public float SlowingRadius = 100.0f;
    [Export] public float AlignmentRadius = 20.0f;
    [Export] public float SeparationRadius = 10.0f;
    [Export] public float CohesionRadius = 50.0f;
    [Export] public float MaxAngularVelocity = 500.0f;
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
    
    [OnReadyGet] protected MultiViewportMeshInstance _mesh;
    [OnReadyGet] private BoidTrail _trail;

    #endregion

    #region Signals

    public Action<BoidBase> OnBoidDestroyed;

    #endregion

    #region Public

    public virtual BoidAlignment Alignment => BoidAlignment.Ally;
    public bool Destroyed => _destroyed;    
    public string ID = "";
    public Vector2 Velocity;
    public Vector2 Steering;
    public Vector2 TargetPos;
    
    public Vector2 GlobalPosition
    {
        get { return new Vector2(GlobalTransform.origin.x, GlobalTransform.origin.z); }
        set { GlobalTransform = new Transform(GlobalTransform.basis, value.To3D()); }
    }

    #endregion

    #region Protected

    protected BoidPlayer _player;
    protected Game _game;
    protected BoidBase _target;
    protected bool _destroyed;
    protected Vector3 _baseScale;
    protected DebugBoid _debugBoid;
    protected bool _acceptInput = true;
    protected AudioStreamPlayer2D _sfxOnHit;
    protected virtual Color BaseColour => ColourManager.Instance.Secondary;

    #endregion

    #region Private

    private float _health;
    private float _hitFlashTimer;
    private List<Particles> _damagedParticles = new List<Particles>();
    private ShaderMaterial _meshMaterial;
    private ShaderMaterial _altMaterial;
    private List<Particles> _hitParticles = new List<Particles>();
    private AudioStreamPlayer2D _sfxOnDestroy;
    private Vector3 _cachedLastHitDir;
    private float _cachedLastHitDamage;
    
    private Color MeshColour
    {
        set
        {
            _meshMaterial.SetShaderParam("u_primary_colour", value);
            _meshMaterial.SetShaderParam("u_secondary_colour", value);
        }
    }

    #endregion

    public void Init(string id, BoidPlayer player, Game game, BoidBase target, Action<BoidBase> onDestroy)
    {
        _player = player;
        _game = game;
        _target = target;
        ID = id;
        OnBoidDestroyed += onDestroy;
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
        Debug.Assert(_meshMaterial != null, $"_meshMaterial is null on {Name}");
        _mesh.SetSurfaceMaterial(0, _meshMaterial);
        _altMaterial = altMeshes[0].GetActiveMaterial(0) as ShaderMaterial;
        MeshColour = BaseColour;
        
        Connect("area_entered", this, nameof(_OnBoidAreaEntered));
        
        if (Game.Instance != null)
            Game.Instance.OnGameStateChanged += _OnGameStateChanged;
        
#if TOOLS
        if (DebugBoid)
        {
            _debugBoid = DebugBoidScene.Instance<DebugBoid>();
            AddChild(_debugBoid);
            _debugBoid.Owner = this;
            _mesh.Visible = false;
            _trail.Visible = false;
        }
#endif
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (_target != null)
            TargetPos = _target.GlobalTransform.origin.To2D();
        FlockingManager.Instance.UpdateBoid(this);
        
        if (!_destroyed)
        {
            // DoSteering(delta);
            // GlobalTranslate(_velocity * delta);
            // _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(Damping, 0.0f, 1.0f), delta * 60.0f); // damping
            // Rotation = new Vector3(0.0f, -Mathf.Atan2(_velocity.x, -_velocity.z), 0.0f);
            
            // hit flash
            _hitFlashTimer -= delta;
            if (_hitFlashTimer < 0.0)
            {
                if (!_destroyed)
                {
                    MeshColour = BaseColour;
                }
            }

            if (_health < 0.0f && !_destroyed)
            {
                _Destroy(Points > 0, _cachedLastHitDir, _cachedLastHitDamage);
            }
            _cachedLastHitDir = Vector3.Zero;
            _cachedLastHitDamage = 0.0f;
        }
        else
        {
            if (GlobalTransform.origin.y < -100.0f)
            {
                foreach (Particles p in _hitParticles)
                {
                    p.QueueFree();
                }
                _game.FreeBoid(this);
            }
        }

        _altMaterial.SetShaderParam("u_velocity", Velocity);
    }

    protected virtual void _OnHit(float damage, bool score, Vector2 bulletVel, Vector3 pos)
    {
        _health -= damage;

        MeshColour = ColourManager.Instance.White;
        _hitFlashTimer = HitFlashTime;
        
        _sfxOnHit.Play();

        Particles hitParticles = _hitParticlesScene.Instance<Particles>();
        _game.AddChild(hitParticles);
        ParticlesMaterial mat = hitParticles.ProcessMaterial as ParticlesMaterial;
        Debug.Assert(mat != null);
        Vector3 fromCentre = pos - GlobalTransform.origin;
        mat.Direction = -bulletVel.Reflect(fromCentre.Normalized().To2D()).To3D();
        hitParticles.GlobalPosition(pos + Vector3.Up * 5.0f);
        hitParticles.Emitting = true;
        _hitParticles.Add(hitParticles);

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
        if (!_destroyed)
        {
            MeshColour = BaseColour;
            
            _sfxOnDestroy.Play();
            _destroyed = true;
            Disconnect("area_entered", this, nameof(_OnBoidAreaEntered));

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

    protected void SetSteeringBehaviourEnabled(FlockingManager.Behaviours behaviour, bool enabled)
    {
        if (enabled)
        {
            Behaviours |= (1 << (int) behaviour);
        }
        else
        {
            Behaviours &= ~ (1 << (int) behaviour);
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
            boid._OnHit(HitDamage, false, Velocity, GlobalTransform.origin);
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
    
    private void _OnGameStateChanged(Game.State state, Game.State prevState)
    {
        switch (state)
        {
            case Game.State.Play:
                _acceptInput = true;
                break;
            case Game.State.Pause:
            case Game.State.Construct:
                _acceptInput = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}