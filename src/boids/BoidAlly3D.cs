using Godot;

public class BoidAlly3D : BoidBase3D
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

    [Export] private NodePath _sfxHitMicroPlayerNode;

    [Export] public PackedScene BulletScene;
    [Export] public PackedScene MicoBulletScene;

    private AudioStreamPlayer2D _sfxHitMicroPlayer;

    private float _shootCooldown; 
    private float _microBulletTargetSearchTimer;
    private BoidBase3D _microBulletTarget;
    private float _microBulletCd;

    public override void _Ready()
    {
        base._Ready();
        
        _baseScale = _mesh.Scale;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        Vector3 targetPos = _target.GlobalPosition.To3D() + _target.Transform.basis.Xform(_targetOffset.To3D());
        //targetPos += Vector3.Forward * 10.0f;
        
        Vector2 shootDir = (GlobalCamera3D.Instance.MousePosition() - GlobalPosition).Normalized();
        
        // steering
        if (!_destroyed)
        {
            Vector2 steering = _SteeringArrive(targetPos.To2D(), SlowingRadius);
            steering += _SteeringSeparation(_game.Boids, _game.BaseBoidGrouping * 0.66f);
            steering += _SteeringEdgeRepulsion(_game.PlayRadius) * 2.0f;

            // limit angular velocity
            if (_velocity.LengthSquared() > 0)
            {
                Vector2 linearComp = _velocity.To2D().Normalized() * steering.Length() *
                                     steering.Normalized().Dot(_velocity.To2D().Normalized());
                Vector2 tangent = new Vector2(_velocity.z, -_velocity.x);
                Vector2 angularComp = tangent.Normalized() * steering.Length() *
                                      steering.Normalized().Dot(tangent.Normalized());
                steering = linearComp + angularComp.Normalized() *
                    Mathf.Clamp(angularComp.Length(), 0.0f, MaxAngularVelocity);
            }

            steering = steering.Truncate(_maxVelocity);
            _velocity += steering.To3D() * delta;
            _velocity = _velocity.Truncate(_maxVelocity);
        }

        GlobalTranslate(_velocity * delta);

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
            Vector3 from = _baseScale * 2.0f;
            _mesh.Scale = from.LinearInterpolate(_baseScale, 1.0f - t);

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
                foreach (BoidBase3D enemy in _game.Enemies)
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
                        float damage = _game.BaseBoidDamage * MicroBulletDamageMod;
                        mb.Init(dir * _game.BaseBulletSpeed, 0, _game.PlayRadius, damage);
                        _game.AddChild(mb);
                        _sfxHitMicroPlayer.Play();
                    }
                }
            }
        }
    }

    protected override void _Shoot(Vector2 dir)
    {
        base._Shoot(dir);
        
        _shootCooldown = _game.BaseBoidReload;
        Bullet3D bullet = BulletScene.Instance() as Bullet3D;
        float spread = _game.BaseBoidSpread;
        dir += new Vector2(-dir.y, dir.x) * (float) GD.RandRange(-spread, spread);
        bullet.Init(dir * _game.BaseBulletSpeed, 0, _game.PlayRadius, _game.BaseBoidDamage);
        bullet.GlobalPosition = GlobalPosition;
        _game.AddChild(bullet);
        _game.PushBack(this);
        float traumaMod = 1.0f - Mathf.Clamp(_game.NumBoids / 100.0f, 0.0f, 0.5f);
        GlobalCamera.Instance.AddTrauma(ShootTrauma * traumaMod);
        //_sprite.modulate = Colours.Grey;
    }

    public bool _CanShoot(Vector2 dir)
    {
        if (_destroyed)
            return false;
        // can shoot if there are no other boids in the shoot direction
        bool blocked = false;
        foreach (BoidBase3D boid in _game.Boids)
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

    protected override void _Destroy(bool score)
    {
        base._Destroy(score);
    }

    public override void _OnBoidAreaEntered(Area area)
    {
        BoidBase3D boid = area as BoidBase3D;
        if (boid is BoidAlly3D || boid is Player3D)
            return;
        
        if (boid != null && !boid.IsDestroyed())
        {
            boid.OnHit(HitDamage, false, _velocity.To2D(), false, GlobalPosition);
            _Destroy(false);
            return;
        }

        // Laser laser = area as Laser;
        // if (laser != null)
        // {
        //     _Destroy(false);
        //     return;
        // }
        //
        // Bullet bullet = area as Bullet;
        // if (bullet != null && bullet.Alignment == 1)
        // {
        //     bullet.OnHit();
        //     _Destroy(false);
        //     return;
        // }
    }
}