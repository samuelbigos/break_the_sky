using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class HUD : Spatial
{
    public static HUD Instance;
    
    [Export] private PackedScene _warningIndicatorScene;
    [Export] private PackedScene _boidIconScene;
    
    // resources
    [Export] private NodePath _materialsValuePath;
    
    // fabrication
    [Export] private NodePath _TabUIPath;
    [Export] private NodePath _fabicateMenuPath;
    [Export] private NodePath _fabricateQueuePath;
    
    public Action<string> OnFabricateButtonPressed;
    public Action<int> OnQueueButtonPressed;
    
    private Dictionary<BoidEnemyBase, WarningIndicator> _warningIndicators = new Dictionary<BoidEnemyBase, WarningIndicator>();

    private Control _tabUIContainer;
    private Control _fabricateQueue;
    private Control _fabricateMenu;
    private Label _materialValue;
    private List<BoidIcon> _queueIcons = new List<BoidIcon>();

    public HUD()
    {
        Debug.Assert(Instance == null, "Attempting to create multiple HUD instances!");
        Instance = this;
    }
    
    public override void _Ready()
    {
        base._Ready();
        
        _tabUIContainer = GetNode<Control>(_TabUIPath);
        _fabricateQueue = GetNode<Control>(_fabricateQueuePath);
        _fabricateMenu = GetNode<Control>(_fabicateMenuPath);
        _materialValue = GetNode<Label>(_materialsValuePath);

        // setup fabrication buttons
        List<DataAllyBoid> boids = Database.AllyBoids.GetAllEntries<DataAllyBoid>();
        foreach (DataAllyBoid boid in boids)
        {
            BoidIcon icon = _boidIconScene.Instance<BoidIcon>();
            _fabricateMenu.AddChild(icon);
            icon.OnPressed += _OnFabricateButtonPressed;
            icon.Init(boid.Name, false);
        }
        
        FabricateManager.Instance.OnPushQueue += _OnPushQueue;
        FabricateManager.Instance.OnPopQueue += _OnPopQueue;
        
        if (Game.Instance != null)
            Game.Instance.OnGameStateChanged += _OnGameStateChanged;
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
        foreach (BoidEnemyBase boid in Game.Instance.EnemyBoids)
        {
            _warningIndicators.TryGetValue(boid, out WarningIndicator indicator);
            if (BoidOffScreen(boid, out Vector2 pos, 0.05f))
            {
                if (indicator != null)
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
            else if (indicator != null)
            {
                indicator.Visible = false;
            }
        }
        
        // hud values
        _materialValue.Text = $"{SaveDataPlayer.MaterialCount}";
        
        // queue progress
        for (int i = 0; i < _queueIcons.Count; i++)
        {
            BoidIcon icon = _queueIcons[i];
            FabricateManager.Fabricant fab = FabricateManager.Instance.Queue[i];
            icon.UpdateProgress(1.0f - fab.TimeLeft / fab.TotalTime);
        }

        // input
        if (Input.IsActionJustPressed("open_construct_ui"))
        {
            switch (Game.Instance.CurrentState)
            {
                case Game.State.Play:
                    Game.Instance.ChangeGameState(Game.State.Construct);
                    break;
                case Game.State.Construct:
                    Game.Instance.ChangeGameState(Game.State.Play);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

    private void _OnPushQueue(string boidId)
    {
        BoidIcon icon = _boidIconScene.Instance<BoidIcon>();
        _fabricateQueue.AddChild(icon);
        icon.OnPressed += _OnQueueButtonPressed;
        icon.Init(boidId, true);
        _queueIcons.Add(icon);
    }
    
    private void _OnBoidDestroyed(BoidBase boid)
    {
        _warningIndicators[boid as BoidEnemyBase].QueueFree();
        _warningIndicators.Remove(boid as BoidEnemyBase);
    }

    private void _OnGameStateChanged(Game.State state, Game.State prevState)
    {
        switch (state)
        {
            case Game.State.Play:
                _tabUIContainer.Visible = false;
                break;
            case Game.State.Construct:
                _tabUIContainer.Visible = true;
                break;
            case Game.State.Pause:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void _OnFabricateButtonPressed(BoidIcon icon)
    {
        OnFabricateButtonPressed?.Invoke(icon.BoidID);
    }
    
    private void _OnQueueButtonPressed(BoidIcon icon)
    {
        Debug.Assert(_queueIcons.Contains(icon), "Clicking icon that doesn't exist in queue list.");
        OnQueueButtonPressed?.Invoke(_queueIcons.IndexOf(icon));
    }
}
