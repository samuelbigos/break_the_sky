using Godot;
using System;

public partial class Tooltip : Control
{
    [Export] private NodePath _namePath;

    private Label _label;

    public string Text
    {
        set => _label.Text = value;
    }

    public Vector3 WorldPosition
    {
        get => GameCamera.Instance.ProjectPosition(GlobalPosition, 0.0f);
        set
        {
            if (GetViewport()?.GetCamera3D() != null)
                GlobalPosition = GetViewport().GetCamera3D().UnprojectPosition(value);
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

    public override void _Process(double delta)
    {
        base._Process(delta);
    }
}
