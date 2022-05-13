using Godot;
using System;
using System.Collections.Generic;

public class Clouds : Spatial
{
    [Export] private Texture3D _noise;
    [Export] private List<NodePath> _cloudLayerPaths;

    private List<MeshInstance> _cloudLayers = new List<MeshInstance>();
    private int _cloudMode = 0;
    
    public override void _Ready()
    {
        base._Ready();
        
        for (int i = 0; i < _cloudLayerPaths.Count; i++)
        {
            _cloudLayers.Add(GetNode<MeshInstance>(_cloudLayerPaths[i]));
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        Vector3 pos = GlobalCamera.Instance.GlobalTransform.origin;
        pos.y = 0.0f;
        GlobalTransform = GlobalTransform.Position(pos);
        
        for (int i = 0; i < _cloudLayers.Count; i++)
        {
            ShaderMaterial mat = _cloudLayers[i].GetSurfaceMaterial(0) as ShaderMaterial;
            mat.SetShaderParam("u_mode", _cloudMode);
        }

        // if (Input.IsActionJustReleased("spacebar"))
        //     _cloudMode = _cloudMode == 0 ? 1 : 0;
    }
}
