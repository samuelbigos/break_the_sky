using Godot;

public class BoidEnemyBase3D : BoidBase3D
{
    [Export] public float PickupDropRate = 0.25f;
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float HitTrauma = 0.05f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float MaxAngularVelocity = 1000.0f;
    
    public override BoidAlignment Alignment => BoidAlignment.Enemy;

    protected override void _OnHit(float damage, bool score, Vector2 bulletVel, bool microbullet, Vector2 pos)
    {
        base._OnHit(damage, score, bulletVel, microbullet, pos);

        PauseManager.Instance.PauseFlash();
        GlobalCamera.Instance.AddTrauma(HitTrauma);
    }
    
    protected override void _Destroy(bool score)
    {
        if (!_destroyed)
        {
            GlobalCamera.Instance.AddTrauma(DestroyTrauma);
            
            if (score && !_destroyed)
            {
                _game.AddScore(Points, GlobalPosition, true);
                if (GD.RandRange(0.0, 1.0) < PickupDropRate)
                {
                    _game.SpawnPickupAdd(GlobalPosition, false);
                }
            }
        }
        
        base._Destroy(score);
    }
}