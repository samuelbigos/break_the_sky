using System.Collections.Generic;
using Godot;

public class BoidBase3D : Area
{
    [Export] public float Damping = 0.05f;
    [Export] public int TrailLength = 5;
    [Export] public float TrailPeriod = 0.05f;
    [Export] public float DestroyTime = 3.0f;
    [Export] public PackedScene HitParticles;
    [Export] private List<AudioStream> _hitSfx;

    [Export] private NodePath _trailPath;
    [Export] private NodePath _damagedParticlesPath;
    [Export] private NodePath _meshPath;
    [Export] private NodePath _sfxDestroyPath;
    [Export] private NodePath _sfxHitPlayerPath;
    [Export] private NodePath _sfxShootPlayerPath;

    protected float _maxVelocity;
    protected Vector3 _velocity;
    private List<Vector3> _trailPoints = new List<Vector3>();
    private float _trailTimer;
    protected Player3D _player;
    protected Game3D _game;
    protected BoidBase3D _target;
    protected Vector2 _targetOffset;
    protected bool _destroyed = false;
    protected float _destroyedTimer;
    protected Vector3 _baseScale;

    private Trail3D _trail;
    protected Particles _damagedParticles;
    protected MeshInstance _mesh;
    private AudioStreamPlayer2D _sfxDestroyPlayer;
    protected AudioStreamPlayer2D _sfxHitPlayer;
    protected AudioStreamPlayer2D _sfxShootPlayer;
    
    public Vector3 Velocity => _velocity;
    public List<Vector3> TrailPoints => _trailPoints;

    public Vector2 GlobalPosition
    {
        get { return new Vector2(GlobalTransform.origin.x, GlobalTransform.origin.z); }
        set { GlobalTransform = new Transform(GlobalTransform.basis, value.To3D()); }
    }

    public virtual void Init(Player3D player, Game3D game, BoidBase3D target)
    {
        _player = player;
        _game = game;
        _target = target;
    }

    public override void _Ready()
    {
        base._Ready();

        _mesh = GetNode<MeshInstance>(_meshPath); 
        _trail = GetNode<Trail3D>(_trailPath);
        _damagedParticles = GetNode<Particles>(_damagedParticlesPath);
        _sfxDestroyPlayer = GetNode<AudioStreamPlayer2D>(_sfxDestroyPath);
        _sfxHitPlayer = GetNode<AudioStreamPlayer2D>(_sfxHitPlayerPath);
        _sfxShootPlayer = GetNode<AudioStreamPlayer2D>(_sfxShootPlayerPath);
        
        _trail.Init(this);
        _sfxHitPlayer.Stream = _hitSfx[0];
        _baseScale = _mesh.Scale;

        Connect("area_entered", this, nameof(_OnBoidAreaEntered));
    }

    public override void _Process(float delta)
    {
        _maxVelocity = _game.BaseBoidSpeed;
        
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
        
        Rotation = new Vector3(0.0f, -Mathf.Atan2(_velocity.x, _velocity.z), 0.0f);
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
    }

    public bool IsDestroyed()
    {
        return _destroyed;
    }

    public virtual void OnHit(float damage, bool score, Vector2 bulletVel, bool microbullet, Vector2 pos)
    {
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
    
    protected virtual void _Shoot(Vector2 dir)
    {
        _sfxShootPlayer.Play();
    }
    
    public void SetOffset(Vector2 targetOffset)
    {  
        _targetOffset = targetOffset;
		
    }

    public virtual Vector2 _SteeringPursuit(Vector2 targetPos, Vector2 targetVel)
    {
        Vector2 desiredVelocity = (targetPos - GlobalPosition).Normalized() * _maxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    public virtual Vector2 _SteeringEdgeRepulsion(float radius)
    {
        float edgeThreshold = 50.0f;
        float edgeDist = Mathf.Clamp(GlobalPosition.Length() - (radius - edgeThreshold), 0.0f, edgeThreshold) /
                         edgeThreshold;
        Vector2 desiredVelocity = edgeDist * -GlobalPosition.Normalized() * _maxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    public virtual Vector2 _SteeringFollow(Vector2 target, float delta)
    {
        Vector2 desiredVelocity = (target - GlobalPosition).Normalized() * _maxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    public virtual Vector2 _SteeringArrive(Vector2 target, float slowingRadius)
    {
        Vector2 desiredVelocity = (target - GlobalPosition).Normalized() * _maxVelocity;
        float distance = (target - GlobalPosition).Length();
        if (distance < slowingRadius)
        {
            desiredVelocity = desiredVelocity.Normalized() * _maxVelocity * (distance / slowingRadius);
        }

        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    public virtual Vector2 _SteeringAlignment(List<BoidBase3D> boids, float alignmentRadius)
    {
        int nCount = 0;
        Vector2 desiredVelocity = new Vector2(0.0f, 0.0f);

        foreach (BoidBase3D boid in boids)
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

        desiredVelocity = desiredVelocity.Normalized() * _maxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    public virtual Vector2 _SteeringCohesion(List<BoidBase3D> boids, float cohesionRadius)
    {
        int nCount = 0;
        Vector2 desiredVelocity = new Vector2(0.0f, 0.0f);

        foreach (BoidBase3D boid in boids)
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
        desiredVelocity = desiredVelocity.Normalized() * _maxVelocity;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    public virtual Vector2 _SteeringSeparation(List<BoidBase3D> boids, float separationRadius)
    {
        int nCount = 0;
        Vector2 desiredVelocity = new Vector2(0.0f, 0.0f);

        foreach (BoidBase3D boid in boids)
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

        desiredVelocity = desiredVelocity.Normalized() * _maxVelocity * -1;
        Vector2 steering = desiredVelocity - _velocity.To2D();
        return steering;
    }

    public virtual void _OnBoidAreaEntered(Area area)
    {
    }
}