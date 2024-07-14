using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public partial class Water : MeshInstance3D
{
    [Export] private NodePath _waterMeshViewportPath;
    private SubViewport _waterMeshViewport;

    [Export] private Array<NodePath> _waveMapPaths;
    private List<SubViewport> _waveMaps = new List<SubViewport>();
    [Export] private Array<NodePath> _waveTextureRectPaths;
    private List<TextureRect> _waveTextureRects = new List<TextureRect>();
    
    [Export] private Array<NodePath> _blurViewportPaths;
    private List<SubViewport> _blurViewports = new List<SubViewport>();
    [Export] private Array<NodePath> _blurTextureRectPaths;
    private List<TextureRect> _blurTextureRects = new List<TextureRect>();

    private int _currentWaveMap;
    private float _waterWaveUpdateTimer;
    
    public override void _Ready()
    {
        base._Ready();
        
        _waterMeshViewport = GetNode<SubViewport>(_waterMeshViewportPath);
        
        _waveMaps.Add(GetNode<SubViewport>(_waveMapPaths[0]));
        _waveMaps.Add(GetNode<SubViewport>(_waveMapPaths[1]));
        _waveTextureRects.Add(GetNode<TextureRect>(_waveTextureRectPaths[0]));
        _waveTextureRects.Add(GetNode<TextureRect>(_waveTextureRectPaths[1]));
        
        _blurViewports.Add(GetNode<SubViewport>(_blurViewportPaths[0]));
        _blurViewports.Add(GetNode<SubViewport>(_blurViewportPaths[1]));
        _blurTextureRects.Add(GetNode<TextureRect>(_blurTextureRectPaths[0]));
        _blurTextureRects.Add(GetNode<TextureRect>(_blurTextureRectPaths[1]));

        // _waveMaps[0].GetTexture().Flags = (uint)Texture.FlagsEnum.Filter;
        // _waveMaps[1].GetTexture().Flags = (uint)Texture.FlagsEnum.Filter;
        //_blurViewports[0].GetTexture().Flags = (uint)Texture2D.FlagsEnum.Filter;
        //_blurViewports[1].GetTexture().Flags = (uint)Texture2D.FlagsEnum.Filter;
        
        _waveTextureRects[0].Texture = _waterMeshViewport.GetTexture();
        _waveTextureRects[1].Texture = _waterMeshViewport.GetTexture();
        _blurTextureRects[1].Texture = _blurViewports[0].GetTexture();
        ((ShaderMaterial) GetActiveMaterial(0)).SetShaderParameter("u_wave_texture", _blurViewports[1].GetTexture());
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (_waterWaveUpdateTimer <= 0.0f)
        {
            int nextWaveMap = (_currentWaveMap + 1) % 2;
            
            ShaderMaterial mat = _waveTextureRects[_currentWaveMap].Material as ShaderMaterial;
            mat?.SetShaderParameter("u_prev_wave", _waveMaps[nextWaveMap].GetTexture());
            
            _waveMaps[_currentWaveMap].RenderTargetUpdateMode = SubViewport.UpdateMode.Once;

            _blurTextureRects[0].Texture = _waveMaps[_currentWaveMap].GetTexture();

            _waterWaveUpdateTimer = 1.0f / 60.0f;
            _currentWaveMap = nextWaveMap;
        }

        _waterWaveUpdateTimer -= (float)delta;
    }
}
