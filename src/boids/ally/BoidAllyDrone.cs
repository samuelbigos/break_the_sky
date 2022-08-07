using Godot;
using System;
using GodotOnReady.Attributes;

public partial class BoidAllyDrone : BoidAllyBase
{
    [Export] private PackedScene _bulletScene;
    [Export] private float _rotorSpinSpeed = 25.0f;

    [OnReadyGet] private MeshInstance _rotorMesh;
    [OnReadyGet] private AudioStreamPlayer2D _sfxShoot;

    public float ShootCooldown => _shootCooldown;
    
    private float _shootCooldown;
    private bool _cachedShoot;

    public override void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(data, onDestroy, position, velocity);

        SteeringBoid.DesiredDistFromTargetMin = _engageRange - 10.0f;
        SteeringBoid.DesiredDistFromTargetMax = _engageRange + 10.0f;
    }

    protected override void ProcessAlive(float delta)
    {
        base.ProcessAlive(delta);

        switch (_aiState)
        {
            case AIState.Engaged:
            {
                _shootCooldown -= delta;
                float distToTargetSq = (_targetBoid.GlobalPosition - GlobalPosition).LengthSquared();
                if (distToTargetSq < _resourceStats.AttackRangeSq && _shootCooldown <= 0.0f)
                {
                    Shoot();
                }
                break;
            }
            case AIState.Idle:
                break;
        }
        
        _rotorMesh.RotateY(_rotorSpinSpeed * delta);
    }
    
    protected override void _OnEnterAIState_Engaged()
    {
        base._OnEnterAIState_Engaged();
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, true);
    }

    private void Shoot()
    {
        Vector2 dir = (_targetBoid.GlobalPosition - GlobalPosition).Normalized();
        _shootCooldown = _resourceStats.AttackCooldown;
        Bullet bullet = _bulletScene.Instance() as Bullet;
        Game.Instance.AddChild(bullet);
        bullet.Init(GlobalPosition.To3D(), dir * _resourceStats.AttackVelocity, Alignment, _resourceStats.AttackDamage);
    }
}
