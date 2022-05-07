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
        get => GlobalCamera.Instance.ProjectPosition(RectGlobalPosition, 0.0f);
        set => RectGlobalPosition = GlobalCamera.Instance.UnprojectPosition(value);
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
