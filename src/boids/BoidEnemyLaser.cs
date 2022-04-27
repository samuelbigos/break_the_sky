using Godot;

public class BoidEnemyLaser : BoidEnemyBase
{
    [Export] private float _targetLaserDist = 250.0f;
    [Export] private float _laserCooldown = 5.0f;
    [Export] private float _laserCharge = 1.0f;
    [Export] private float _laserDuration = 2.0f;

    [Export] private NodePath _sfxLaserChargeNode;
    [Export] private NodePath _sfxLaserFireNode;
    [Export] private NodePath _laserNode;
    [Export] private NodePath _rotorNode;
    
    public Laser _laser;
    public Sprite _rotor;
    public AudioStreamPlayer2D _sfxLaserCharge;
    public AudioStreamPlayer2D _sfxLaserFire;

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

        _laser = GetNode<Laser>(_laserNode);
        _rotor = GetNode<Sprite>(_rotorNode);
        _sfxLaserCharge = GetNode<AudioStreamPlayer2D>(_sfxLaserChargeNode);
        _sfxLaserFire = GetNode<AudioStreamPlayer2D>(_sfxLaserFireNode);

        _maxVelBase = MaxVelocity;
        _laser.Monitorable = false;
        _rotor.Modulate = ColourManager.Instance.Secondary;
        _laserCooldownTimer = _laserCooldown * 0.5f;
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

            // firin' mah lazor
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
                _laser.Update();
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

            _laser.state = _laserState;
            _rotor.Rotation = Mathf.PosMod(_rotor.Rotation + 50.0f * delta, Mathf.Pi * 2.0f);
        }
    }

    public void LaserCharging()
    {
        _laser.Update();
        _sfxLaserCharge.Play();
    }

    public void LaserFiring()
    {
        _laser.Update();
        _laser.Monitorable = true;
        _sfxLaserFire.Play();
    }

    public void LaserInactive()
    {
        _laser.Update();
        _laser.Monitorable = false;
    }

    public override Vector2 _SteeringPursuit(Vector2 targetPos, Vector2 targetVel)
    {
        if (_laserState == LaserState.Charging || _laserState == LaserState.Firing)
        {
            return new Vector2(0.0f, 0.0f);
        }

        Vector2 desiredVelocity = (targetPos - GlobalPosition).Normalized() * MaxVelocity;
        Vector2 steering = desiredVelocity - _velocity;
        return steering;
    }
}