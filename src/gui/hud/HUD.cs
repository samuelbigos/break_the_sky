using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class HUD : Spatial
{
    [Export] private PackedScene _warningIndicatorScene;
    [Export] private NodePath _constructUIPath;
    
    private Control _constructUI;
    private Dictionary<BoidEnemyBase, WarningIndicator> _warningIndicators = new Dictionary<BoidEnemyBase, WarningIndicator>();
    

    public override void _Ready()
    {
        base._Ready();
        
        _constructUI = GetNode<Control>(_constructUIPath);

        Game.Instance.OnGameStateChanged += _OnGameStateChanged;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // ignore timescale
        delta = TimeSystem.UnscaledDelta;
        
        foreach (BoidEnemyBase boid in Game.Instance.EnemyBoids)
        {
            _warningIndicators.TryGetValue(boid, out WarningIndicator indicator);
            if (BoidOffScreen(boid, out Vector2 pos, 0.05f))
            {
                if (indicator != null)
                {
                    indicator.Visible = true;
                    indicator.GlobalPosition(GlobalCamera.Instance.ProjectToZero(pos));
                }
                else
                {
                    WarningIndicator newIndicator = _warningIndicatorScene.Instance<WarningIndicator>();
                    newIndicator.Target = boid;
                    AddChild(newIndicator);
                    newIndicator.GlobalPosition(GlobalCamera.Instance.ProjectToZero(pos));
                    _warningIndicators[boid] = newIndicator;
                    boid.OnBoidDestroyed += _OnBoidDestroyed;
                }
            }
            else if (indicator != null)
            {
                indicator.Visible = false;
            }
        }

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
        Vector2 screenPos = GlobalCamera.Instance.UnprojectPosition(boid.GlobalTransform.origin);
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
                _constructUI.Visible = false;
                break;
            case Game.State.Construct:
                _constructUI.Visible = true;
                break;
            case Game.State.Pause:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}
