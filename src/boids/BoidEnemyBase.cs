using Godot;

public class BoidEnemyBase : BoidBase
{
    [Export] public float PickupDropRate = 0.25f;
    [Export] public int Points = 10;
    [Export] public float MaxHealth = 1.0f;
    [Export] public float HitFlashTime = 1.0f / 30.0f;
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float HitTrauma = 0.05f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float MaxAngularVelocity = 1000.0f;
    
    public float _health;
    public float _hitFlashTimer;
    public bool _move = true;
    public bool _destroyScore;

    public override void _Ready()
    {
        base._Ready();
        
        _health = MaxHealth;
    }
    
    public override void _Process(float delta)
    {
        base._Process(delta);
        
        Vector2 steering = new Vector2(0.0f, 0.0f);
        if (!_destroyed)
        {
            if (_move)
            {
                steering += _SteeringPursuit(_target.GlobalPosition, _target.Velocity);
                steering += _SteeringEdgeRepulsion(_game.PlayRadius) * 2.0f;

                // limit angular velocity
            }

            if (_velocity.LengthSquared() > 0)
            {
                Vector2 linearComp = _velocity.Normalized() * steering.Length() *
                                     steering.Normalized().Dot(_velocity.Normalized());
                Vector2 tangent = new Vector2(_velocity.y, -_velocity.x);
                Vector2 angularComp = tangent.Normalized() * steering.Length() *
                                      steering.Normalized().Dot(tangent.Normalized());
                steering = linearComp + angularComp.Normalized() *
                    Mathf.Clamp(angularComp.Length(), 0.0f, MaxAngularVelocity);
            }

            steering = ClampVec(steering, MinVelocity, MaxVelocity);

            if (MinVelocity > 0.0)
            {
                _velocity = ClampVec(_velocity + steering * delta, MinVelocity, MaxVelocity);
            }
            else
            {
                _velocity = Truncate(_velocity + steering * delta, MaxVelocity);
            }
        }

        GlobalPosition += _velocity * delta;

        _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(Damping, 0.0f, 1.0f), delta * 60.0f);

        // hit flash
        _hitFlashTimer -= delta;
        if (_hitFlashTimer < 0.0)
        {
            if (!_destroyed)
            {
                _sprite.Modulate = ColourManager.Instance.Secondary;
            }
        }

        if (_health < 0.0f && !_destroyed)
        {
            _Destroy(_destroyScore);
        }
    }

    public override void OnHit(float damage, bool score, Vector2 bulletVel, bool microbullet, Vector2 pos)
    {
        base.OnHit(damage, score, bulletVel, microbullet, pos);

        _health -= damage;
        _sprite.Modulate = ColourManager.Instance.White;
        _hitFlashTimer = HitFlashTime;
        CPUParticles2D hitParticles = HitParticles.Instance() as CPUParticles2D;
        hitParticles.Position = pos;
        hitParticles.Emitting = true;
        _game.AddChild(hitParticles);
        if (IsInstanceValid(_damagedParticles))
        {
            _damagedParticles.Emitting = true;
        }
        
        PauseManager.Instance.PauseFlash();
        GlobalCamera.Instance.AddTrauma(HitTrauma);

        _destroyScore = score;
    }
    
    protected override void _Destroy(bool score)
    {
        if (!_destroyed)
        {
            GlobalCamera.Instance.AddTrauma(DestroyTrauma);
            
            if (score && !_destroyed)
            {
                _game.AddScore(Points, GlobalPosition, true);
                if (GD.RandRange(0.0, 1.0) < PickupDropRate)
                {
                    _game.SpawnPickupAdd(GlobalPosition, false);
                }
            }
        }
        
        base._Destroy(score);
    }

    public override void _OnBoidAreaEntered(Area2D area)
    {
        Bullet bullet = area as Bullet;
        if (IsInstanceValid(bullet) && bullet.Alignment == 0 && !_destroyed)
        {
            OnHit(bullet.Damage, true, bullet.Velocity, bullet.Microbullet, bullet.GlobalPosition);
            area.QueueFree();
        }
    }
}