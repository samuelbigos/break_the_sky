using Godot;
using System;

public class ViewportCamera : Camera
{
    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!GameCamera.Instance.Null())
        {
            GlobalTransform = GameCamera.Instance.GlobalTransform;
            Fov = GameCamera.Instance.Fov;
        }
    }
}
