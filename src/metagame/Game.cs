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

    [Export] private Rect2 _areaRect;
    [Export] private PackedScene _playerScene;
    
    private bool _initialSpawn;
    private bool _fullScreen = false;
    private BoidPlayer _player;
    
    public static BoidPlayer Player => Instance._player;
    public Rect2 SpawningRect => new(Player.GlobalPosition - _areaRect.Size * 0.5f, _areaRect.Size);

    [OnReady]
    private void Ready()
    {
        base._Ready();

        SteeringManager.EdgeBounds = _areaRect;

        _player = _playerScene.Instance<BoidPlayer>();
        AddChild(_player);
        _player.Init("player", _OnPlayerDestroyed, Vector2.Zero, Vector2.Zero);
        BoidFactory.Instance.AllyBoids.Add(_player);
        BoidFactory.Instance.AllBoids.Add(_player);

        _aiSpawningDirector.Init(this, _player);

        GD.Randomize();

        GameCamera.Instance.Init(_player);
        MusicPlayer.Instance.PlayGame();

        StateMachine_Game.Instance.SendInitialStateChange();

        GameCamera.OnPostCameraTransformed += OnPostCameraTransformed;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.Instance.RegisterWindow("spawning", "Spawning", _OnImGuiLayoutWindow);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.Instance.UnRegisterWindow("spawning", _OnImGuiLayoutWindow);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        SteeringManager.EdgeBounds = SpawningRect;

        if (!_initialSpawn)
        {
            // BoidFactory.Instance.CreateEnemyBoid(Database.EnemyBoid("driller"), new Vector2(100.0f, 0.0f), new Vector2(-50.0f, 0.0f));
            // BoidFactory.Instance.CreateEnemyBoid(Database.EnemyBoid("driller"), new Vector2(-100.0f, 0.0f), new Vector2(50.0f, 0.0f));
            
            _initialSpawn = true;
        }
    }
    
    private void OnPostCameraTransformed()
    {
        Vector3 topLeft = GameCamera.Instance.ProjectToY(new Vector2(0.0f, 0.0f), -50.0f);
        Vector3 bottomRight = GameCamera.Instance.ProjectToY(GetViewport().Size, -50.0f);
        _sand.Scale = new Vector3(bottomRight.x - topLeft.x,1.0f, bottomRight.z - topLeft.z);
        Vector3 pos = GameCamera.Instance.GlobalTransform.origin;
        pos.y = _sand.GlobalTransform.origin.y;
        _sand.GlobalPosition(pos);
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

    private void _OnImGuiLayoutWindow()
    {
        ImGui.Text($"Boids: {BoidFactory.Instance.AllBoids.Count}");
        ImGui.Text(" ### Fabricants");
        foreach (DataAllyBoid boid in Database.AllyBoids.GetAllEntries<DataAllyBoid>())
        {
            if (ImGui.Button($"{boid.DisplayName}"))
            {
                BoidFactory.Instance.CreateAllyBoid(boid);
            }

            if (ImGui.Button($"{boid.DisplayName} x10"))
            {
                for (int i = 0; i < 10; i++)
                    BoidFactory.Instance.CreateAllyBoid(boid);
            }
        }

        ImGui.Text(" ### Enemies");
        foreach (DataEnemyBoid boid in Database.EnemyBoids.GetAllEntries<DataEnemyBoid>())
        {
            if (ImGui.Button($"{boid.DisplayName}"))
            {
                _aiSpawningDirector.SpawnEnemyRandom(boid);
            }

            if (ImGui.Button($"{boid.DisplayName} x10"))
            {
                for (int i = 0; i < 10; i++)
                    _aiSpawningDirector.SpawnEnemyRandom(boid);
            }
        }

        ImGui.Text(" ### Waves");
        foreach (DataWave wave in Database.Waves.GetAllEntries<DataWave>())
        {
            if (ImGui.Button($"{wave.Name}"))
            {
                _aiSpawningDirector.SpawnWave(wave, new List<BoidEnemyBase>(), new List<BoidEnemyBase>());
            }
        }
    }
}