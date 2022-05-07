using Godot;

public class BoidAllyBase : BoidBase
{
    [Export] private float _destroyTime = 3.0f;
    [Export] private float _shootSize = 1.5f;
    [Export] private float _shootTrauma = 0.05f;
    [Export] private float _destroyTrauma = 0.1f;
    [Export] private float _microBulletCd = 1.0f;
    [Export] private float _microBulletRange = 400.0f;
    [Export] private float _microBulletDamageMod = 0.25f;
    
    [Export] private PackedScene _micoBulletScene;
    
    [Export] private NodePath _sfxHitMicroPlayerNode;
    private AudioStreamPlayer3D _sfxHitMicroPlayer;
    
    [Export] private NodePath _sfxShootPlayerPath;
    private AudioStreamPlayer3D _sfxShootPlayer;

    protected override BoidAlignment Alignment => BoidAlignment.Ally;

    private float _microBulletTargetSearchTimer;
    private BoidBase _microBulletTarget;

    public override void _Ready()
    {
        base._Ready();
        
        _baseScale = _mesh.Scale;

        _sfxHitMicroPlayer = GetNode<AudioStreamPlayer3D>(_sfxHitMicroPlayerNode);
        _sfxShootPlayer = GetNode<AudioStreamPlayer3D>(_sfxShootPlayerPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        MaxVelocity = _game.BaseBoidSpeed;

        if (_game.BaseMicroturrets && !_destroyed)
        {
            if (!IsInstanceValid(_microBulletTarget) || _microBulletTarget.Destroyed)
            {
                _microBulletTarget = null;
            }

            _microBulletTargetSearchTimer -= delta;
            if (_microBulletTarget == null && _microBulletTargetSearchTimer < 0.0)
            {
                _microBulletTargetSearchTimer = 0.1f;
                foreach (BoidBase enemy in _game.Enemies)
                {
                    if ((enemy.GlobalPosition - GlobalPosition).Length() < _microBulletRange)
                    {
                        _microBulletTarget = enemy;
                        _microBulletCd = (float) GD.RandRange(_microBulletCd * 0.5f, _microBulletCd * 1.5f);
                    }
                }
            }

            if (IsInstanceValid(_microBulletTarget))
            {
                if ((_microBulletTarget.GlobalPosition - GlobalPosition).Length() > _microBulletRange)
                {
                    _microBulletTarget = null;
                    _microBulletTargetSearchTimer = 0.1f;
                }
                else
                {
                    _microBulletCd -= delta;
                    if (_microBulletCd < 0.0)
                    {
                        _microBulletCd = (float) GD.RandRange(_microBulletCd * 0.5f, _microBulletCd * 1.5f);
                        MicroBullet mb = _micoBulletScene.Instance() as MicroBullet;
                        float spread = _game.BaseBoidSpread;
                        Vector2 dir = (_microBulletTarget.GlobalPosition - GlobalPosition).Normalized();
                        dir += new Vector2(-dir.y, dir.x) * (float) GD.RandRange(-spread, spread);
                        float damage = _game.BaseBoidDamage * _microBulletDamageMod;
                        mb.Init(dir * _game.BaseBulletSpeed, 0, _game.PlayRadius, damage);
                        _game.AddChild(mb);
                        _sfxHitMicroPlayer.Play();
                    }
                }
            }
        }
    }
    
    protected virtual void _Shoot(Vector2 dir)
    {
        float traumaMod = 1.0f - Mathf.Clamp(_game.NumBoids / 100.0f, 0.0f, 0.5f);
        GlobalCamera.Instance.AddTrauma(_shootTrauma * traumaMod);
        
        _sfxShootPlayer.Play();
    }

    protected bool _CanShoot(Vector2 dir)
    {
        if (_destroyed)
            return false;
        
        // can shoot if there are no other boids in the shoot direction
        bool blocked = false;
        foreach (BoidBase boid in _game.Boids)
        {
            if (boid == this || boid.Destroyed)
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
}