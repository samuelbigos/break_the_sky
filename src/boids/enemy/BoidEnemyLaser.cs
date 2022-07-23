using Godot;
using GodotOnReady.Attributes;

public partial class BoidEnemyLaser : BoidEnemyBase
{
    [Export] private NodePath _sfxLaserChargeNode;
    [Export] private NodePath _sfxLaserFireNode;
    [Export] private NodePath _laserAreaPath;
    [Export] private float GunTrackSpeed = 2.0f;

    [OnReadyGet] private MeshInstance _laserWarningMesh;
    [OnReadyGet] private MeshInstance _gunMesh;
    [OnReadyGet] private LaserVFX _vfx;

    private Area _laserArea;
    private AudioStreamPlayer2D _sfxLaserCharge;
    private AudioStreamPlayer2D _sfxLaserFire;

    public enum LaserState
    {
        Inactive,
        Charging,
        Firing
    }

    private LaserState _laserState = LaserState.Inactive;
    private float _laserCooldownTimer;
    private float _laserChargeTimer;
    private float _laserDurationTimer;
    private float _flashingTimer;
    private int _flashState;
    private Vector2 _desiredGunHeading;
    private Vector2 _gunHeading = Vector2.Up;

    [OnReady] private void Ready()
    {
        _laserArea = GetNode<Area>(_laserAreaPath);
        _sfxLaserCharge = GetNode<AudioStreamPlayer2D>(_sfxLaserChargeNode);
        _sfxLaserFire = GetNode<AudioStreamPlayer2D>(_sfxLaserFireNode);
    }
    
    protected override void ProcessAlive(float delta)
    {
        _laserCooldownTimer -= delta;

        Basis basis = new(Vector3.Up, _gunHeading.AngleToY());
        _gunMesh.GlobalTransform = new Transform(basis, _gunMesh.GlobalTransform.origin);
        
        if (_aiState == AIState.Engaged)
        {
            switch (_laserState)
            {
                case LaserState.Inactive:
                {
                    _desiredGunHeading = (_targetBoid.GlobalPosition - _gunMesh.GlobalTransform.origin.To2D()).Normalized();

                    if (_laserCooldownTimer < 0.0f && CanFire())
                    {
                        _laserState = LaserState.Charging;
                        _laserChargeTimer = _resourceStats.AttackCharge;
                        LaserCharging();
                    }

                    if ((_targetBoid.GlobalPosition - GlobalPosition).Length() > EngageRange + 50.0f)
                    {
                        EnterAIState(AIState.Seeking);
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
        _gunHeading = _gunHeading.Slerp(_desiredGunHeading, delta * GunTrackSpeed).Normalized();
        
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
        EnterAIState(AIState.Seeking);
    }

    protected override void _Destroy(Vector2 hitDir, float hitStrength)
    {
        base._Destroy(hitDir, hitStrength);
        
        _laserWarningMesh.Visible = false;
    }
    
    protected override void OnEnterAIState_Seeking()
    {
        base.OnEnterAIState_Seeking();
        
        _laserCooldownTimer = _resourceStats.AttackCooldown;
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Stop, false);
    }

    protected override void OnEnterAIState_Engaged()
    {
        base.OnEnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Stop, true);
    }
}