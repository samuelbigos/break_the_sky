using Godot;
using System;

[Tool]
public class CloudBox : MeshInstance
{
    private ShaderMaterial _mat;

    public override void _Ready()
    {
        base._Ready();
        
        _mat = GetActiveMaterial(0) as ShaderMaterial;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        _mat.SetShaderParam("u_cloud_box_min", GlobalTransform.origin - Scale * 0.5f);
        _mat.SetShaderParam("u_cloud_box_max", GlobalTransform.origin + Scale * 0.5f);
    }
}
