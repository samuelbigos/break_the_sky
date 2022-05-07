using Godot;

public class BoidEnemyLaser : BoidEnemyBase
{
    [Export] private float _targetLaserDist = 250.0f;
    [Export] private float _laserCooldown = 5.0f;
    [Export] private float _laserCharge = 1.0f;
    [Export] private float _laserDuration = 2.0f;

    [Export] private NodePath _sfxLaserChargeNode;
    [Export] private NodePath _sfxLaserFireNode;
    [Export] private NodePath _rotorNode;
    [Export] private NodePath _laserMeshPath;
    [Export] private NodePath _laserAreaPath;

    private MeshInstance _rotor;
    private MeshInstance _laserMesh;
    private Area _laserArea;
    private AudioStreamPlayer3D _sfxLaserCharge;
    private AudioStreamPlayer3D _sfxLaserFire;

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

    public override void _Ready()
    {
        base._Ready();
        
        _rotor = GetNode<MeshInstance>(_rotorNode);
        _laserMesh = GetNode<MeshInstance>(_laserMeshPath);
        _laserArea = GetNode<Area>(_laserAreaPath);
        _sfxLaserCharge = GetNode<AudioStreamPlayer3D>(_sfxLaserChargeNode);
        _sfxLaserFire = GetNode<AudioStreamPlayer3D>(_sfxLaserFireNode);

        _maxVelBase = MaxVelocity;
        _laserCooldownTimer = _laserCooldown * 0.5f;

        LaserInactive();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        float distToTarget = (GlobalPosition - _target.GlobalPosition).Length();
        if (distToTarget < _targetLaserDist && _laserState == LaserState.Inactive)
        {
            MaxVelocity = 50.0f;
        }
        else
        {
            MaxVelocity = _maxVelBase;
        }

        if (!_destroyed)
        {
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
        }
    }

    private void LaserCharging()
    {
        _sfxLaserCharge.Play();
    }

    private void LaserFiring()
    {
        _sfxLaserFire.Play();
        _laserMesh.Visible = true;
        _laserArea.Monitorable = true;
    }

    private void LaserInactive()
    {
        _laserMesh.Visible = false;
        _laserArea.Monitorable = false;
    }

    protected override void _Destroy(bool score)
    {
        base._Destroy(score);
    }

    protected override Vector2 _SteeringPursuit(Vector2 targetPos, Vector2 targetVel)
    {
        if (_laserState == LaserState.Charging || _laserState == LaserState.Firing)
        {
            return new Vector2(0.0f, 0.0f);
        }

        return base._SteeringPursuit(targetPos, targetVel);
    }
}