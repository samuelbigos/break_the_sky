using System.Collections.Generic;
using Godot;

public class BoidBase : Area2D
{
    [Export] public float MaxVelocity = 1000.0f;
    [Export] public float Damping = 0.05f;
    [Export] public int TrailLength = 5;
    [Export] public float TrailPeriod = 0.05f;
    [Export] public float DestroyTime = 3.0f;
    [Export] public PackedScene HitParticles;
    [Export] private List<AudioStream> _hitSfx;

    [Export] private NodePath _trailNode;
    [Export] private NodePath _damagedParticlesNode;
    [Export] private NodePath _spriteNode;
    [Export] private NodePath _sfxDestroyNode;
    [Export] private NodePath _sfxHitPlayerNode;
    [Export] private NodePath _sfxShootPlayerNode;

    protected Vector2 _velocity;
    private List<Vector2> _trailPoints = new List<Vector2>();
    private float _trailTimer;
    protected Leader _player;
    protected Game _game;
    protected BoidBase _target;
    protected Vector2 _targetOffset;
    protected bool _destroyed = false;
    protected float _destroyedTimer;
    protected Vector2 _baseScale;

    private Trail _trail;
    protected Particles2D _damagedParticles;
    protected Sprite _sprite;
    private AudioStreamPlayer2D _sfxDestroyPlayer;
    protected AudioStreamPlayer2D _sfxHitPlayer;
    protected AudioStreamPlayer2D _sfxShootPlayer;
    
    public Vector2 Velocity => _velocity;
    public List<Vector2> TrailPoints => _trailPoints;

    public virtual void Init(Leader player, Game game, BoidBase target)
    {
        _player = player;
        _game = game;
        _target = target;
    }

    public override void _Ready()
    {
        base._Ready();

        _sprite = GetNode<Sprite>(_spriteNode); 
        _trail = GetNode<Trail>(_trailNode);
        _damagedParticles = GetNode<Particles2D>(_damagedParticlesNode);
        _sfxDestroyPlayer = GetNode<AudioStreamPlayer2D>(_sfxDestroyNode);
        _sfxHitPlayer = GetNode<AudioStreamPlayer2D>(_sfxHitPlayerNode);
        _sfxShootPlayer = GetNode<AudioStreamPlayer2D>(_sfxShootPlayerNode);
        
        _trail.Init(this);
        _sfxHitPlayer.Stream = _hitSfx[0];
        _baseScale = _sprite.Scale;

        Connect("area_entered", this, nameof(_OnBoidAreaEntered));
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
            _sprite.Modulate = ColourManager.Instance.White;
            _destroyedTimer = DestroyTime;
            _sprite.ZIndex = -1;
            _damagedParticles.Emitting = true;
        }
    }

    public override void _Process(float delta)
    {
        if (TrailLength > 0)
        {
            _trailTimer -= delta;
            if (_trailTimer < 0.0)
            {
                _trailTimer = TrailPeriod;
                _trailPoints.Add(GlobalPosition);
                if (_trailPoints.Count > TrailLength)
                {
                    _trailPoints.RemoveAt(0);
                }
            }
        }
        
        Rotation = -Mathf.Atan2(_velocity.x, _velocity.y);
        _trail.Rotation = -Rotation;
        
        if (_destroyed)
        {
            _destroyedTimer -= delta;
            float t = 1.0f - Mathf.Clamp(_destroyedTimer / DestroyTime, 0.0f, 1.0f);
            _sprite.Scale = _baseScale.LinearInterpolate(new Vector2(0.0f, 0.0f), t);
            if (_trail != null)
            {
                Color mod = _trail.Modulate;
                mod.a = Mathf.Lerp(1.0f, 0.0f, t);
                _trail.Modulate = mod;
            }

            if (_destroyedTimer < 0.0f)
            {
                QueueFree();
            }
        }
        
        Update();
    }

    protected virtual void _Shoot(Vector2 dir)
    {
        _sfxShootPlayer.Play();
    }
    
    public void SetOffset(Vector2 targetOffset)
    {  
        _targetOffset = targetOffset;
		
    }

    protected Vector2 GetVelocity()
    {
        return _velocity;
    }

    public virtual Vector2 _SteeringPursuit(Vector2 targetPos, Vector2 targetVel)
    {
        Vector2 desiredVelocity = (targetPos - GlobalPosition).Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringEdgeRepulsion(float radius)
    {
        float edgeThreshold = 50.0f;
        float edgeDist = Mathf.Clamp(GlobalPosition.Length() - (radius - edgeThreshold), 0.0f, edgeThreshold) /
                         edgeThreshold;
        Vector2 desiredVelocity = edgeDist * -GlobalPosition.Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringFollow(Vector2 target, float delta)
    {
        Vector2 desiredVelocity = (target - GlobalPosition).Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringArrive(Vector2 target, float slowingRadius)
    {
        Vector2 desiredVelocity = (target - GlobalPosition).Normalized() * MaxVelocity;
        float distance = (target - GlobalPosition).Length();
        if (distance < slowingRadius)
        {
            desiredVelocity = desiredVelocity.Normalized() * MaxVelocity * (distance / slowingRadius);
        }

        Vector2 steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringAlignment(List<BoidBase> boids, float alignmentRadius)
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
                desiredVelocity += boid.GetVelocity();
                nCount += 1;
            }
        }

        if (nCount == 0)
        {
            return desiredVelocity;
        }

        desiredVelocity = desiredVelocity.Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringCohesion(List<BoidBase> boids, float cohesionRadius)
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
        Vector2 steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringSeparation(List<BoidBase> boids, float separationRadius)
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
        Vector2 steering = desiredVelocity - _velocity;
        return steering;
    }

    public Vector2 Truncate(Vector2 vector, float v_max)
    {
        float length = vector.Length();
        if (length == 0.0f)
        {
            return vector;
        }

        float i = v_max / vector.Length();
        i = Mathf.Min(i, 1.0f);
        return vector * i;
    }

    public Vector2 ClampVec(Vector2 vector, float v_min, float v_max)
    {
        float length = vector.Length();
        if (length == 0.0f)
        {
            return vector;
        }

        float i = vector.Length();
        i = Mathf.Clamp(i, v_min, v_max);
        return vector.Normalized() * i;
    }

    public virtual void _OnBoidAreaEntered(Area2D area)
    {
    }
}