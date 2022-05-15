using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class Clouds : Spatial
{
    [Export] private Texture3D _noise;
    [Export] private List<NodePath> _cloudLayerViewportPaths;
    [Export] private List<NodePath> _cloudLayerCameraPaths;
    [Export] private List<NodePath> _cloudLayerPaths;
    [Export] private NodePath _boidVelocityMapPath;
    [Export] private NodePath _boidTransparentMapPath;
    [Export] private List<NodePath> _displacementMapPaths;
    [Export] private List<NodePath> _displacementTextureRectPaths;
    [Export] private NodePath _boidVelMapCameraPath;
    [Export] private NodePath _debugCameraPath;
    
    private Viewport _boidVelocityMap;
    private Viewport _boidTransparentMap;
    private List<Viewport> _displacementMaps = new List<Viewport>();
    private List<Camera> _cloudLayerCameras = new List<Camera>();
    private List<TextureRect> _displacementTextureRects = new List<TextureRect>();
    private Camera _boidVelMapCamera;
    private List<Viewport> _cloudLayerViewports = new List<Viewport>();
    private List<MeshInstance> _cloudLayers = new List<MeshInstance>();
    private Camera _debugCamera;
    
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
        
        Debug.Assert(_cloudLayerViewportPaths.Count ==_cloudLayerPaths.Count);
        Debug.Assert(_cloudLayerCameraPaths.Count ==_cloudLayerPaths.Count);
        for (int i = 0; i < _cloudLayerPaths.Count; i++)
        {
            _cloudLayerViewports.Add(GetNode<Viewport>(_cloudLayerViewportPaths[i]));
            _cloudLayerCameras.Add(GetNode<Camera>(_cloudLayerCameraPaths[i]));
            _cloudLayers.Add(GetNode<MeshInstance>(_cloudLayerPaths[i]));
            
            ShaderMaterial mat = _cloudLayers[i].GetSurfaceMaterial(0) as ShaderMaterial;
            Debug.Assert(mat != null);
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

            _cloudLayerCameras[i].Size = Mathf.Max(GetViewport().Size.x, GetViewport().Size.y);
            _cloudLayerViewports[i].Size = GetViewport().Size;
        }

        _boidVelocityMap.Size = GetViewport().Size;
        _boidVelMapCamera.Size = Mathf.Max(GetViewport().Size.x, GetViewport().Size.y);

        _debugCamera = GetNode<Camera>(_debugCameraPath);
        _debugCamera.Size = Mathf.Max(GetViewport().Size.x, GetViewport().Size.y);

        if (GlobalCamera.Instance != null)
        {
            _debugCamera.QueueFree();
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (GlobalCamera.Instance != null)
        {
            Vector3 camPos = GlobalCamera.Instance.GlobalTransform.origin;
            MeshInstance c1 = GetNode<MeshInstance>("Layer1");
            MeshInstance c2 = GetNode<MeshInstance>("Layer2");
            c1.GlobalTransform = new Transform(c1.GlobalTransform.basis,
                new Vector3(camPos.x, c1.GlobalTransform.origin.y, camPos.z));
            c2.GlobalTransform = new Transform(c2.GlobalTransform.basis,
                new Vector3(camPos.x, c2.GlobalTransform.origin.y, camPos.z));

            _boidVelMapCamera.GlobalTransform = new Transform(_boidVelMapCamera.GlobalTransform.basis, 
                new Vector3(camPos.x, _boidVelMapCamera.GlobalTransform.origin.y, camPos.z));
            
            for (int i = 0; i < _cloudLayers.Count; i++)
            {
                Vector3 pos = camPos * 6.0f; // TODO: Fix hacky hardcoded value.
                
                MeshInstance mesh = _cloudLayers[i];
                Camera cam = _cloudLayerCameras[i];

                pos.y = mesh.GlobalTransform.origin.y;
                mesh.GlobalTransform = new Transform(mesh.GlobalTransform.basis, pos);
                
                pos.y = cam.GlobalTransform.origin.y;
                cam.GlobalTransform = new Transform(cam.GlobalTransform.basis, pos);
            }
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
