using System;
using Godot;

public class BoidEnemyCarrierRotorgun : BoidEnemyBase
{
    [Export] private PackedScene _bulletScene;
    [Export] private float _bulletSpeed = 200.0f;
    [Export] private float _bulletRange = 500.0f;
    [Export] private float _bulletCooldown = 1.0f;

    [Export] private NodePath _rotorMeshPath;
    private MeshInstance _rotorMesh;

    private Spatial _lock;
    private float _shotCooldown;

    private BoidEnemyCarrier _parent;

    public void InitRotorgun(Spatial lockNode, BoidEnemyCarrier parent)
    {
        _parent = parent;
        _lock = lockNode;
    }
    
    public override void _Ready()
    {
        base._Ready();

        _rotorMesh = GetNode<MeshInstance>(_rotorMeshPath);
        
        _parent = GetParent() as BoidEnemyCarrier;
    }

    public override void _Process(float delta)
    {
        Vector3 rotRot = _rotorMesh.Rotation;
        rotRot.y = Mathf.PosMod(_rotorMesh.Rotation.y + 100.0f * delta, Mathf.Pi * 2.0f);
        _rotorMesh.Rotation = rotRot;

        if (!_destroyed)
        {
            Vector2 toTarget = (TargetPos - GlobalPosition).Normalized();
            Vector2 awayParent = (_lock.GlobalTransform.origin.To2D() - GlobalPosition).Normalized();
            
            float dist = (TargetPos - GlobalPosition).Length();
            _shotCooldown -= delta;
            if (toTarget.Dot(awayParent) > 0.0f && _shotCooldown < 0.0f && dist < _bulletRange)
            {
                Shoot(new Vector2());
                _shotCooldown = _bulletCooldown;
            }
        }
    }

    private void Shoot(Vector2 dir)
    {
        Bullet bullet = _bulletScene.Instance() as Bullet;
        dir = (TargetPos - GlobalPosition).Normalized();
        Vector2 spawnPos = GlobalPosition + dir * 80.0f;
        bullet.Init(spawnPos, dir * _bulletSpeed, Alignment, 1.0f);
        Game.Instance.AddChild(bullet);
    }
}