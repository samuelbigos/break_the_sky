using Godot;

public class BoidAlly : BoidBase
{
    [Export] public float SlowingRadius = 100.0f;
    [Export] public float AlignmentRadius = 20.0f;
    [Export] public float SeparationRadius = 10.0f;
    [Export] public float CohesionRadius = 50.0f;
    [Export] public float HitDamage = 3.0f;
    [Export] public float DestroyTime = 3.0f;
    [Export] public float ShootSize = 1.5f;
    [Export] public float ShootTrauma = 0.05f;
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float MicroBulletCD = 1.0f;
    [Export] public float MicroBulletRange = 400.0f;
    [Export] public float MicroBulletDamageMod = 0.25f;
    [Export] public float MaxAngularVelocity = 500.0f;

    [Export] public PackedScene BulletScene;
    [Export] public PackedScene MicoBulletScene;

    private AudioStreamPlayer2D _sfxShot;
    private AudioStreamPlayer2D _sfxHit;
    private AudioStreamPlayer2D _sfxHitMicro;
    private Particles2D _damagedParticles;
    

    private float _shootCooldown;
    private Vector2 _baseScale;
    private float _destroyRot;
    private float _microBulletTargetSearchTimer;
    private BoidBase _microBulletTarget;
    private float _microBulletCd;

    public override void _Ready()
    {
        base._Ready();

        _sfxShot = GetNode("SFXShot") as AudioStreamPlayer2D;
        _sfxHit = GetNode("SFXHit") as AudioStreamPlayer2D;
        _sfxHitMicro = GetNode("SFXHitMicro") as AudioStreamPlayer2D;
        _damagedParticles = GetNode("Damaged") as Particles2D;
        

        _trail.Init(this);
        _sprite.Modulate = ColourManager.Instance.Secondary;
        _baseScale = _sprite.Scale;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        MaxVelocity = _game.BaseBoidSpeed;

        Vector2 targetPos = _target.GlobalPosition + (_target.Transform.BasisXform(_targetOffset) / _target.Scale);
        Vector2 shootDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();

        // steering
        Vector2 steering = new Vector2(0.0f, 0.0f);
        if (!_destroyed)
        {
            steering = _SteeringArrive(targetPos, SlowingRadius);
            steering += _SteeringSeparation(_game.GetBoids(), _game.BaseBoidGrouping * 0.66f);
            steering += _SteeringEdgeRepulsion(_game.PlayRadius) * 2.0f;

            // limit angular velocity
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

            steering = Truncate(steering, MaxVelocity);
            _velocity = Truncate(_velocity + steering * delta, MaxVelocity);
        }

        GlobalPosition += _velocity * delta;
        _sprite.Rotation = -Mathf.Atan2(_velocity.x, _velocity.y);

        // damping
        _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(Damping, 0.0f, 1.0f), delta * 60.0f);

        _shootCooldown -= delta;
        if (Input.IsActionPressed("shoot") && _shootCooldown <= 0.0)
        {
            if (_CanShoot(shootDir))
            {
                _Shoot(shootDir);
                //	if _shootCooldown < 0.0:
                //		_sprite.modulate = Colours.Secondary;

                // shooting
            }
        }

        if (_shootCooldown > 0.0f)
        {
            float t = _shootCooldown / _game.BaseBoidReload;
            t = Mathf.Pow(Mathf.Clamp(t, 0.0f, 1.0f), 5.0f);
            Vector2 from = _baseScale * 2.0f;
            _sprite.Scale = from.LinearInterpolate(_baseScale, 1.0f - t);

            // microbullets
        }

        if (_game.BaseMicroturrets && !_destroyed)
        {
            if (!IsInstanceValid(_microBulletTarget) || _microBulletTarget.IsDestroyed())
            {
                _microBulletTarget = null;
            }

            _microBulletTargetSearchTimer -= delta;
            if (_microBulletTarget == null && _microBulletTargetSearchTimer < 0.0)
            {
                _microBulletTargetSearchTimer = 0.1f;
                foreach (BoidBase enemy in _game.Enemies)
                {
                    if ((enemy.GlobalPosition - GlobalPosition).Length() < MicroBulletRange)
                    {
                        _microBulletTarget = enemy;
                        _microBulletCd = (float) GD.RandRange(MicroBulletCD * 0.5f, MicroBulletCD * 1.5f);
                    }
                }
            }

            if (IsInstanceValid(_microBulletTarget))
            {
                if ((_microBulletTarget.GlobalPosition - GlobalPosition).Length() > MicroBulletRange)
                {
                    _microBulletTarget = null;
                    _microBulletTargetSearchTimer = 0.1f;
                }
                else
                {
                    _microBulletCd -= delta;
                    if (_microBulletCd < 0.0)
                    {
                        _microBulletCd = (float) GD.RandRange(MicroBulletCD * 0.5f, MicroBulletCD * 1.5f);
                        MicroBullet mb = MicoBulletScene.Instance() as MicroBullet;
                        float spread = _game.BaseBoidSpread;
                        Vector2 dir = (_microBulletTarget.GlobalPosition - GlobalPosition).Normalized();
                        dir += new Vector2(-dir.y, dir.x) * (float) GD.RandRange(-spread, spread);
                        mb.Init(dir * _game.BaseBulletSpeed, 0, _game.PlayRadius);
                        mb.GlobalPosition = GlobalPosition;
                        mb.Damage = _game.BaseBoidDamage * MicroBulletDamageMod;
                        _game.AddChild(mb);
                        _sfxHitMicro.Play();

                        // update trail
                    }
                }
            }
        }

        (GetNode("Trail") as Trail).Update();

        if (_destroyed)
        {
            _destroyedTimer -= delta;
            float t = 1.0f - Mathf.Clamp(_destroyedTimer / DestroyTime, 0.0f, 1.0f);
            _sprite.Scale = _baseScale.Slerp(new Vector2(0.0f, 0.0f), t);
            _destroyRot += Mathf.Pi * 2.0f * delta;
            if (_destroyedTimer < 0.0)
            {
                QueueFree();
            }
        }
    }

    public void _Shoot(Vector2 dir)
    {
        _shootCooldown = _game.BaseBoidReload;
        Bullet bullet = BulletScene.Instance() as Bullet;
        var spread = _game.BaseBoidSpread;
        dir += new Vector2(-dir.y, dir.x) * (float) GD.RandRange(-spread, spread);
        bullet.Init(dir * _game.BaseBulletSpeed, 0, _game.PlayRadius, _game.BaseBoidDamage);
        bullet.GlobalPosition = GlobalPosition;
        _game.AddChild(bullet);
        _game.PushBack(this);
        float traumaMod = 1.0f - Mathf.Clamp(_game.GetNumBoids() / 100.0f, 0.0f, 0.5f);
        GlobalCamera.Instance.AddTrauma(ShootTrauma * traumaMod);
        _sfxShot.Play();
        //_sprite.modulate = Colours.Grey;
    }

    public bool _CanShoot(Vector2 dir)
    {
        if (_destroyed)
            return false;
        // can shoot if there are no other boids in the shoot direction
        bool blocked = false;
        foreach (var boid in _game.GetBoids())
        {
            if (boid == this || boid.IsDestroyed())
            {
                continue;
            }

            if ((boid.GlobalPosition - GlobalPosition).Normalized().Dot(dir.Normalized()) > 0.9f)
            {
                blocked = true;
                break;
            }
        }

        return !blocked;
    }

    public void _Destroy()
    {
        if (!_destroyed)
        {
            _game.RemoveBoid(this);
            _destroyed = true;
            _sprite.Modulate = ColourManager.Instance.White;
            _destroyedTimer = DestroyTime;
            _sprite.ZIndex = -1;
            GlobalCamera.Instance.AddTrauma(DestroyTrauma);
            _sfxHit.Play();
            _damagedParticles.Emitting = true;
        }
    }

    public void _OnBoidAllyAreaEntered(Area2D area)
    {
        BoidBase boid = area as BoidBase;
        if (boid != null && !boid.IsDestroyed())
        {
            boid.OnHit(HitDamage, false, _velocity, false, GlobalPosition);
            _Destroy();
            return;
        }

        BoidBase laser = area as BoidEnemyLaser;
        if (laser != null)
        {
            _Destroy();
            return;
        }

        Bullet bullet = area as Bullet;
        if (bullet != null && bullet.Alignment == 1)
        {
            //bullet.OnHit(HitDamage, false, _velocity, false, GlobalPosition);
            _Destroy();
            return;
        }
    }
}