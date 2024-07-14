using Godot;
using System;

[Tool]
public partial class CloudBox : MeshInstance3D
{
    private ShaderMaterial _mat;

    public override void _Ready()
    {
        base._Ready();
        
        _mat = GetActiveMaterial(0) as ShaderMaterial;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        _mat.SetShaderParameter("u_cloud_box_min", GlobalTransform.Origin - Scale * 0.5f);
        _mat.SetShaderParameter("u_cloud_box_max", GlobalTransform.Origin + Scale * 0.5f);
    }
}
