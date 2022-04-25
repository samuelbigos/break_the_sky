using Godot;

public class BoidEnemyBeacon : BoidEnemyBase
{
    [Export] private PackedScene BulletScene;

    [Export] public float TargetBeaconDist = 350.0f;
    [Export] public float BeaconCooldown = 5.0f;
    [Export] public float BeaconCharge = 1.0f;
    [Export] public float BeaconPulseDuration = 0.25f;
    [Export] public int Pulses = 5;
    [Export] public int BulletsPerPulse = 18;
    [Export] public float BulletSpeed = 300.0f;

    enum BeaconState
    {
        Inactive,
        Charging,
        Firing
    }

    public AudioStreamPlayer2D _sfxBeaconFire;
    public AudioStreamPlayer2D _sfxDestroy;

    private BeaconState _beaconState = BeaconState.Inactive;
    public float _beaconCooldown;
    public float _beaconCharge;
    public float _beaconDuration;
    public int _pulses;
    private Sprite _sprite;

    public override void _Ready()
    {
        base._Ready();
        
        _sfxBeaconFire = GetNode("SFXBeaconFire") as AudioStreamPlayer2D;
        _sprite.Modulate = ColourManager.Instance.Secondary;
    }

    public override void _Process(float delta)
    {
        var distToTarget = (GlobalPosition - _target.GlobalPosition).Length();

        // firin' mah lazor
        if (!_destroyed)
        {
            if (_beaconState == BeaconState.Inactive)
            {
                _beaconCooldown -= delta;
                if (distToTarget < TargetBeaconDist && _beaconCooldown < 0.0f)
                {
                    _beaconState = BeaconState.Charging;
                    _beaconCharge = BeaconCharge;
                }
            }

            if (_beaconState == BeaconState.Charging)
            {
                _beaconCharge -= delta;
                if (_beaconCharge < 0.0f)
                {
                    _beaconState = BeaconState.Firing;
                    _beaconDuration = 0.0f;
                    _pulses = Pulses;
                }
            }

            if (_beaconState == BeaconState.Firing)
            {
                _beaconDuration -= delta;
                if (_beaconDuration < 0.0f)
                {
                    _pulses -= 1;
                    if (_pulses < 0)
                    {
                        _beaconState = BeaconState.Inactive;
                        _beaconCooldown = BeaconCooldown;
                    }
                    else
                    {
                        FirePulse();
                        _beaconDuration = BeaconPulseDuration;
                    }
                }
            }
        }

        Rotation = -Mathf.Atan2(_velocity.x, _velocity.y);
    }

    public void FirePulse()
    {
        foreach (var i in GD.Range(0, BulletsPerPulse))
        {
            BulletBeacon bullet = BulletScene.Instance() as BulletBeacon;
            float f = (float) (i) * Mathf.Pi * 2.0f / (float) (BulletsPerPulse);
            Vector2 dir = new Vector2(Mathf.Sin(f), -Mathf.Cos(f)).Normalized();
            bullet.Init(dir * BulletSpeed, 1, _game.PlayRadius);
            bullet.GlobalPosition = GlobalPosition + dir * 32.0f;
            _game.AddChild(bullet);
            _sfxBeaconFire.Play();
        }
    }

    protected override void Destroy(bool score)
    {
        base.Destroy(score);
        _sfxDestroy.Play();
    }
}