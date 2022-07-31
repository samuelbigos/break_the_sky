using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using GodotOnReady.Attributes;

public partial class HUD : Singleton<HUD>
{
    [Export] private PackedScene _warningIndicatorScene;
    [Export] private PackedScene _boidIconScene;
    
    // level
    [OnReadyGet] private Button _openSkillTreeButton;
    [OnReadyGet] private ProgressBar _levelBar;
    [OnReadyGet] private Label _progressLabel;
    [OnReadyGet] private Label _levelLabel;
    
    // resources
    [Export] private NodePath _materialsValuePath;
    
    // fabrication
    [OnReadyGet] private Control _tabUIContainer;
    [Export] private NodePath _fabicateMenuPath;
    [Export] private NodePath _fabricateQueuePath;
    
    // skill trees
    [OnReadyGet] private Label _skillPointsValue;

    public Action<ResourceBoidAlly> OnFabricateButtonPressed;
    public Action<int> OnQueueButtonPressed;

    public bool RequestShowConstructMenu;
    
    private Dictionary<BoidEnemyBase, WarningIndicator> _warningIndicators = new();

    private Control _fabricateQueue;
    private Control _fabricateMenu;
    private Label _materialValue;
    private List<BoidIcon> _queueIcons = new();

    [OnReady] private void Ready()
    {
        _fabricateQueue = GetNode<Control>(_fabricateQueuePath);
        _fabricateMenu = GetNode<Control>(_fabicateMenuPath);
        _materialValue = GetNode<Label>(_materialsValuePath);

        Refresh();
        
        if (Game.Instance != null)
        {
            FabricateManager.Instance.OnPushQueue += _OnPushQueue;
            FabricateManager.Instance.OnPopQueue += _OnPopQueue;
            StateMachine_Game.OnGameStateChanged += _OnGameStateChanged;
        }

        _openSkillTreeButton.Connect("pressed", this, nameof(_OnOpenSkillTreeButtonPressed));

        SaveDataPlayer.OnLevelUp += _OnPlayedLevelUp;
    }

    private void _OnPlayedLevelUp(int level)
    {
        Refresh();
    }

    private void Refresh()
    {
        // setup fabrication buttons
        for (int i = 0; i < _fabricateMenu.GetChildCount(); i++)
        {
            _fabricateMenu.GetChild(i).QueueFree();
        }
        
        foreach (ResourceBoidAlly fabricant in FabricateManager.Instance.Fabricants)
        {
            if (!SaveDataPlayer.IsFabricantUnlocked(fabricant))
                continue;
            
            BoidIcon icon = _boidIconScene.Instance<BoidIcon>();
            _fabricateMenu.AddChild(icon);
            icon.OnPressed += _OnFabricateButtonPressed;
            icon.Init(fabricant, false);
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // ignore timescale
        delta = TimeSystem.UnscaledDelta;

#if TOOLS
        if (Game.Instance == null)
            return;
#endif
        
        // warning indicators
        foreach (BoidEnemyBase boid in BoidFactory.Instance.EnemyBoids)
        {
            _warningIndicators.TryGetValue(boid, out WarningIndicator indicator);
            if (BoidOffScreen(boid, out Vector2 pos, 0.05f))
            {
                if (!indicator.Null())
                {
                    indicator.Visible = true;
                    indicator.GlobalPosition(GameCamera.Instance.ProjectToZero(pos));
                }
                else
                {
                    WarningIndicator newIndicator = _warningIndicatorScene.Instance<WarningIndicator>();
                    newIndicator.Target = boid;
                    AddChild(newIndicator);
                    newIndicator.GlobalPosition(GameCamera.Instance.ProjectToZero(pos));
                    _warningIndicators[boid] = newIndicator;
                    boid.OnBoidDestroyed += _OnBoidDestroyed;
                }
            }
            else if (!indicator.Null())
            {
                indicator.Visible = false;
            }
        }
        
        // hud values
        _materialValue.Text = $"{SaveDataPlayer.MaterialCount}";
        _skillPointsValue.Text = $"{SaveDataPlayer.SkillPoints}";
        
        // level
        _openSkillTreeButton.Visible = SaveDataPlayer.SkillPoints > 0 && StateMachine_Game.CurrentState == StateMachine_Game.States.Play;
        float expInLevel = SaveDataPlayer.Experience - SaveDataPlayer.TotalExpRequiredForLevel(SaveDataPlayer.Level);
        float expRequiredForLevel = SaveDataPlayer.TotalExpRequiredForLevel(SaveDataPlayer.Level + 1) -
                                    SaveDataPlayer.TotalExpRequiredForLevel(SaveDataPlayer.Level);
        _levelBar.Value = expInLevel / expRequiredForLevel; 
        _progressLabel.Text = $"{(int)expInLevel} / {(int)expRequiredForLevel}";
        _levelLabel.Text = $"Level: {SaveDataPlayer.Level + 1}";

        // queue progress
        for (int i = 0; i < _queueIcons.Count; i++)
        {
            BoidIcon icon = _queueIcons[i];
            FabricateManager.Fabricant fab = FabricateManager.Instance.Queue[i];
            icon.UpdateProgress(1.0f - fab.TimeLeft / fab.TotalTime);
        }
    }

    private bool BoidOffScreen(BoidBase boid, out Vector2 edgePosition, float marginPercent)
    {
        edgePosition = Vector2.Zero;
        Vector2 screenPos = GameCamera.Instance.UnprojectPosition(boid.GlobalTransform.origin);
        Vector2 screen = GetViewport().Size;

        if (screenPos.x < 0.0f || screenPos.x > screen.x || screenPos.y < 0.0f || screenPos.y > screen.y)
        {
            float margin = marginPercent * screen.y;
            edgePosition = new Vector2(Mathf.Clamp(screenPos.x, 0.0f + margin, screen.x - margin),
                Mathf.Clamp(screenPos.y, 0.0f + margin, screen.y - margin));
            return true;
        }

        return false;
    }
    
    private void _OnPopQueue(int idx)
    {
        Debug.Assert(_queueIcons.Count > idx, "Invalid index when trying to remove from queue.");
        _queueIcons[idx].QueueFree();
        _queueIcons.RemoveAt(idx);
    }

    private void _OnPushQueue(ResourceBoidAlly data)
    {
        BoidIcon icon = _boidIconScene.Instance<BoidIcon>();
        _fabricateQueue.AddChild(icon);
        icon.OnPressed += _OnQueueButtonPressed;
        icon.Init(data, true);
        _queueIcons.Add(icon);
    }
    
    private void _OnBoidDestroyed(BoidBase boid)
    {
        _warningIndicators[boid as BoidEnemyBase].QueueFree();
        _warningIndicators.Remove(boid as BoidEnemyBase);
    }

    private void _OnGameStateChanged(StateMachine_Game.States state, StateMachine_Game.States prevState)
    {
        switch (state)
        {
            case StateMachine_Game.States.Play:
            case StateMachine_Game.States.TacticalPause:
                _tabUIContainer.Visible = false;
                break;
            case StateMachine_Game.States.Construct:
                RequestShowConstructMenu = false;
                _tabUIContainer.Visible = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void _OnFabricateButtonPressed(BoidIcon icon, ResourceBoid data)
    {
        OnFabricateButtonPressed?.Invoke(data as ResourceBoidAlly);
    }
    
    private void _OnQueueButtonPressed(BoidIcon icon, ResourceBoid boidData)
    {
        Debug.Assert(_queueIcons.Contains(icon), "Clicking icon that doesn't exist in queue list.");
        OnQueueButtonPressed?.Invoke(_queueIcons.IndexOf(icon));
    }

    private void _OnOpenSkillTreeButtonPressed()
    {
        RequestShowConstructMenu = true;
    }
}
