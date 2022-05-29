using Godot;

public class BoidEnemyBase : BoidBase
{
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float HitTrauma = 0.05f;
    [Export] public float EngageRange = 100.0f;

    public bool IsTargetted = false;

    public override BoidAlignment Alignment => BoidAlignment.Enemy;
    protected override Color BaseColour => ColourManager.Instance.Secondary;

    private int _cachedBehaviours;
    private bool _escorting;
    
    public override void _Ready()
    {
        base._Ready();

        HitDamage = 9999.0f; // colliding with enemy boids should always destroy the allied boid.
        
        SetSteeringBehaviourEnabled(FlockingManager.Behaviours.Cohesion, true);
        SetSteeringBehaviourEnabled(FlockingManager.Behaviours.Alignment, true);
        SetSteeringBehaviourEnabled(FlockingManager.Behaviours.Separation, true, 10.0f);
        SetSteeringBehaviourEnabled(FlockingManager.Behaviours.EdgeRepulsion, true, 2.0f);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // if (_targetType == TargetType.None)
        // {
        //     float distToPlayerSq = (_player.GlobalPosition - GlobalPosition).LengthSquared();
        //     if (distToPlayerSq < EngageRange * EngageRange)
        //     {
        //         SetTarget(TargetType.Enemy, _player);
        //         SetSteeringBehaviourEnabled(FlockingManager.Behaviours.Pursuit, true);
        //         SetSteeringBehaviourEnabled(FlockingManager.Behaviours.Wander, false);
        //     }
        // }

        if (_escorting)
        {
            float distToPlayerSq = (_player.GlobalPosition - GlobalPosition).LengthSquared();
            if (distToPlayerSq < EngageRange * EngageRange)
            {
                DropEscortAndEngage();
            }
        }
    }

    public void SetupEscort(BoidEnemyBase leader)
    {
        SetTarget(TargetType.Ally, leader);
        _cachedBehaviours = Behaviours;
        Behaviours = 0;
        _escorting = true;
        SetSteeringBehaviourEnabled(FlockingManager.Behaviours.Pursuit, true);
        SetSteeringBehaviourEnabled(FlockingManager.Behaviours.Separation, true);
    }

    private void DropEscortAndEngage()
    {
        Behaviours = _cachedBehaviours;
        SetTarget(TargetType.Enemy, _player);
        _escorting = false;
    }

    protected override void _OnHit(float damage, bool score, Vector2 bulletVel, Vector3 pos)
    {
        base._OnHit(damage, score, bulletVel, pos);

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
            drop.Init(new Vector2(Utils.RandfUnit(), Utils.RandfUnit()).Normalized() * eject, _player);
        }
    }
}