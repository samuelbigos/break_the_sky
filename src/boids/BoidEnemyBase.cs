using Godot;

public class BoidEnemyBase : BoidBase
{
    [Export] public float PickupDropRate = 0.25f;
    [Export] public int Points = 10;
    [Export] public float MaxHealth = 1.0f;
    [Export] public float HitFlashTime = 1.0f / 30.0f;
    [Export] public float DestroyTime = 3.0f;
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float HitTrauma = 0.05f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float MaxAngularVelocity = 1000.0f;
    
    public AudioStreamPlayer2D _sfxDestroy;
    public Particles2D _damagedParticles;
    public Trail _trail;
    public AudioStream _hitSfx2;
    public AudioStream _hitSfx3;

    public AudioStreamPlayer2D _sfxHit;
    public AudioStreamPlayer2D _sfxHitMicro;
    public float _health;
    public float _hitFlashTimer;
    public Vector2 _baseScale;
    public float _destroyRot;
    public bool _move = true;
    public bool _destroyScore;

    public override void _Ready()
    {
        base._Ready();

        _sfxDestroy = GetNode("SFXDestroy") as AudioStreamPlayer2D;
        _sprite.Modulate = ColourManager.Instance.Secondary;
        
        _damagedParticles = GetNode("Damaged") as Particles2D;
        _trail = GetNode("Trail") as Trail;
        _hitSfx2 = GD.Load("res://assets/sfx/hit2.wav") as AudioStream;
        _hitSfx3 = GD.Load("res://assets/sfx/hit3.wav") as AudioStream;

        Connect("area_entered", this, nameof(_OnBoidBaseAreaEntered));
        _health = MaxHealth;
        _baseScale = _sprite.Scale;
        _sfxHit = new AudioStreamPlayer2D();
        _sfxHitMicro = new AudioStreamPlayer2D();
        AddChild(_sfxHit);
        AddChild(_sfxHitMicro);
        _sfxHit.Stream = _hitSfx2;
        _sfxHitMicro.Stream = _hitSfx3;
        _sfxHitMicro.VolumeDb = -5;
        if (!IsInstanceValid(_trail))
        {
            _trail = null;
        }

        if (_trail != null)
        {
            _trail.Init(this);
        }
    }
    
    public override void _Process(float delta)
    {
        base._Process(delta);
        
        Vector2 steering = new Vector2(0.0f, 0.0f);
        if (!_destroyed)
        {
            if (_move)
            {
                steering += _SteeringPursuit(_target.GlobalPosition, (_target as BoidBase)._velocity);
                steering += _SteeringEdgeRepulsion(_game.PlayRadius) * 2.0f;

                // limit angular velocity
            }

            if (_velocity.LengthSquared() > 0)
            {
                var linearComp = _velocity.Normalized() * steering.Length() *
                                 steering.Normalized().Dot(_velocity.Normalized());
                Vector2 tangent = new Vector2(_velocity.y, -_velocity.x);
                var angularComp = tangent.Normalized() * steering.Length() *
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
        Update();

        if (IsInstanceValid(_trail) && TrailLength > 0)
        {
            _trail.Update();

            // damping
        }

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

            _destroyRot += Mathf.Pi * 2.0f * delta;
            if (_destroyedTimer < 0.0f)
            {
                QueueFree();
            }
        }

        if (_health < 0.0f && !_destroyed)
        {
            Destroy(_destroyScore);
        }
    }

    protected virtual void Destroy(bool score)
    {
        if (!_destroyed)
        {
            if (score && !_destroyed)
            {
                _game.AddScore(Points, GlobalPosition, true);
                if (GD.RandRange(0.0, 1.0) < PickupDropRate)
                {
                    _game.SpawnPickupAdd(GlobalPosition, false);
                }
            }
        }

        _destroyed = true;
        _sprite.Modulate = ColourManager.Instance.White;
        _destroyedTimer = DestroyTime;
        _sprite.ZIndex = -1;
        if (IsInstanceValid(_damagedParticles))
        {
            _damagedParticles.Emitting = false;
        }
        
        _sfxDestroy.Play();

        GlobalCamera.Instance.AddTrauma(DestroyTrauma);
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

        if (!microbullet)
        {
            PauseManager.Instance.PauseFlash();
            GlobalCamera.Instance.AddTrauma(HitTrauma);
            _sfxHit.Play();
        }
        else
        {
            _sfxHitMicro.Play();
        }

        _destroyScore = score;
    }

    public void _OnBoidBaseAreaEntered(Area2D area)
    {
        Bullet bullet = area as Bullet;
        if (IsInstanceValid(bullet) && bullet.GetAlignment() == 0 && !_destroyed)
        {
            OnHit(bullet.Damage, true, bullet._velocity, bullet._microbullet, bullet.GlobalPosition);
            area.QueueFree();
        }
    }
}