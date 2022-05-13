using Godot;
using System;
using System.Collections.Generic;

public class Clouds : Spatial
{
    [Export] private Texture3D _noise;
    [Export] private List<NodePath> _cloudLayerPaths;
    [Export] private NativeScript _anl;

    private List<MeshInstance> _cloudLayers = new List<MeshInstance>();
    private int _cloudMode = 0;
    
    public override void _Ready()
    {
        base._Ready();
        
        for (int i = 0; i < _cloudLayerPaths.Count; i++)
        {
            _cloudLayers.Add(GetNode<MeshInstance>(_cloudLayerPaths[i]));
            ShaderMaterial mat = _cloudLayers[i].GetSurfaceMaterial(0) as ShaderMaterial;

            switch (i)
            {
                case 0:
                    mat.SetShaderParam("u_colour_a", ColourManager.Instance.Accent);
                    mat.SetShaderParam("u_colour_b", ColourManager.Instance.White);
                    break;
                case 1:
                    mat.SetShaderParam("u_colour_a", ColourManager.Instance.Secondary);
                    mat.SetShaderParam("u_colour_b", ColourManager.Instance.Tertiary);
                    break;
            }
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (GlobalCamera.Instance != null)
        {
            Vector3 pos = GlobalCamera.Instance.GlobalTransform.origin;
            pos.y = 0.0f;
            GlobalTransform = GlobalTransform.Position(pos);
        }
    }
}
