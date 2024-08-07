using System;
using Godot;

public partial class BoidEnemyLaser : BoidEnemyBase
{
    [Export] private NodePath _sfxLaserChargeNode;
    [Export] private NodePath _sfxLaserFireNode;
    [Export] private NodePath _laserAreaPath;
    [Export] private float GunTrackSpeed = 2.0f;

    [Export] private MeshInstance3D _laserWarningMesh;
    [Export] private MeshInstance3D _gunMesh;
    [Export] private LaserVFX _vfx;

    private Area3D _laserArea;
    private AudioStreamPlayer2D _sfxLaserCharge;
    private AudioStreamPlayer2D _sfxLaserFire;

    public enum LaserState
    {
        Inactive,
        Charging,
        Firing
    }

    private LaserState _laserState = LaserState.Inactive;
    private double _laserCooldownTimer;
    private double _laserChargeTimer;
    private double _laserDurationTimer;
    private double _flashingTimer;
    private int _flashState;
    private Vector2 _desiredGunHeading;
    private Vector2 _gunHeading = Vector2.Up;

    public override void _Ready()
    {
        base._Ready();
        
        _laserArea = GetNode<Area3D>(_laserAreaPath);
        _sfxLaserCharge = GetNode<AudioStreamPlayer2D>(_sfxLaserChargeNode);
        _sfxLaserFire = GetNode<AudioStreamPlayer2D>(_sfxLaserFireNode);
    }

    public override void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(data, onDestroy, position, velocity);
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        steeringBoid.DesiredDistFromTargetMin = EngageRange - EngageRange * 0.5f;
    }

    protected override void ProcessAlive(double delta)
    {
        _laserCooldownTimer -= delta;

        Basis basis = new(Vector3.Up, _gunHeading.AngleToY());
        _gunMesh.GlobalTransform = new Transform3D(basis, _gunMesh.GlobalTransform.Origin);
        
        if (_aiState == AIState.Engaged)
        {
            switch (_laserState)
            {
                case LaserState.Inactive:
                {
                    _desiredGunHeading = (_targetBoid.GlobalPosition - _gunMesh.GlobalTransform.Origin.To2D()).Normalized();

                    if (_laserCooldownTimer < 0.0f && CanFire())
                    {
                        _laserState = LaserState.Charging;
                        _laserChargeTimer = _resourceStats.AttackCharge;
                        LaserCharging();
                    }

                    if ((_targetBoid.GlobalPosition - GlobalPosition).Length() > EngageRange + 50.0f)
                    {
                        SwitchAiState(AIState.Seeking);
                    }

                    break;
                }
                case LaserState.Charging:
                {
                    _laserChargeTimer -= delta;
                    _flashingTimer -= delta;
                    if (_flashingTimer < 0.0f)
                    {
                        _flashingTimer = 0.1f;
                        _laserWarningMesh.Visible = _flashState == 1;
                        _flashState = (_flashState + 1) % 2;
                    }
                    if (_laserChargeTimer < 0.0f)
                    {
                        _laserState = LaserState.Firing;
                        _laserDurationTimer = _resourceStats.AttackDuration;
                        LaserFiring();
                    }

                    break;
                }
                case LaserState.Firing:
                {
                    _laserDurationTimer -= delta;
                    if (_laserDurationTimer < 0.0f)
                    {
                        _laserState = LaserState.Inactive;
                        _laserCooldownTimer = _resourceStats.AttackCooldown;
                        LaserInactive();
                    }

                    break;
                }
            }
        }
        else
        {
            _desiredGunHeading = _cachedHeading;
        }

        if (!_desiredGunHeading.IsNormalized())
        {
            _desiredGunHeading = Vector2.Up;
        }
        _gunHeading = _gunHeading.Slerp(_desiredGunHeading, (float)delta * GunTrackSpeed).Normalized();
        
        base.ProcessAlive(delta);
    }

    private bool CanFire()
    {
        if (_targetType != TargetType.Enemy)
            return false;

        return (_targetBoid.GlobalPosition - GlobalPosition).Normalized().Dot(_gunHeading) > 0.99f;
    }

    private void LaserCharging()
    {
        _vfx.Reset();
        _vfx.ChargeTime = _resourceStats.AttackCharge;
        _vfx.FireTime = _resourceStats.AttackDuration;
        _vfx.Start();
        _sfxLaserCharge.Play();
        _laserWarningMesh.Visible = true;
    }

    private void LaserFiring()
    {
        _sfxLaserFire.Play();
        _laserArea.Monitorable = true;
        _laserWarningMesh.Visible = false;
    }

    private void LaserInactive()
    {
        _laserArea.Monitorable = false;
        SwitchAiState(AIState.Seeking);
    }

    protected override void _OnDestroy(Vector2 hitDir, float hitStrength)
    {
        base._OnDestroy(hitDir, hitStrength);
        
        _laserWarningMesh.Visible = false;
        _laserArea.Monitorable = false;
        _vfx.QueueFree();
    }
    
    protected override void OnEnterAIState_Seeking()
    {
        base.OnEnterAIState_Seeking();
        
        _laserCooldownTimer = _resourceStats.AttackCooldown;
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, false);
    }

    protected override void OnEnterAIState_Engaged()
    {
        base.OnEnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, true);
    }
}