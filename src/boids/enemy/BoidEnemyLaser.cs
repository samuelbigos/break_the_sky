using Godot;
using GodotOnReady.Attributes;

public partial class BoidEnemyLaser : BoidEnemyBase
{
    [Export] private float _targetLaserDist = 250.0f;
    [Export] private float _laserCooldown = 5.0f;
    [Export] private float _laserCharge = 1.0f;
    [Export] private float _laserDuration = 2.0f;

    [Export] private NodePath _sfxLaserChargeNode;
    [Export] private NodePath _sfxLaserFireNode;
    [Export] private NodePath _rotorNode;
    [Export] private NodePath _laserAreaPath;

    [OnReadyGet] private MeshInstance _laserMesh;
    [OnReadyGet] private MeshInstance _laserWarningMesh;

    private MeshInstance _rotor;
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
    private float _maxVelBase;
    private float _flashingTimer;
    private int _flashState;

    [OnReady] private void Ready()
    {
        _rotor = GetNode<MeshInstance>(_rotorNode);
        _laserArea = GetNode<Area>(_laserAreaPath);
        _sfxLaserCharge = GetNode<AudioStreamPlayer2D>(_sfxLaserChargeNode);
        _sfxLaserFire = GetNode<AudioStreamPlayer2D>(_sfxLaserFireNode);

        _maxVelBase = MaxVelocity;
        _laserCooldownTimer = _laserCooldown * 0.5f;

        LaserInactive();

        SpatialMaterial mat = _laserMesh.GetActiveMaterial(0) as SpatialMaterial;
        mat.AlbedoColor = ColourManager.Instance.White;
    }
    
    protected override void ProcessAlive(float delta)
    {
        float distToTarget = (GlobalPosition - TargetPos).Length();
        if (distToTarget < _targetLaserDist && _laserState == LaserState.Inactive)
        {
            MaxVelocity = 50.0f;
        }
        else
        {
            MaxVelocity = _maxVelBase;
        }
        
        if (_laserState == LaserState.Inactive)
        {
            _laserCooldownTimer -= delta;
            if (distToTarget < _targetLaserDist && _laserCooldownTimer < 0.0f)
            {
                _laserState = LaserState.Charging;
                _laserChargeTimer = _laserCharge;
                LaserCharging();
            }
        }

        if (_laserState == LaserState.Charging)
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
                _laserDurationTimer = _laserDuration;
                LaserFiring();
            }
        }

        if (_laserState == LaserState.Firing)
        {
            _laserDurationTimer -= delta;
            if (_laserDurationTimer < 0.0f)
            {
                _laserState = LaserState.Inactive;
                _laserCooldownTimer = _laserCooldown;
                LaserInactive();
            }
        }

        Vector3 rot = _rotor.Rotation;
        rot.y = Mathf.PosMod(_rotor.Rotation.y + 50.0f * delta, Mathf.Pi * 2.0f);
        _rotor.Rotation = rot;
        
        base.ProcessAlive(delta);
    }

    private void LaserCharging()
    {
        _sfxLaserCharge.Play();
        _laserWarningMesh.Visible = true;
    }

    private void LaserFiring()
    {
        _sfxLaserFire.Play();
        _laserMesh.Visible = true;
        _laserArea.Monitorable = true;
        _laserWarningMesh.Visible = false;
    }

    private void LaserInactive()
    {
        _laserMesh.Visible = false;
        _laserArea.Monitorable = false;
    }

    // protected override Vector2 _SteeringPursuit(Vector2 targetPos, Vector2 targetVel)
    // {
    //     if (_laserState == LaserState.Charging || _laserState == LaserState.Firing)
    //     {
    //         return new Vector2(0.0f, 0.0f);
    //     }
    //
    //     return base._SteeringPursuit(targetPos, targetVel);
    // }

    protected override void _Destroy(bool score, Vector3 hitDir, float hitStrength)
    {
        base._Destroy(score, hitDir, hitStrength);
        
        _laserWarningMesh.Visible = false;
    }
}