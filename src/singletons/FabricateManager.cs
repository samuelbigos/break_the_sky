using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class FabricateManager : Singleton<FabricateManager>
{
    public class Fabricant
    {
        public string BoidId;
        public float TotalTime;
        public float TimeLeft;
    }

    public IReadOnlyList<Fabricant> Queue => _queue;
    
    public Action<string> OnPushQueue;
    public Action<int> OnPopQueue;

    private List<Fabricant> _queue = new List<Fabricant>();

    public override void _Ready()
    {
        base._Ready();
        
        HUD.Instance.OnFabricateButtonPressed += _OnFabricateButtonPressed;
        HUD.Instance.OnQueueButtonPressed += _OnQueueButtonPressed;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (_queue.Count > 0)
        {
            _queue[0].TimeLeft -= delta;
            if (_queue[0].TimeLeft <= 0.0f && Game.Instance.AddAllyBoid(_queue[0].BoidId))
            {
                RemoveFromQueue(0);
            }
        }
    }

    private void AddToQueue(DataAllyBoid boid)
    {
        Debug.Assert(boid != null, $"ID {boid.Name} unknown.");
        _queue.Add(new Fabricant() { BoidId = boid.Name, TotalTime = boid.FabricateTime, TimeLeft = boid.FabricateTime });
        OnPushQueue?.Invoke(boid.Name);
    }

    private void RemoveFromQueue(int idx)
    {
        Debug.Assert(_queue.Count > idx, "Invalid index when trying to remove from queue.");
        
        // refund
        DataAllyBoid boid = Database.AllyBoid(_queue[idx].BoidId);
        SaveDataPlayer.MaterialCount += boid.FabricateCost;
        
        _queue.RemoveAt(idx);
        OnPopQueue?.Invoke(idx);
    }
    
    private void _OnFabricateButtonPressed(string boidId)
    {
        DataAllyBoid boid = Database.AllyBoids.FindEntry<DataAllyBoid>(boidId);
        int mats = SaveDataPlayer.MaterialCount;
        if (mats >= boid.FabricateCost)
        {
            SaveDataPlayer.MaterialCount -= boid.FabricateCost;
            AddToQueue(boid);
        }
    }
    
    private void _OnQueueButtonPressed(int idx)
    {
        RemoveFromQueue(idx);
    }
}
