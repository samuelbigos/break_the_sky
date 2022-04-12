using Godot;

public class BoidEnemyLaser : BoidEnemyBase
{
    [Export] public float TargetLaserDist = 250.0f;
    [Export] public float LaserCooldown = 5.0f;
    [Export] public float LaserCharge = 1.0f;
    [Export] public float LaserDuration = 2.0f;

    public Laser _laser;
    public Sprite _rotor;
    public AudioStreamPlayer _sfxLaserCharge;
    public AudioStreamPlayer _sfxLaserFire;

    public enum LaserState
    {
        Inactive,
        Charging,
        Firing
    }

    private LaserState _laserState = LaserState.Inactive;
    public float _laserCooldown;
    public float _laserCharge;
    public float _laserDuration;
    public float _maxVelBase;


    public override void _Ready()
    {
        base._Ready();

        _laser = GetNode("LaserArea") as Laser;
        _rotor = GetNode("Rotor") as Sprite;
        _sfxLaserCharge = GetNode("SFXLaserCharge") as AudioStreamPlayer;
        _sfxLaserFire = GetNode("SFXLaserFire") as AudioStreamPlayer;

        _maxVelBase = MaxVelocity;
        _laser.Monitorable = false;
        _rotor.Modulate = ColourManager.Instance.Secondary;
        _laserCooldown = LaserCooldown * 0.5f;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        var distToTarget = (GlobalPosition - _target.GlobalPosition).Length();
        if (distToTarget < TargetLaserDist && _laserState == LaserState.Inactive)
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
                _laserCooldown -= delta;
                if (distToTarget < TargetLaserDist && _laserCooldown < 0.0f)
                {
                    _laserState = LaserState.Charging;
                    _laserCharge = LaserCharge;
                    LaserCharging();
                }
            }

            if (_laserState == LaserState.Charging)
            {
                _laser.Update();
                _laserCharge -= delta;
                if (_laserCharge < 0.0f)
                {
                    _laserState = LaserState.Firing;
                    _laserDuration = LaserDuration;
                    LaserFiring();
                }
            }

            if (_laserState == LaserState.Firing)
            {
                _laserDuration -= delta;
                if (_laserDuration < 0.0f)
                {
                    _laserState = LaserState.Inactive;
                    _laserCooldown = LaserCooldown;
                    LaserInactive();
                }
            }

            _laser.state = _laserState;
            _rotor.Rotation = Mathf.PosMod(_rotor.Rotation + 50.0f * delta, Mathf.Pi * 2.0f);
        }

        Rotation = -Mathf.Atan2(_velocity.x, _velocity.y);
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

        var desiredVelocity = (targetPos - GlobalPosition).Normalized() * MaxVelocity;
        var steering = desiredVelocity - _velocity;
        return steering;
    }

    protected override void Destroy(bool score)
    {
        base.Destroy(score);
        
        _laser.QueueFree();
        _rotor.QueueFree();
        _sfxDestroy.Play();
    }
}