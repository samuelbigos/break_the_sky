using Godot;
using System;
using GodotOnReady.Attributes;

public partial class Turret : MeshInstance
{
    [Export] private PackedScene _bulletScene;
    [Export] private float ClampOffsetDeg;
    [Export] private float ClampRangeDeg = 180.0f;

    [OnReadyGet] private Spatial _barrel1;
    [OnReadyGet] private Spatial _barrel2;
     
    public BoidBase Owner;
    public BoidBase Target;

    public float BurstCooldown;
    public float BurstDuration;
    public float ShootVelocity;
    public float ShootCooldown;
    public float ShootDamage;
    public BoidBase.BoidAlignment Alignment;

    private float _burstCooldownTimer;
    private float _burstDurationTimer;
    private float _shootTimer;
    private int _shootCount;
    
    public override void _Process(float delta)
    {
        base._Process(delta);

        BoidPlayer player = Game.Player;
        if (IsInstanceValid(player))
        {
            Vector2 toPlayer = (player.GlobalTransform.origin - GlobalTransform.origin).Normalized().To2D();
            Vector2 desiredDir = toPlayer;
            
            float angle = desiredDir.AngleToY();
            
            // TODO: fix
            float clampAngle = Owner.Heading.AngleToY() + Mathf.Deg2Rad(ClampOffsetDeg);
            float minAngle = clampAngle - Mathf.Deg2Rad(ClampRangeDeg) * 0.5f;
            float maxAngle = clampAngle + Mathf.Deg2Rad(ClampRangeDeg) * 0.5f;
            angle = Mathf.Clamp(angle, minAngle, maxAngle);

            Quat desired = new Quat(new Basis(Vector3.Up, angle));
            Quat current = GlobalTransform.basis.Quat();

            float speed = 2.0f;
            float length = desired.AngleTo(current);
            if (length > 0.0f)
            {
                float t = 1.0f / length;
                t = Mathf.Min(1.0f, t * delta * speed);
                GlobalTransform = new Transform(current.Slerp(desired, t), GlobalTransform.origin);
            }
        }

        _shootTimer -= delta;
        if (!Target.Null())
        {
            _burstCooldownTimer -= delta;
            if (_burstCooldownTimer < 0.0f)
            {
                _burstDurationTimer = BurstDuration;
                _burstCooldownTimer = BurstCooldown + BurstDuration;
            }

            if (_burstDurationTimer > 0.0f)
            {
                _burstDurationTimer -= delta;
                if (_shootTimer < 0.0f)
                {
                    Vector2 toTarget = (player.GlobalTransform.origin - GlobalTransform.origin).Normalized().To2D();
                    if (toTarget.Dot(GlobalTransform.basis.z.To2D()) > 0.99f)
                    {
                        Shoot();
                    }
                }
            }
        }
    }

    private void Shoot()
    {
        Bullet bullet = _bulletScene.Instance() as Bullet;
        Game.Instance.AddChild(bullet);
        Vector3 spawnPos;
        if (_shootCount % 2 == 0)
        {
            spawnPos = _barrel1.GlobalTransform.origin;
        }
        else
        {
            spawnPos = _barrel1.GlobalTransform.origin;
        }
        bullet.Init(spawnPos, GlobalTransform.basis.z.To2D() * ShootVelocity, Alignment, ShootDamage);
        
        _shootTimer = ShootCooldown;
        _shootCount++;
    }
}
