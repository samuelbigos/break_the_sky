using Godot;

public class BoidEnemyCarrierRotorgun : BoidEnemyBase
{
    [Export] private PackedScene _bulletScene;
    [Export] private float _bulletSpeed = 200.0f;
    [Export] private float _bulletRange = 500.0f;
    [Export] private float _bulletCooldown = 1.0f;

    [Export] private NodePath _rotorMeshPath;
    private MeshInstance _rotorMesh;

    private Spatial _lock;
    private float _shotCooldown;

    private BoidEnemyCarrier _parent;

    public override void Init(Player player, Game game, BoidBase target)
    {
        _player = player;
        _game = game;
        _target = target;
    }

    public void InitRotorgun(Spatial lockNode, BoidEnemyCarrier parent)
    {
        _parent = parent;
        _lock = lockNode;
    }
    
    public override void _Ready()
    {
        base._Ready();

        _rotorMesh = GetNode<MeshInstance>(_rotorMeshPath);
        
        _parent = GetParent() as BoidEnemyCarrier;
    }

    public override void _Process(float delta)
    {
        Vector3 rotRot = _rotorMesh.Rotation;
        rotRot.y = Mathf.PosMod(_rotorMesh.Rotation.y + 100.0f * delta, Mathf.Pi * 2.0f);
        _rotorMesh.Rotation = rotRot;

        if (!_destroyed)
        {
            Vector2 toTarget = (_target.GlobalPosition - GlobalPosition).Normalized();
            Vector2 awayParent = (_lock.GlobalTransform.origin.To2D() - GlobalPosition).Normalized();
            
            float dist = (_target.GlobalPosition - GlobalPosition).Length();
            _shotCooldown -= delta;
            if (toTarget.Dot(awayParent) > 0.0f && _shotCooldown < 0.0f && dist < _bulletRange)
            {
                _Shoot(new Vector2());
                _shotCooldown = _bulletCooldown;
            }
        }
    }

    protected override void _Shoot(Vector2 dir)
    {
        base._Shoot(dir);
        
        Bullet bullet = _bulletScene.Instance() as Bullet;
        dir = (_target.GlobalPosition - GlobalPosition).Normalized();
        bullet.Init(dir * _bulletSpeed, Alignment, _game.PlayRadius, 1.0f);
        bullet.GlobalPosition = GlobalPosition + dir * 80.0f;
        _game.AddChild(bullet);
    }
}