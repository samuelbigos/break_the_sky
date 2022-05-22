using Godot;
using System;

public class Tooltip : Control
{
    [Export] private NodePath _namePath;

    private Label _label;

    public string Text
    {
        set => _label.Text = value;
    }

    public Vector3 WorldPosition
    {
        get => GameCamera.Instance.ProjectPosition(RectGlobalPosition, 0.0f);
        set
        {
            if (GetViewport()?.GetCamera() != null)
                RectGlobalPosition = GetViewport().GetCamera().UnprojectPosition(value);
        }
    }

    public bool Showing
    {
        get => _label.Visible;
        set => _label.Visible = value;
    }
    
    public override void _Ready()
    {
        base._Ready();

        _label = GetNode<Label>(_namePath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
    }
}
