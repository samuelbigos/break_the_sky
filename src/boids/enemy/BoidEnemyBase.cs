using System;
using System.Collections.Generic;
using Godot;

public class BoidEnemyBase : BoidBase
{
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float HitTrauma = 0.05f;
    [Export] public float EngageRange = 100.0f;

    public bool IsTargetted = false;

    protected override BoidAlignment Alignment => BoidAlignment.Enemy;
    protected override Color BaseColour => ColourManager.Instance.Secondary;

    private int _cachedBehaviours;
    private bool _escorting;
    
    public override void _Ready()
    {
        base._Ready();

        HitDamage = 9999.0f; // colliding with enemy boids should always destroy the allied boid.

        _cachedBehaviours = _behaviours;

        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Cohesion, true);
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Alignment, true);
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Separation, true, 10.0f);
    }

    protected override void ProcessAlive(float delta)
    {
        switch (_targetType)
        {
            case TargetType.None:
            {
                List<BoidAllyBase> allyBoids = BoidFactory.Instance.AllyBoids;
                foreach (BoidAllyBase boid in allyBoids)
                {
                    float distSq = (boid.GlobalPosition - GlobalPosition).LengthSquared();
                    if (distSq < EngageRange * EngageRange)
                    {
                        SetTarget(TargetType.Enemy, boid);
                        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
                        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Wander, false);
                        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Cohesion, false);
                        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Alignment, false);
                        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.FlowFieldFollow, false);
                    }
                }
                break;
            }
            case TargetType.Ally:
            case TargetType.Enemy:
            case TargetType.Position:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        // if (_escorting)
        // {
        //     float distToPlayerSq = (_player.GlobalPosition - GlobalPosition).LengthSquared();
        //     if (distToPlayerSq < EngageRange * EngageRange)
        //     {
        //         DropEscortAndEngage();
        //     }
        // }
        
        base.ProcessAlive(delta);
    }

    public void SetupEscort(BoidEnemyBase leader)
    {
        // SetTarget(TargetType.Ally, leader);
        // _cachedBehaviours = _behaviours;
        // _behaviours = 0;
        // _escorting = true;
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Separation, true);
    }

    private void DropEscortAndEngage()
    {
        // Behaviours = _cachedBehaviours;
        // SetTarget(TargetType.Enemy, _player);
        // _escorting = false;
    }

    protected override void _OnHit(float damage, bool score, Vector2 bulletVel, Vector3 pos)
    {
        base._OnHit(damage, score, bulletVel, pos);

        GameCamera.Instance.AddTrauma(HitTrauma);
    }
    
    protected override void _Destroy(bool score, Vector3 hitDir, float hitStrength)
    {
        base._Destroy(score, hitDir, hitStrength);

        DataEnemyBoid data = Database.EnemyBoids.FindEntry<DataEnemyBoid>(Id);
        for (int i = 0; i < data.MaterialDropCount; i++)
        {
            PickupMaterial drop = _pickupMaterialScene.Instance<PickupMaterial>();
            Game.Instance.RegisterPickup(drop);
            drop.GlobalTransform = GlobalTransform;
            float eject = 25.0f;
            drop.Init(new Vector2(Utils.RandfUnit(), Utils.RandfUnit()).Normalized() * eject, Game.Player);
        }
    }

    protected override void _OnTargetBoidDestroyed(BoidBase boid)
    {
        base._OnTargetBoidDestroyed(boid);
        
        _behaviours = _cachedBehaviours;
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        steeringBoid.Behaviours = _behaviours;
    }
}