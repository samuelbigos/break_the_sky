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
                if (_shootCooldown <= 0.0f)
                {
                    Shoot();
                }
                break;
            }
            case AIState.Idle:
                break;
        }

        if (_shootCooldown > 0.0f)
        {
            float t = _shootCooldown / _resourceStats.AttackCooldown;
            t = Mathf.Pow(Mathf.Clamp(t, 0.0f, 1.0f), 5.0f);
            Vector3 from = _baseScale * 2.0f;
            _mesh.Scale = from.LinearInterpolate(_baseScale, 1.0f - t);
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
