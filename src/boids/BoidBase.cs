using System.Collections.Generic;
using Godot;

public class BoidBase : Area
{
    public enum BoidAlignment
    {
        Ally,
        Enemy,
        Neutral
    }
    
    protected enum SteeringBehaviours
    {
        Arrive = 1,
        Separation = 2,
        EdgeRepulsion = 4,
        Pursuit = 8
    }
    
    [Export(PropertyHint.Flags, "Arrive,Separation,EdgeRepulsion,Pursuit")] protected int _behaviours;

    [Export] public float MaxVelocity = 500.0f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float Damping = 0.05f;
    [Export] public int TrailLength = 5;
    [Export] public float TrailPeriod = 0.05f;
    [Export] public float DestroyTime = 3.0f;
    [Export] public PackedScene HitParticles;
    [Export] public float SlowingRadius = 100.0f;
    [Export] public float AlignmentRadius = 20.0f;
    [Export] public float SeparationRadius = 10.0f;
    [Export] public float CohesionRadius = 50.0f;
    [Export] public float MaxAngularVelocity = 500.0f;
    [Export] public float HitDamage = 3.0f;
    [Export] public float MaxHealth = 1.0f;
    [Export] public float HitFlashTime = 1.0f / 30.0f;
    [Export] public int Points = 10;
    
    [Export] private List<AudioStream> _hitSfx;

    [Export] private NodePath _trailPath;
    [Export] private NodePath _damagedParticlesPath;
    [Export] private NodePath _meshPath;
    [Export] private NodePath _sfxDestroyPath;
    [Export] private NodePath _sfxHitPlayerPath;

    protected virtual BoidAlignment Alignment => BoidAlignment.Ally;
    public bool Destroyed => _destroyed;

    protected Vector3 _velocity;
    private List<Vector3> _trailPoints = new List<Vector3>();
    private float _trailTimer;
    protected Player _player;
    protected Game _game;
    protected BoidBase _target;
    protected Vector2 _targetOffset;
    protected bool _destroyed = false;
    private float _destroyedTimer;
    protected Vector3 _baseScale;
    private float _health;
    private float _hitFlashTimer;

    private Trail _trail;
    private Particles _damagedParticles;
    protected MultiViewportMeshInstance _mesh;
    private AudioStreamPlayer3D _sfxDestroyPlayer;
    protected AudioStreamPlayer3D _sfxHitPlayer;
    
    public List<Vector3> TrailPoints => _trailPoints;
    public float Health => Health;

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

    public virtual void Init(Player player, Game game, BoidBase target)
    {
        _player = player;
        _game = game;
        _target = target;
    }

    public override void _Ready()
    {
        base._Ready();
        
        _health = MaxHealth;

        _mesh = GetNode<MultiViewportMeshInstance>(_meshPath); 
        _trail = GetNode<Trail>(_trailPath);
        _damagedParticles = GetNode<Particles>(_damagedParticlesPath);
        _sfxDestroyPlayer = GetNode<AudioStreamPlayer3D>(_sfxDestroyPath);
        _sfxHitPlayer = GetNode<AudioStreamPlayer3D>(_sfxHitPlayerPath);

        _trail.Init(this);
        _sfxHitPlayer.Stream = _hitSfx[0];
        _baseScale = _mesh.Scale;

        ShaderMaterial mat = _mesh.GetActiveMaterial(0) as ShaderMaterial;
        mat?.SetShaderParam("u_primary_colour", ColourManager.Instance.Primary);
        mat?.SetShaderParam("u_secondary_colour", ColourManager.Instance.Secondary);
        
        Connect("area_entered", this, nameof(_OnBoidAreaEntered));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        // steering
        if (!_destroyed)
        {
            DoSteering(delta);
        }

        GlobalTranslate(_velocity * delta);

        // damping
        _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(Damping, 0.0f, 1.0f), delta * 60.0f);
        
        if (TrailLength > 0)
        {
            _trailTimer -= delta;
            if (_trailTimer < 0.0)
            {
                _trailTimer = TrailPeriod;
                _trailPoints.Add(GlobalPosition.To3D());
                if (_trailPoints.Count > TrailLength)
                {
                    _trailPoints.RemoveAt(0);
                }
            }
        }
        
        Rotation = new Vector3(0.0f, -Mathf.Atan2(_velocity.x, -_velocity.z), 0.0f);
        //_trail.Rotation = -Rotation;
        
        if (_destroyed)
        {
            _destroyedTimer -= delta;
            float t = 1.0f - Mathf.Clamp(_destroyedTimer / DestroyTime, 0.0f, 1.0f);
            _mesh.Scale = _baseScale.LinearInterpolate(new Vector3(0.0f, 0.0f, 0.0f), t);

            if (_destroyedTimer < 0.0f)
            {
                QueueFree();
            }
        }

        List<MeshInstance> altMeshes = _mesh.AltMeshes;
        if (altMeshes.Count > 0)
        {
            ShaderMaterial mat = altMeshes[0].GetActiveMaterial(0) as ShaderMaterial;
            mat?.SetShaderParam("u_velocity", _velocity);
        }
        
        // hit flash
        _hitFlashTimer -= delta;
        if (_hitFlashTimer < 0.0)
        {
            if (!_destroyed)
            {
                //_sprite.Modulate = ColourManager.Instance.Secondary;
            }
        }

        if (_health < 0.0f && !_destroyed)
        {
            _Destroy(Points > 0);
        }
    }

    protected virtual void DoSteering(float delta)
    {
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
            steering += _SteeringSeparation(_game.AllBoids, _game.BaseBoidGrouping * 0.66f);
        }
        if ((_behaviours & (int) SteeringBehaviours.EdgeRepulsion) != 0)
        {
            steering += _SteeringEdgeRepulsion(_game.PlayRadius) * 2.0f;
        }

        // limit angular velocity
        if (_velocity.LengthSquared() > 0)
        {
            Vector2 linearComp = _velocity.To2D().Normalized() * steering.Length() * steering.Normalized().Dot(_velocity.To2D().Normalized());
            Vector2 tangent = new Vector2(_velocity.z, -_velocity.x);
            Vector2 angularComp = tangent.Normalized() * steering.Length() * steering.Normalized().Dot(tangent.Normalized());
            steering = linearComp + angularComp.Normalized() * Mathf.Clamp(angularComp.Length(), 0.0f, MaxAngularVelocity);
        }

        steering = steering.Truncate(MaxVelocity);
        _velocity += steering.To3D() * delta;
        _velocity = _velocity.Truncate(MaxVelocity);
    }

    protected virtual void _OnHit(float damage, bool score, Vector2 bulletVel, bool microbullet, Vector2 pos)
    {
        _health -= damage;
        _hitFlashTimer = HitFlashTime;
        CPUParticles2D hitParticles = HitParticles.Instance() as CPUParticles2D;
        hitParticles.Position = pos;
        hitParticles.Emitting = true;
        _game.AddChild(hitParticles);
        if (IsInstanceValid(_damagedParticles))
        {
            _damagedParticles.Emitting = true;
        }

        _sfxHitPlayer.Play();
    }
    
    protected virtual void _Destroy(bool score)
    {
        if (!_destroyed)
        {
            _sfxDestroyPlayer.Play();
            _game.RemoveBoid(this);
            _destroyed = true;
            _destroyedTimer = DestroyTime;
            _damagedParticles.Emitting = true;
        }
    }

    public void SetOffset(Vector2 targetOffset)
    {  
        _targetOffset = targetOffset;
    }
    
    protected virtual Vector2 _SteeringPursuit(Vector2 targetPos, Vector2 targetVel)
    {
        Vector2 desiredVelocity = (targetPos - GlobalPosition).Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    protected virtual Vector2 _SteeringEdgeRepulsion(float radius)
    {
        float edgeThreshold = 50.0f;
        float edgeDist = Mathf.Clamp(GlobalPosition.Length() - (radius - edgeThreshold), 0.0f, edgeThreshold) /
                         edgeThreshold;
        Vector2 desiredVelocity = edgeDist * -GlobalPosition.Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

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

            float distance = (boid.GlobalPosition - GlobalPosition).Length();
            if (distance < separationRadius)
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
        BoidBase boid = area as BoidBase;
        if (boid is BoidAllyBase || boid is Player)
        {
            if (boid.Alignment == Alignment)
            {
                return;
            }
        }
        
        if (boid != null && !boid.Destroyed)
        {
            boid._OnHit(HitDamage, false, _velocity.To2D(), false, GlobalPosition);
            _Destroy(false);
            return;
        }
        
        if (area.IsInGroup("laser") && Alignment != BoidAlignment.Enemy)
        {
            _Destroy(false);
            return;
        }

        if (area is Bullet bullet && bullet.Alignment != Alignment)
        {
            bullet.OnHit();
            _OnHit(bullet.Damage, Alignment == BoidAlignment.Enemy, bullet.Velocity, bullet.Microbullet, bullet.GlobalPosition);
            return;
        }
    }
}