using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public partial class Clouds : Node3D
{
    [Export] private Texture3D _noise;
    [Export] private Array<NodePath> _cloudLayerPaths;
    
    [Export] private NodePath _boidVelocityMapPath;
    private SubViewport _boidVelocityMap;
    [Export] private NodePath _boidTransparentMapPath;
    private SubViewport _boidTransparentMap;
    
    [Export] private Array<NodePath> _displacementMapPaths;
    private List<SubViewport> _displacementMaps = new List<SubViewport>();
    [Export] private Array<NodePath> _displacementTextureRectPaths;
    private List<TextureRect> _displacementTextureRects = new List<TextureRect>();
    [Export] private NodePath _boidVelMapCameraPath;
    private Camera3D _boidVelMapCamera;

    private List<MeshInstance3D> _cloudLayers = new List<MeshInstance3D>();
    private List<ShaderMaterial> _cloudMats = new List<ShaderMaterial>();
    private int _cloudMode = 0;
    private int _currentDisplacementMap;
    private float _waterDisplacementUpdateTimer;
    
    public override void _Ready()
    {
        base._Ready();

        _boidVelocityMap = GetNode<SubViewport>(_boidVelocityMapPath);
        _boidVelMapCamera = GetNode<Camera3D>(_boidVelMapCameraPath);
        _boidTransparentMap = GetNode<SubViewport>(_boidTransparentMapPath);
        
        _displacementMaps.Add(GetNode<SubViewport>(_displacementMapPaths[0]));
        _displacementMaps.Add(GetNode<SubViewport>(_displacementMapPaths[1]));
        _displacementTextureRects.Add(GetNode<TextureRect>(_displacementTextureRectPaths[0]));
        _displacementTextureRects.Add(GetNode<TextureRect>(_displacementTextureRectPaths[1]));
        
        _displacementTextureRects[0].Texture = _boidVelocityMap.GetTexture();
        _displacementTextureRects[1].Texture = _boidVelocityMap.GetTexture();
        
        for (int i = 0; i < _cloudLayerPaths.Count; i++)
        {
            _cloudLayers.Add(GetNode<MeshInstance3D>(_cloudLayerPaths[i]));
            _cloudMats.Add(_cloudLayers[i].GetSurfaceOverrideMaterial(0) as ShaderMaterial);
            ShaderMaterial mat = _cloudMats[i];

            switch (i)
            {
                case 0:
                    mat.SetShaderParameter("u_colour_a", ColourManager.Instance.Accent);
                    mat.SetShaderParameter("u_colour_b", ColourManager.Instance.White);
                    break;
                case 1:
                    mat.SetShaderParameter("u_colour_a", ColourManager.Instance.Four);
                    mat.SetShaderParameter("u_colour_b", ColourManager.Instance.Tertiary);
                    break;
            }
            
            mat.SetShaderParameter("u_boid_vel_tex", _boidVelocityMap.GetTexture());
            mat.SetShaderParameter("u_transparent_tex", _boidTransparentMap);
            mat.SetShaderParameter("u_transparent_col", ColourManager.Instance.Secondary);
            mat.SetShaderParameter("u_plane_size", ((PlaneMesh) _cloudLayers[i].Mesh).Size);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!GameCamera.Instance.Null())
        {
            _boidVelMapCamera.GlobalTransform = GameCamera.Instance.BaseTransform;
            
            Vector3 pos = GameCamera.Instance.BaseTransform.Origin;
            pos.Y = 0.0f;
            this.GlobalPosition(pos);

            // parallax
            ShaderMaterial mat = _cloudLayers[1].GetSurfaceOverrideMaterial(0) as ShaderMaterial;
            mat.SetShaderParameter("u_parallax_offset", -GameCamera.Instance.GlobalTransform.Origin.To2D() * 0.25f);
        }

        if (!Game.Instance.Null())
        {
            float displaceRadius = 12.5f;
            List<BoidBase> boids = new();
            float cloudY = _cloudLayers[1].GlobalTransform.Origin.Y;
            foreach (BoidBase boid in BoidFactory.Instance.DestroyedBoids)
            {
                float boidY = boid.GlobalTransform.Origin.Y;
                if (boidY < cloudY + displaceRadius && boidY > cloudY - displaceRadius)
                {
                    boids.Add(boid);
                }

                if (boids.Count >= 5)
                    break;
            }
            
            _cloudMats[1].SetShaderParameter("u_pos_y", cloudY);
            _cloudMats[1].SetShaderParameter("u_num_boids", boids.Count);
            _cloudMats[1].SetShaderParameter("u_displace_radius", displaceRadius);
            for (int i = 0; i < boids.Count; i++)
            {
                _cloudMats[1].SetShaderParameter($"u_boid_pos_{i + 1}", boids[i].GlobalTransform.Origin);
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
