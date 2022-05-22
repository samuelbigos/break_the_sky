using Godot;
using System;

public class BoidAllyDrone : BoidAllyBase
{
    [Export] private PackedScene _bulletScene;
    
    private float _shootCooldown; 
    
    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        Vector2 shootDir = (GameCamera.Instance.MousePosition() - GlobalPosition).Normalized();
        
        _shootCooldown -= delta;
        if (Input.IsActionPressed("shoot") && _shootCooldown <= 0.0)
        {
            if (_CanShoot(shootDir))
            {
                _Shoot(shootDir);
            }
        }

        if (_shootCooldown > 0.0f)
        {
            float t = _shootCooldown / _game.BaseBoidReload;
            t = Mathf.Pow(Mathf.Clamp(t, 0.0f, 1.0f), 5.0f);
            Vector3 from = _baseScale * 2.0f;
            _mesh.Scale = from.LinearInterpolate(_baseScale, 1.0f - t);
        }
    }
    
    protected override void _Shoot(Vector2 dir)
    {
        base._Shoot(dir);
        
        _shootCooldown = _game.BaseBoidReload;
        Bullet bullet = _bulletScene.Instance() as Bullet;
        float spread = _game.BaseBoidSpread;
        dir += new Vector2(-dir.y, dir.x) * (float) GD.RandRange(-spread, spread);
        _game.AddChild(bullet);
        bullet.Init(GlobalPosition, dir * _game.BaseBulletSpeed, Alignment, _game.BaseBoidDamage);
        _game.PushBack(this);
    }
}
