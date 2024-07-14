using Godot;
using System;

public partial class ViewportCamera : Camera3D
{
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!GameCamera.Instance.Null())
        {
            GlobalTransform = GameCamera.Instance.GlobalTransform;
            Fov = GameCamera.Instance.Fov;
        }
    }
}
