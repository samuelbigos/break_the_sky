using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;

public class FabricateManager : Singleton<FabricateManager>
{
    [Export] private List<ResourceBoidAlly> _fabricantPool = new();
    
    public class Fabricant
    {
        public ResourceBoidAlly FabricantData;
        public float TotalTime;
        public float TimeLeft;
    }

    public IReadOnlyList<ResourceBoidAlly> Fabricants => _fabricantPool;
    public IReadOnlyList<Fabricant> Queue => _queue;
    
    public Action<ResourceBoidAlly> OnPushQueue;
    public Action<int> OnPopQueue;

    private List<Fabricant> _queue = new();

    public override void _Ready()
    {
        base._Ready();

        if (!HUD.Instance.Null())
        {
            HUD.Instance.OnFabricateButtonPressed += _OnFabricateButtonPressed;
            HUD.Instance.OnQueueButtonPressed += _OnQueueButtonPressed;
        }

        SaveDataPlayer.OnLevelUp += OnLevelUp;
    }

    private void OnLevelUp(int level)
    {
        if (level == 1)
            SaveDataPlayer.UnlockFabricant(_fabricantPool[0]);
    }

    public ResourceBoidAlly GetBoidResourceByUID(string UID)
    {
        foreach (ResourceBoidAlly res in _fabricantPool)
        {
            if (res.UniqueID == UID)
                return res;
        }

        return null;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (_queue.Count > 0)
        {
            _queue[0].TimeLeft -= delta;
            if (_queue[0].TimeLeft <= 0.0f && BoidFactory.Instance.CreateAllyBoid(_queue[0].FabricantData) != null)
            {
                RemoveFromQueue(0);
            }
        }
    }

    private void AddToQueue(ResourceBoidAlly fabricant)
    {
        Debug.Assert(fabricant != null, $"ID {fabricant} unknown.");
        _queue.Add(new Fabricant() { FabricantData = fabricant, TotalTime = fabricant.FabricateTime, TimeLeft = fabricant.FabricateTime });
        OnPushQueue?.Invoke(fabricant);
    }

    private void RemoveFromQueue(int idx, bool cancelled = false)
    {
        Debug.Assert(_queue.Count > idx, "Invalid index when trying to remove from queue.");
        
        // refund
        if (cancelled)
        {
            SaveDataPlayer.MaterialCount += _queue[idx].FabricantData.FabricateCost;
        }

        _queue.RemoveAt(idx);
        OnPopQueue?.Invoke(idx);
    }
    
    private void _OnFabricateButtonPressed(ResourceBoidAlly fabricant)
    {
        int mats = SaveDataPlayer.MaterialCount;
        if (mats >= fabricant.FabricateCost)
        {
            SaveDataPlayer.MaterialCount -= fabricant.FabricateCost;
            AddToQueue(fabricant);
        }
    }
    
    private void _OnQueueButtonPressed(int idx)
    {
        RemoveFromQueue(idx, true);
    }
    
    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.Instance.RegisterWindow("fabricate", "Fabricate", _OnImGuiLayoutWindow);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.Instance.UnRegisterWindow("fabricate", _OnImGuiLayoutWindow);
    }
    
    private void _OnImGuiLayoutWindow()
    {
        ImGui.Text($"Ally Boids: {BoidFactory.Instance.AllyBoids.Count}");
        foreach (ResourceBoidAlly fabricant in _fabricantPool)
        {
            if (ImGui.Button($"{fabricant.DisplayName}"))
            {
                BoidFactory.Instance.CreateAllyBoid(fabricant);
            }

            if (ImGui.Button($"{fabricant.DisplayName} x10"))
            {
                for (int i = 0; i < 10; i++)
                    BoidFactory.Instance.CreateAllyBoid(fabricant);
            }
        }
    }
}
