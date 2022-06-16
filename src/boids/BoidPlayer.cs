using System;
using System.Collections.Generic;
using Godot;

public class BoidPlayer : BoidBase
{
    [Export] private NodePath _sfxPickupPath;

    private AudioStreamPlayer2D _sfxPickup;
    private Vector2 _velocity;

    public override void _Ready()
    {
        base._Ready();

        _sfxPickup = GetNode<AudioStreamPlayer2D>(_sfxPickupPath);

        _mesh.Visible = true;
    }

    protected override void ProcessAlive(float delta)
    {
        // manage ally boid formations
        int droneCount = 0;
        foreach (BoidAllyBase ally in BoidFactory.Instance.AllyBoids)
        {
            if (ally is not BoidAllyDrone)
                continue;
            
            droneCount++;
        }
        
        // TODO: optimise by caching the column lists
        if (droneCount > 0)
        {
            int colCount = Mathf.CeilToInt(Mathf.Sqrt(droneCount));
            int perCol = Mathf.CeilToInt((float)droneCount / colCount);
            List<List<BoidAllyDrone>> boidCols = new();
            for (int i = 0; i < colCount; i++)
            {
                boidCols.Add(new List<BoidAllyDrone>());
            }

            for (int i = 0; i < BoidFactory.Instance.AllyBoids.Count; i++)
            {
                BoidAllyBase ally = BoidFactory.Instance.AllyBoids[i];
                if (ally is not BoidAllyDrone)
                    continue;

                int col = (i / perCol) % colCount;
                boidCols[col].Add(ally as BoidAllyDrone);
            }
            
            for (int x = 0; x < boidCols.Count; x++)
            {
                boidCols[x].Sort(DroneSort);
                for (int y = 0; y < boidCols[x].Count; y++)
                {
                    BoidBase ally = boidCols[x][y];
                    ref SteeringManager.Boid boid = ref SteeringManager.Instance.GetBoid(ally.SteeringId);
                    boid.TargetOffset = CalcBoidOffset(x, y, colCount, perCol, boid.Radius * 3.5f).ToNumerics();
                }
            }
        }

        if (_acceptInput)
        {
            Vector2 mousePos = GameCamera.Instance.MousePosition;
            Vector2 lookAt = (mousePos - GlobalPosition).Normalized();
            Rotation = new Vector3(0.0f, -Mathf.Atan2(lookAt.x, -lookAt.y), 0.0f);

            Vector2 forward = new Vector2(0.0f, -1.0f);
            Vector2 left = new Vector2(-1.0f, 0.0f);

            Vector2 dir = new Vector2(0.0f, 0.0f);
            if (Input.IsActionPressed("w"))
            {
                dir += forward;
            }

            if (Input.IsActionPressed("s"))
            {
                dir += -forward;
            }

            if (Input.IsActionPressed("a"))
            {
                dir += left;
            }

            if (Input.IsActionPressed("d"))
            {
                dir += -left;
            }

            if (dir != new Vector2(0.0f, 0.0f))
            {
                dir = dir.Normalized();
                dir *= MaxVelocity * delta;
                _velocity += dir;
            }
            _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(Damping, 0.0f, 1.0f), delta * 60.0f);

            ref SteeringManager.Boid boid = ref SteeringManager.Instance.GetBoid(_steeringId);
            boid.Position += (_velocity * delta).ToNumerics();
            boid.Heading = lookAt.ToNumerics();
        }
        
        base.ProcessAlive(delta);
    }

    private int DroneSort(BoidAllyDrone x, BoidAllyDrone y)
    {
        return x.ShootCooldown < y.ShootCooldown ? -1 : 1;
    }

    private Vector2 CalcBoidOffset(int col, int idxInCol, int numCols, int perCol, float separation)
    {
        Vector2 offset = Vector2.Zero;
        offset.x = -(float)numCols * 0.5f + (float)col + 0.5f;
        offset.y = -(float)perCol * 0.5f + (float)idxInCol + 0.5f;
        offset *= separation;
        return offset;
    }

    public void RegisterPickup(PickupMaterial pickup)
    {
        pickup.OnCollected += _OnPickupCollected;
    }

    protected override void _Destroy(bool score, Vector3 hitDir, float hitStrength)
    {
        // TODO: do something when player destroyed.
    }

    public override void _OnBoidAreaEntered(Area area)
    {
    }

    private void _OnPickupCollected(PickupMaterial pickup)
    {
        SaveDataPlayer.MaterialCount += 1;
        _sfxPickup.Play();
    }
}