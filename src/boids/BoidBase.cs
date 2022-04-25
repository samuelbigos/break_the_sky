using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

public class BoidBase : Area2D
{
    [Export] public float MaxVelocity = 1000.0f;
    [Export] public float Damping = 0.05f;
    [Export] public int TrailLength = 5;
    [Export] public float TrailPeriod = 0.05f;
    [Export] public PackedScene HitParticles;

    public Vector2 _velocity;
    public List<Vector2> _trailPoints = new List<Vector2>();
    private float _trailTimer = 0.0f;

    protected Leader _player;
    protected Game _game;
    protected BoidBase _target;
    protected Vector2 _targetOffset;
    protected bool _destroyed = false;
    protected float _destroyedTimer;
    protected Sprite _sprite;
    protected Trail _trail;

    public virtual void Init(Leader player, Game game, BoidBase target)
    {
        _player = player;
        _game = game;
        _target = target;
    }

    public override void _Ready()
    {
        base._Ready();
        
        _sprite = GetNode("Sprite") as Sprite;
        _trail = GetNode<Trail>("Trail");
    }

    public bool IsDestroyed()
    {
        return _destroyed;
    }

    public virtual void OnHit(float damage, bool score, Vector2 bulletVel, bool microbullet, Vector2 pos)
    {
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
        var desiredVelocity = (targetPos - GlobalPosition).Normalized() * MaxVelocity;
        var steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringEdgeRepulsion(float radius)
    {
        float edgeThreshold = 50.0f;
        var edgeDist = Mathf.Clamp(GlobalPosition.Length() - (radius - edgeThreshold), 0.0f, edgeThreshold) /
                       edgeThreshold;
        var desiredVelocity = edgeDist * -GlobalPosition.Normalized() * MaxVelocity;
        var steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringFollow(Vector2 target, float delta)
    {
        var desiredVelocity = (target - GlobalPosition).Normalized() * MaxVelocity;
        var steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringArrive(Vector2 target, float slowingRadius)
    {
        var desiredVelocity = (target - GlobalPosition).Normalized() * MaxVelocity;
        var distance = (target - GlobalPosition).Length();
        if (distance < slowingRadius)
        {
            desiredVelocity = desiredVelocity.Normalized() * MaxVelocity * (distance / slowingRadius);
        }

        var steering = desiredVelocity - _velocity;
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

            var distance = (boid.GlobalPosition - GlobalPosition).Length();
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
        var steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringCohesion(List<BoidBase> boids, float cohesionRadius)
    {
        int nCount = 0;
        Vector2 desiredVelocity = new Vector2(0.0f, 0.0f);

        foreach (var boid in boids)
        {
            if (boid == this)
            {
                continue;
            }

            var distance = (boid.GlobalPosition - GlobalPosition).Length();
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
        var steering = desiredVelocity - _velocity;
        return steering;
    }

    public virtual Vector2 _SteeringSeparation(List<BoidBase> boids, float separationRadius)
    {
        int nCount = 0;
        Vector2 desiredVelocity = new Vector2(0.0f, 0.0f);

        foreach (var boid in boids)
        {
            if (boid == this)
            {
                continue;
            }

            var distance = (boid.GlobalPosition - GlobalPosition).Length();
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
        var steering = desiredVelocity - _velocity;
        return steering;
    }

    public Vector2 Truncate(Vector2 vector, float v_max)
    {
        var length = vector.Length();
        if (length == 0.0f)
        {
            return vector;
        }

        var i = v_max / vector.Length();
        i = Mathf.Min(i, 1.0f);
        return vector * i;
    }

    public Vector2 ClampVec(Vector2 vector, float v_min, float v_max)
    {
        var length = vector.Length();
        if (length == 0.0f)
        {
            return vector;
        }

        var i = vector.Length();
        i = Mathf.Clamp(i, v_min, v_max);
        return vector.Normalized() * i;
    }
}