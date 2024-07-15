using Godot;
using ImGuiNET;

public partial class Game : Singleton<Game>
{
    [Export] public StateMachine_Game _stateMachine { get; set; }
    [Export] public AISpawningDirector _aiSpawningDirector { get; set; }
    [Export] private HUD _hud;
    [Export] private SubViewport _outlineViewport;
    [Export] private MeshInstance3D _outlineMesh;
    [Export] private ShaderMaterial _outlineShader;
    [Export] private DirectionalLight3D _directionalLight;
    [Export] private StaticBody3D _ground;

    [Export] private Rect2 _areaRect;
    [Export] private ResourceBoidAlly _playerData;

    private BoidAllyBase _player;

    public static BoidAllyBase Player => Instance._player;
    public Rect2 SpawningRect => new(Player.GlobalPosition - _areaRect.Size * 0.5f, _areaRect.Size);

    private float _debugRenderSize = 1.0f;

    public override void _Ready()
    {
        ResourceBoidAlly playerBoidRes = FabricateManager.Instance.GetBoidResourceByUID(SaveDataPlayer.InitialPlayerBoid);
        _player = playerBoidRes.Scene.Instantiate<BoidAllyBase>();
        _player.SetIsPlayer(true);
        
        AddChild(_player);
        _player.Init(_playerData, _OnPlayerDestroyed, Vector2.Zero, Vector2.Zero);
        BoidFactory.Instance.AllyBoids.Add(_player);
        BoidFactory.Instance.AllBoids.Add(_player);

        _aiSpawningDirector.Init(this);

        GD.Randomize();

        GameCamera.Instance.Init();
        MusicPlayer.Instance.PlayGame();

        StateMachine_Game.Instance.SendInitialStateChange();

        GameCamera.OnPostCameraTransformed += OnPostCameraTransformed;

        GetViewport().Connect("size_changed", new Callable(this, nameof(_OnWindowSizeChanged)));
        _OnWindowSizeChanged();
        
        _outlineShader.SetShaderParameter("u_outlineBuffer", _outlineViewport.GetTexture());
        _outlineMesh.MaterialOverride = _outlineShader;
        //_outlineViewport.GetTexture().Flags = (uint) Texture2D.FlagsEnum.Default;
        
        DebugImGui.Instance.RegisterWindow("render_textures", "Render Textures", _OnImGuiLayoutRenderTextures);
        DebugImGui.Instance.RegisterWindow("render_settings", "Render Settings", _OnImGuiLayoutRenderSettings);

        OnPostCameraTransformed();
    }

    private void _OnWindowSizeChanged()
    {
        _outlineViewport.Size = DisplayServer.WindowGetSize();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        GameCamera.OnPostCameraTransformed -= OnPostCameraTransformed;
    }

    private void OnPostCameraTransformed()
    {
        // Keep the shadow max distance at the distance between camera and ground, to maximise shadow resolution.
        _directionalLight.DirectionalShadowMaxDistance = GameCamera.Instance.GlobalTransform.Origin.Y - _ground.GlobalTransform.Origin.Y;
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
                //ImGui.Image(ImGuiGD.BindTexture(_outlineViewport.GetTexture()), size * _debugRenderSize);
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
                int width = (int)_outlineShader.GetShaderParameter("u_width");
                ImGui.SliderInt("Width", ref width, 0, 10);
                _outlineShader.SetShaderParameter("u_width", width);
                
                float aa = (float) _outlineShader.GetShaderParameter("u_aa");
                ImGui.SliderFloat("Anti Aliasing", ref aa, 0.0f, 1.0f);
                _outlineShader.SetShaderParameter("u_aa", aa);
                
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }
}