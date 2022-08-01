using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;
using ImGuiNET;

public partial class Game : Singleton<Game>
{
    [OnReadyGet] private StateMachine_Game _stateMachine;
    [OnReadyGet] private AISpawningDirector _aiSpawningDirector;
    [OnReadyGet] private HUD _hud;
    [OnReadyGet] private MeshInstance _sand;
    [OnReadyGet] private CloudBox _clouds;
    [OnReadyGet] private Viewport _outlineViewport;
    [OnReadyGet] private MeshInstance _outlineMesh;

    [Export] private Rect2 _areaRect;
    [Export] private PackedScene _playerScene;

    [Export] private ResourceBoidAlly _playerData;

    private BoidPlayer _player;

    public static BoidPlayer Player => Instance._player;
    public Rect2 SpawningRect => new(Player.GlobalPosition - _areaRect.Size * 0.5f, _areaRect.Size);

    private float _debugRenderSize = 1.0f;
    private ShaderMaterial _outlineShader;

    [OnReady] private void Ready()
    {
        _player = _playerScene.Instance<BoidPlayer>();
        AddChild(_player);
        _player.Init(_playerData, _OnPlayerDestroyed, Vector2.Zero, Vector2.Zero);
        BoidFactory.Instance.AllyBoids.Add(_player);
        BoidFactory.Instance.AllBoids.Add(_player);

        _aiSpawningDirector.Init(this, _player);

        GD.Randomize();

        GameCamera.Instance.Init(_player);
        MusicPlayer.Instance.PlayGame();

        StateMachine_Game.Instance.SendInitialStateChange();

        GameCamera.OnPostCameraTransformed += OnPostCameraTransformed;

        GetViewport().Connect("size_changed", this, nameof(_OnWindowSizeChanged));
        _OnWindowSizeChanged();
        
        _outlineShader = _outlineMesh.GetActiveMaterial(0) as ShaderMaterial;
        _outlineShader.SetShaderParam("u_outlineBuffer", _outlineViewport.GetTexture());
        _outlineViewport.GetTexture().Flags = (uint) Texture.FlagsEnum.Default;
        
        DebugImGui.Instance.RegisterWindow("render_textures", "Render Textures", _OnImGuiLayoutRenderTextures);
        DebugImGui.Instance.RegisterWindow("render_settings", "Render Settings", _OnImGuiLayoutRenderSettings);
    }

    private void _OnWindowSizeChanged()
    {
        _outlineViewport.Size = OS.WindowSize * 1.0f;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        GameCamera.OnPostCameraTransformed -= OnPostCameraTransformed;
    }

    private void OnPostCameraTransformed()
    {
        // scale and position sand
        {
            Vector3 topLeft = GameCamera.Instance.ProjectToY(new Vector2(0.0f, 0.0f), _sand.GlobalTransform.origin.y);
            Vector3 bottomRight = GameCamera.Instance.ProjectToY(GetViewport().Size, _sand.GlobalTransform.origin.y);
            _sand.Scale = new Vector3(bottomRight.x - topLeft.x, 1.0f, bottomRight.z - topLeft.z);
            Vector3 pos = GameCamera.Instance.GlobalTransform.origin;
            pos.y = _sand.GlobalTransform.origin.y;
            _sand.GlobalPosition(pos);
        }

        // position clouds
        Vector3 cloudPos = GameCamera.Instance.GlobalTransform.origin;
        _clouds.GlobalPosition(new Vector3(cloudPos.x, _clouds.GlobalTransform.origin.y, cloudPos.z));
    }

    public void RegisterPickup(PickupMaterial pickup)
    {
        AddChild(pickup);
        Player.RegisterPickup(pickup);
    }

    private void _OnPlayerDestroyed(BoidBase player)
    {
        // TODO: game over
    }

    private void _OnImGuiLayoutRenderTextures()
    {
        ImGui.SliderFloat("Scale", ref _debugRenderSize, 0.1f, 10.0f);
        if (ImGui.BeginTabBar("buffers"))
        {
            if (ImGui.BeginTabItem("Outline Buffer"))
            {
                System.Numerics.Vector2 size = _outlineViewport.GetTexture().GetSize().ToNumerics();
                ImGui.Text($"Size: {size}");
                ImGui.Image(ImGuiGD.BindTexture(_outlineViewport.GetTexture()), size * _debugRenderSize);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }
    
    private void _OnImGuiLayoutRenderSettings()
    {
        if (ImGui.BeginTabBar("render_settings"))
        {
            if (ImGui.BeginTabItem("Outline"))
            {
                int width = (int)_outlineShader.GetShaderParam("u_width");
                ImGui.SliderInt("Width", ref width, 0, 10);
                _outlineShader.SetShaderParam("u_width", width);
                
                float aa = (float) _outlineShader.GetShaderParam("u_aa");
                ImGui.SliderFloat("Anti Aliasing", ref aa, 0.0f, 1.0f);
                _outlineShader.SetShaderParam("u_aa", aa);
                
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }
}