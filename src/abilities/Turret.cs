using Godot;
using System;

public partial class Turret : MeshInstance3D
{
    [Export] private PackedScene _bulletScene;
    [Export] private float ClampOffsetDeg;
    [Export] private float ClampRangeDeg = 180.0f;

    [Export] private Node3D _barrel1;
    [Export] private Node3D _barrel2;
     
    public BoidBase Owner;
    public BoidBase Target;

    public float BurstCooldown;
    public float BurstDuration;
    public float ShootVelocity;
    public float ShootCooldown;
    public float ShootDamage;
    public BoidBase.BoidAlignment Alignment;

    private double _burstCooldownTimer;
    private double _burstDurationTimer;
    private double _shootTimer;
    private int _shootCount;
    
    public override void _Process(double delta)
    {
        base._Process(delta);

        BoidAllyBase player = Game.Player;
        if (IsInstanceValid(player))
        {
            Vector2 toPlayer = (player.GlobalTransform.Origin - GlobalTransform.Origin).Normalized().To2D();
            Vector2 desiredDir = toPlayer;
            
            float angle = desiredDir.AngleToY();
            
            // TODO: fix
            float clampAngle = Owner.Heading.AngleToY() + Mathf.DegToRad(ClampOffsetDeg);
            float minAngle = clampAngle - Mathf.DegToRad(ClampRangeDeg) * 0.5f;
            float maxAngle = clampAngle + Mathf.DegToRad(ClampRangeDeg) * 0.5f;
            angle = Mathf.Clamp(angle, minAngle, maxAngle);

            Quaternion desired = new Quaternion(new Basis(Vector3.Up, angle));
            Quaternion current = GlobalTransform.Basis.GetRotationQuaternion();

            float speed = 2.0f;
            float length = desired.AngleTo(current);
            if (length > 0.0f)
            {
                double t = 1.0f / length;
                t = Mathf.Min(1.0f, t * delta * speed);
                GlobalTransform = new Transform3D(new Basis(current.Slerp(desired, (float) t)), GlobalTransform.Origin);
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
                    Vector2 toTarget = (player.GlobalTransform.Origin - GlobalTransform.Origin).Normalized().To2D();
                    if (toTarget.Dot(GlobalTransform.Basis.Z.To2D()) > 0.99f)
                    {
                        Shoot();
                    }
                }
            }
        }
    }

    private void Shoot()
    {
        Bullet bullet = _bulletScene.Instantiate() as Bullet;
        Game.Instance.AddChild(bullet);
        Vector3 spawnPos;
        if (_shootCount % 2 == 0)
        {
            spawnPos = _barrel1.GlobalTransform.Origin;
        }
        else
        {
            spawnPos = _barrel1.GlobalTransform.Origin;
        }
        bullet.Init(spawnPos, Target, false, ShootVelocity, ShootDamage, Alignment);
        
        _shootTimer = ShootCooldown;
        _shootCount++;
    }
}
