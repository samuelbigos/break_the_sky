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

    public AudioStreamPlayer2D _sfxBeaconFire;

    private BeaconState _beaconState = BeaconState.Inactive;
    public float _beaconCooldown;
    public float _beaconCharge;
    public float _beaconDuration;
    public int _pulses;

    public override void _Ready()
    {
        base._Ready();
        
        _sfxBeaconFire = GetNode("SFXBeaconFire") as AudioStreamPlayer2D;
        _rotorMesh = GetNode<MeshInstance>(_rotorMeshPath);
    }

    protected override void ProcessAlive(float delta)
    {
        float distToTarget = (GlobalPosition - TargetPos).Length();
        
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
        
        base.ProcessAlive(delta);
    }

    private void FirePulse()
    {
        foreach (int i in GD.Range(0, BulletsPerPulse))
        {
            Bullet bullet = BulletScene.Instance() as Bullet;
            float f = i * Mathf.Pi * 2.0f / BulletsPerPulse;
            Vector2 dir = new Vector2(Mathf.Sin(f), -Mathf.Cos(f)).Normalized();
            Vector2 spawnPos = GlobalPosition + dir * 32.0f;
            bullet.Init(spawnPos.To3D(), dir * BulletSpeed, Alignment, 1.0f);
            Game.Instance.AddChild(bullet);
            _sfxBeaconFire.Play();
        }
    }
}