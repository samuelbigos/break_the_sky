using Godot;
using System;
using System.Collections.Generic;

public class HUD : Spatial
{
    [Export] private PackedScene _warningIndicatorScene;

    private Dictionary<BoidEnemyBase, WarningIndicator> _warningIndicators = new Dictionary<BoidEnemyBase, WarningIndicator>();

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

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
                    AddChild(newIndicator);
                    newIndicator.GlobalPosition(GlobalCamera.Instance.ProjectToZero(pos));
                    _warningIndicators[boid] = newIndicator;
                }
            }
            else if (indicator != null)
            {
                indicator.Visible = false;
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
    
    private float SdBox(in Vector2 p, in Vector2 b)
    {
        Vector2 d = p.Abs() - b;
        Vector2 maxD = new Vector2(Mathf.Max(d.x, 0.0f), Mathf.Max(d.y, 0.0f));
        return maxD.Length() + Mathf.Min(Mathf.Max(d.x, d.y), 0.0f);
    }
}
