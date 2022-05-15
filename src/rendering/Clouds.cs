using Godot;
using System;
using System.Collections.Generic;

public class Clouds : Spatial
{
    [Export] private Texture3D _noise;
    [Export] private List<NodePath> _cloudLayerPaths;
    
    [Export] private NodePath _boidVelocityMapPath;
    private Viewport _boidVelocityMap;
    [Export] private NodePath _boidTransparentMapPath;
    private Viewport _boidTransparentMap;
    
    [Export] private List<NodePath> _displacementMapPaths;
    private List<Viewport> _displacementMaps = new List<Viewport>();
    [Export] private List<NodePath> _displacementTextureRectPaths;
    private List<TextureRect> _displacementTextureRects = new List<TextureRect>();
    [Export] private NodePath _boidVelMapCameraPath;
    private Camera _boidVelMapCamera;

    private List<MeshInstance> _cloudLayers = new List<MeshInstance>();
    private int _cloudMode = 0;
    private int _currentDisplacementMap;
    private float _waterDisplacementUpdateTimer;
    
    public override void _Ready()
    {
        base._Ready();

        _boidVelocityMap = GetNode<Viewport>(_boidVelocityMapPath);
        _boidVelMapCamera = GetNode<Camera>(_boidVelMapCameraPath);
        _boidTransparentMap = GetNode<Viewport>(_boidTransparentMapPath);
        
        _displacementMaps.Add(GetNode<Viewport>(_displacementMapPaths[0]));
        _displacementMaps.Add(GetNode<Viewport>(_displacementMapPaths[1]));
        _displacementTextureRects.Add(GetNode<TextureRect>(_displacementTextureRectPaths[0]));
        _displacementTextureRects.Add(GetNode<TextureRect>(_displacementTextureRectPaths[1]));
        
        _displacementTextureRects[0].Texture = _boidVelocityMap.GetTexture();
        _displacementTextureRects[1].Texture = _boidVelocityMap.GetTexture();
        
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
                    mat.SetShaderParam("u_colour_a", ColourManager.Instance.Four);
                    mat.SetShaderParam("u_colour_b", ColourManager.Instance.Tertiary);
                    break;
            }
            
            mat.SetShaderParam("u_boid_vel_tex", _boidVelocityMap.GetTexture());
            mat.SetShaderParam("u_transparent_tex", _boidTransparentMap);
            mat.SetShaderParam("u_transparent_col", ColourManager.Instance.Secondary);
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (GlobalCamera.Instance != null)
        {
            _boidVelMapCamera.GlobalTransform = GlobalCamera.Instance.BaseTransform;
            
            Vector3 pos = GlobalCamera.Instance.BaseTransform.origin;
            pos.y = 0.0f;
            this.GlobalPosition(pos);

            // parallax
            ShaderMaterial mat = _cloudLayers[1].GetSurfaceMaterial(0) as ShaderMaterial;
            mat.SetShaderParam("u_parallax_offset", -GlobalCamera.Instance.GlobalTransform.origin.To2D() * 0.25f);
        }

        // _waterDisplacementUpdateTimer -= delta;
        // if (_waterDisplacementUpdateTimer <= 0.0f)
        // {
        //     int nextWaveMap = (_currentDisplacementMap + 1) % 2;
        //     
        //     ShaderMaterial mat = _displacementTextureRects[_currentDisplacementMap].Material as ShaderMaterial;
        //     mat?.SetShaderParam("u_prev_wave", _displacementMaps[nextWaveMap].GetTexture());
        //     _displacementMaps[_currentDisplacementMap].RenderTargetUpdateMode = Viewport.UpdateMode.Once;
        //
        //     foreach (MeshInstance t in _cloudLayers)
        //     {
        //         ShaderMaterial cloudMat = t.GetSurfaceMaterial(0) as ShaderMaterial;
        //         cloudMat.SetShaderParam("u_displacement_map", _displacementMaps[_currentDisplacementMap].GetTexture());
        //     }
        //
        //     _waterDisplacementUpdateTimer = 1.0f / 60.0f;
        //     _currentDisplacementMap = nextWaveMap;
        // }
    }
}
