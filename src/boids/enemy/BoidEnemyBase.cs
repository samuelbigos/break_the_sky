using Godot;

public class BoidEnemyBase : BoidBase
{
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float HitTrauma = 0.05f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float MaxAngularVelocity = 1000.0f;

    public bool IsTargetted = false;
    
    protected override BoidAlignment Alignment => BoidAlignment.Enemy;
    protected override Color BaseColour => ColourManager.Instance.Secondary;
    
    public override void _Ready()
    {
        base._Ready();

        HitDamage = 9999.0f; // colliding with enemy boids should always destroy the allied boid.
    }

    protected override void _OnHit(float damage, bool score, Vector2 bulletVel, Vector3 pos)
    {
        base._OnHit(damage, score, bulletVel, pos);

        PauseManager.Instance.PauseFlash();
        GameCamera.Instance.AddTrauma(HitTrauma);
    }
    
    protected override void _Destroy(bool score, Vector3 hitDir, float hitStrength)
    {
        if (!_destroyed)
        {
            GameCamera.Instance.AddTrauma(DestroyTrauma);
            
            if (score && !_destroyed)
            {
                //_game.AddScore(Points, GlobalPosition, true);
            }
        }
        
        base._Destroy(score, hitDir, hitStrength);

        DataEnemyBoid data = Database.EnemyBoids.FindEntry<DataEnemyBoid>(ID);
        for (int i = 0; i < data.MaterialDropCount; i++)
        {
            PickupMaterial drop = _pickupMaterialScene.Instance<PickupMaterial>();
            _game.AddChild(drop);
            _player.RegisterPickup(drop);
            drop.GlobalTransform = GlobalTransform;
            float eject = 25.0f;
            drop.Init(new Vector2(Utils.Randf01(), Utils.Randf01()).Normalized() * eject, _player);
        }
    }
}