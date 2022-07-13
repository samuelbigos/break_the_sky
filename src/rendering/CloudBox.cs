using Godot;
using System;

[Tool]
public class CloudBox : MeshInstance
{
    private ShaderMaterial _mat;
    private Vector3 _size;

    public override void _Ready()
    {
        base._Ready();
        
        _mat = GetActiveMaterial(0) as ShaderMaterial;
        CubeMesh cubeMesh = Mesh as CubeMesh;
        _size = cubeMesh.Size;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        _mat.SetShaderParam("u_cloud_box_min", GlobalTransform.origin - _size * 0.5f);
        _mat.SetShaderParam("u_cloud_box_max", GlobalTransform.origin + _size * 0.5f);
    }
}
