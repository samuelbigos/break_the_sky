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
    }
    
    protected enum SteeringBehaviours
    {
        Arrive = 1,
        Separation = 2,
        Pursuit = 8,
        Flee = 16
    }
    
    [Export(PropertyHint.Flags, "Arrive,Separation,EdgeRepulsion,Pursuit,Flee")] protected int _behaviours;

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
    
    public Action<BoidBase> OnBoidDestroyed;
    
    protected virtual BoidAlignment Alignment => BoidAlignment.Ally;
    public bool Destroyed => _destroyed;    
    public string ID = "";

    protected Vector3 _velocity;
    protected BoidPlayer _player;
    protected Game _game;
    protected BoidBase _target;
    private Vector2 _targetOffset;
    protected bool _destroyed;
    protected Vector3 _baseScale;
    private float _health;
    private float _hitFlashTimer;
    private List<Particles> _damagedParticles = new List<Particles>();
    private ShaderMaterial _meshMaterial;
    private ShaderMaterial _altMaterial;
    private List<Particles> _hitParticles = new List<Particles>();

    [OnReadyGet] protected MultiViewportMeshInstance _mesh;
    private AudioStreamPlayer2D _sfxOnDestroy;
    protected AudioStreamPlayer2D _sfxOnHit;

    private Vector3 _cachedLastHitDir;
    private float _cachedLastHitDamage;
    protected bool _acceptInput = true;

    public Vector3 Velocity
    {
        get { return _velocity; }
        set { _velocity = value; }
    }
    
    public Vector2 GlobalPosition
    {
        get { return new Vector2(GlobalTransform.origin.x, GlobalTransform.origin.z); }
        set { GlobalTransform = new Transform(GlobalTransform.basis, value.To3D()); }
    }
    
    public Vector2 Offset { set => _targetOffset = value; }

    private Color MeshColour
    {
        set
        {
            _meshMaterial.SetShaderParam("u_primary_colour", value);
            _meshMaterial.SetShaderParam("u_secondary_colour", value);
        }
    }

    protected virtual Color BaseColour => ColourManager.Instance.Secondary;

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
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        if (!_destroyed)
        {
            DoSteering(delta);
            GlobalTranslate(_velocity * delta);
            _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(Damping, 0.0f, 1.0f), delta * 60.0f); // damping
            Rotation = new Vector3(0.0f, -Mathf.Atan2(_velocity.x, -_velocity.z), 0.0f);
            
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

        _altMaterial.SetShaderParam("u_velocity", _velocity);
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

    protected void SetSteeringBehaviourEnabled(SteeringBehaviours behaviour, bool enabled)
    {
        if (enabled)
        {
            _behaviours |= (int) behaviour;
        }
        else
        {
            _behaviours &= ~(int) behaviour;
        }
    }

    protected virtual void DoSteering(float delta)
    {
        if (_target == null)
            return;
        
        Vector3 targetPos = _target.GlobalPosition.To3D() + _target.Transform.basis.Xform(_targetOffset.To3D());
        Vector2 steering = Vector2.Zero;

        if ((_behaviours & (int) SteeringBehaviours.Arrive) != 0)
        {
            steering += _SteeringArrive(targetPos.To2D(), SlowingRadius);
        }
        if ((_behaviours & (int)SteeringBehaviours.Pursuit) != 0)
        {
            steering += _SteeringPursuit(targetPos.To2D(), _target.Velocity.To2D());
        }
        if ((_behaviours & (int) SteeringBehaviours.Separation) != 0)
        {
            steering += _SteeringSeparation(_game.AllBoids, SeparationRadius);
        }
        if ((_behaviours & (int)SteeringBehaviours.Flee) != 0)
        {
            steering += _SteeringFlee(targetPos.To2D(), _target.Velocity.To2D());
        }

        // limit angular velocity
        if (_velocity != Vector3.Zero)
        {
            Vector2 vel = _velocity.To2D();
            float dot = steering.Normalized().Dot(vel.Normalized());
            
            // this causes the angular velocity to be more limited the less the target is ahead of this boid
            if (dot > 0.0f) dot = Mathf.Pow(dot, TargetVectoringExponent);
            
            Vector2 linearComp = vel.Normalized() * steering.Length() * dot;
            Vector2 tangent = new Vector2(vel.y, -vel.x);
            Vector2 angularComp = tangent.Normalized() * steering.Length() * steering.Normalized().Dot(tangent.Normalized());
            steering = linearComp + angularComp.Normalized() * Mathf.Clamp(angularComp.Length(), 0.0f, MaxAngularVelocity);
        }

        steering = steering.Truncate(MaxVelocity);
        _velocity += steering.To3D() * delta;
        
        float velLength = _velocity.Length();
        Vector3 velDir = _velocity.Normalized();
        velLength = Mathf.Clamp(velLength, MinVelocity, MaxVelocity);
        _velocity = velDir * velLength;
    }
    
    protected virtual Vector2 _SteeringPursuit(Vector2 targetPos, Vector2 targetVel)
    {
        Vector2 desiredVelocity = (targetPos - GlobalPosition).Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        float dot = steering.Normalized().Dot(_velocity.To2D().Normalized());
        if (dot < -0.9f)
        {
            float len = steering.Length();
            steering = new Vector2(steering.y, -steering.x).Normalized() * len;
        }
        return steering;
    }
    
    protected virtual Vector2 _SteeringFlee(Vector2 targetPos, Vector2 targetVel)
    {
        Vector2 desiredVelocity = (GlobalPosition - targetPos).Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        float dot = steering.Normalized().Dot(_velocity.To2D().Normalized());
        if (dot < -0.9f) // we're basically heading towards the target and need corrective action
        {
            float len = steering.Length();
            steering = new Vector2(steering.y, -steering.x).Normalized() * len;
        }
        return steering;
    }

    // ReSharper disable once UnusedMember.Global
    protected virtual Vector2 _SteeringEdgeRepulsion(float radius)
    {
        float edgeThreshold = 50.0f;
        float edgeDist = Mathf.Clamp(GlobalPosition.Length() - (radius - edgeThreshold), 0.0f, edgeThreshold) /
                         edgeThreshold;
        Vector2 desiredVelocity = edgeDist * -GlobalPosition.Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    // ReSharper disable once UnusedMember.Global
    protected virtual Vector2 _SteeringFollow(Vector2 target, float delta)
    {
        Vector2 desiredVelocity = (target - GlobalPosition).Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    protected virtual Vector2 _SteeringArrive(Vector2 target, float slowingRadius)
    {
        Vector2 desiredVelocity = (target - GlobalPosition).Normalized() * MaxVelocity;
        float distance = (target - GlobalPosition).Length();
        if (distance < slowingRadius)
        {
            desiredVelocity = desiredVelocity.Normalized() * MaxVelocity * (distance / slowingRadius);
        }

        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    // ReSharper disable once UnusedMember.Global
    protected virtual Vector2 _SteeringAlignment(List<BoidBase> boids, float alignmentRadius)
    {
        int nCount = 0;
        Vector2 desiredVelocity = new Vector2(0.0f, 0.0f);

        foreach (BoidBase boid in boids)
        {
            if (boid == this)
            {
                continue;
            }

            float distance = (boid.GlobalPosition - GlobalPosition).Length();
            if (distance < alignmentRadius)
            {
                desiredVelocity += boid.Velocity.To2D();
                nCount += 1;
            }
        }

        if (nCount == 0)
        {
            return desiredVelocity;
        }

        desiredVelocity = desiredVelocity.Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    // ReSharper disable once UnusedMember.Global
    protected virtual Vector2 _SteeringCohesion(List<BoidBase> boids, float cohesionRadius)
    {
        int nCount = 0;
        Vector2 desiredVelocity = new Vector2(0.0f, 0.0f);

        foreach (BoidBase boid in boids)
        {
            if (boid == this)
            {
                continue;
            }

            float distance = (boid.GlobalPosition - GlobalPosition).Length();
            if (distance < cohesionRadius)
            {
                desiredVelocity += boid.GlobalPosition;
                nCount += 1;
            }
        }

        if (nCount == 0)
        {
            return desiredVelocity;
        }

        desiredVelocity /= nCount;
        desiredVelocity = desiredVelocity - GlobalPosition;
        desiredVelocity = desiredVelocity.Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    protected virtual Vector2 _SteeringSeparation(List<BoidBase> boids, float separationRadius)
    {
        int nCount = 0;
        Vector2 desiredVelocity = new Vector2(0.0f, 0.0f);

        foreach (BoidBase boid in boids)
        {
            if (boid == this)
            {
                continue;
            }

            float distance = (boid.GlobalPosition - GlobalPosition).LengthSquared();
            if (distance < Mathf.Pow(separationRadius + boid.SeparationRadius, 2.0f))
            {
                desiredVelocity += boid.GlobalPosition - GlobalPosition;
                nCount += 1;
            }
        }

        if (nCount == 0)
        {
            return desiredVelocity;
        }

        desiredVelocity = desiredVelocity.Normalized() * MaxVelocity * -1;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
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
            boid._OnHit(HitDamage, false, _velocity.To2D(), GlobalTransform.origin);
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