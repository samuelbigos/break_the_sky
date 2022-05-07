using Godot;

public class BoidEnemyBeacon : BoidEnemyBase
{
    [Export] private NodePath _rotorMeshPath;
    private MeshInstance _rotorMesh;
    
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

    public AudioStreamPlayer3D _sfxBeaconFire;

    private BeaconState _beaconState = BeaconState.Inactive;
    public float _beaconCooldown;
    public float _beaconCharge;
    public float _beaconDuration;
    public int _pulses;

    public override void _Ready()
    {
        base._Ready();
        
        _sfxBeaconFire = GetNode("SFXBeaconFire") as AudioStreamPlayer3D;
        _rotorMesh = GetNode<MeshInstance>(_rotorMeshPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        float distToTarget = (GlobalPosition - _target.GlobalPosition).Length();
        
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
            
            Vector3 rot = _rotorMesh.Rotation;
            rot.y = Mathf.PosMod(_rotorMesh.Rotation.y + 25.0f * delta, Mathf.Pi * 2.0f);
            _rotorMesh.Rotation = rot;
        }
    }

    private void FirePulse()
    {
        foreach (int i in GD.Range(0, BulletsPerPulse))
        {
            Bullet bullet = BulletScene.Instance() as Bullet;
            float f = (float) (i) * Mathf.Pi * 2.0f / (float) (BulletsPerPulse);
            Vector2 dir = new Vector2(Mathf.Sin(f), -Mathf.Cos(f)).Normalized();
            bullet.Init(dir * BulletSpeed, Alignment, _game.PlayRadius, 1.0f);
            bullet.GlobalPosition = GlobalPosition + dir * 32.0f;
            _game.AddChild(bullet);
            _sfxBeaconFire.Play();
        }
    }
}