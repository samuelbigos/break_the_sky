using Godot;

public class BoidEnemyBase3D : BoidBase3D
{
    [Export] public float PickupDropRate = 0.25f;
    [Export] public int Points = 10;
    [Export] public float MaxHealth = 1.0f;
    [Export] public float HitFlashTime = 1.0f / 30.0f;
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float HitTrauma = 0.05f;
    [Export] public float MinVelocity = 0.0f;
    [Export] public float MaxAngularVelocity = 1000.0f;
    
    public float _health;
    public float _hitFlashTimer;
    public bool _move = true;
    public bool _destroyScore;

    public override void _Ready()
    {
        base._Ready();
        
        _health = MaxHealth;
    }
    
    public override void _Process(float delta)
    {
        base._Process(delta);

        // hit flash
        _hitFlashTimer -= delta;
        if (_hitFlashTimer < 0.0)
        {
            if (!_destroyed)
            {
                //_sprite.Modulate = ColourManager.Instance.Secondary;
            }
        }

        if (_health < 0.0f && !_destroyed)
        {
            _Destroy(_destroyScore);
        }
    }

    public override void OnHit(float damage, bool score, Vector2 bulletVel, bool microbullet, Vector2 pos)
    {
        base.OnHit(damage, score, bulletVel, microbullet, pos);

        _health -= damage;
        //_sprite.Modulate = ColourManager.Instance.White;
        _hitFlashTimer = HitFlashTime;
        CPUParticles2D hitParticles = HitParticles.Instance() as CPUParticles2D;
        hitParticles.Position = pos;
        hitParticles.Emitting = true;
        _game.AddChild(hitParticles);
        if (IsInstanceValid(_damagedParticles))
        {
            _damagedParticles.Emitting = true;
        }
        
        PauseManager.Instance.PauseFlash();
        GlobalCamera.Instance.AddTrauma(HitTrauma);

        _destroyScore = score;
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
    
    public override void _OnBoidAreaEntered(Area area)
    {
        Bullet3D bullet = area as Bullet3D;
        if (IsInstanceValid(bullet) && bullet.Alignment == 0 && !_destroyed)
        {
            OnHit(bullet.Damage, true, bullet.Velocity, bullet.Microbullet, bullet.GlobalPosition);
            area.QueueFree();
        }
    }
}