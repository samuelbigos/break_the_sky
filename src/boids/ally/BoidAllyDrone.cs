using Godot;
using System;

public partial class BoidAllyDrone : BoidAllyBase
{
    [Export] private PackedScene _bulletScene;
    [Export] private float _rotorSpinSpeed = 25.0f;

    [Export] private MeshInstance3D _rotorMesh;
    [Export] private AudioStreamPlayer2D _sfxShoot;

    public double ShootCooldown => _shootCooldown;
    
    private double _shootCooldown;
    private bool _cachedShoot;

    public override void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(data, onDestroy, position, velocity);

        SteeringBoid.DesiredDistFromTargetMin = _engageRange - 10.0f;
        SteeringBoid.DesiredDistFromTargetMax = _engageRange + 10.0f;
    }

    protected override void ProcessAlive(double delta)
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
        
        _rotorMesh.RotateY((float) (_rotorSpinSpeed * delta));
    }
    
    protected override void _OnEnterAIState_Engaged()
    {
        base._OnEnterAIState_Engaged();
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, true);
    }

    private void Shoot()
    {
        _shootCooldown = _resourceStats.AttackCooldown;
        Bullet bullet = _bulletScene.Instantiate<Bullet>();
        Game.Instance.AddChild(bullet);
        bullet.Init(GlobalPosition.To3D(),  _targetBoid, true, _resourceStats.AttackVelocity, _resourceStats.AttackDamage, Alignment);
    }
}
