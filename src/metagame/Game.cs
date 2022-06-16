using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;
using ImGuiNET;

public partial class Game : Singleton<Game>
{
    [OnReadyGet] private StateMachine_Game _stateMachine;
    [OnReadyGet] private BoidPlayer _player;
    [OnReadyGet] private AISpawningDirector _aiSpawningDirector;
    [OnReadyGet] private HUD _hud;

    [Export] public Rect2 AreaRect;
    [Export] public float WaveCooldown = 5.0f;

    [Export] public float BaseBoidReload = 1.75f;
    [Export] public int BaseBoidReinforce = 3;
    [Export] public float BaseBoidGrouping = 10.0f;
    [Export] public float BaseBoidDamage = 1.0f;
    [Export] public float BaseBoidSpread = 0.1f;
    [Export] public float BaseBulletSpeed = 500.0f;
    [Export] public bool BaseMicroturrets;

    [Export] public float ScoreMultiTimeout = 10.0f;
    [Export] public int ScoreMultiMax = 10;
    [Export] public float ScoreMultiIncrement = 0.5f;
    
    private bool _initialSpawn;
    
    public static BoidPlayer Player => Instance._player;
    public Rect2 SpawningRect => new(Player.GlobalPosition - AreaRect.Size * 0.5f, AreaRect.Size);
    
    [OnReady] private void Ready()
    {
        base._Ready();
        
        SteeringManager.EdgeBounds = AreaRect;
        
        _player.Init("player", _OnPlayerDestroyed, Vector2.Zero, Vector2.Zero);
        
        _aiSpawningDirector.Init(this, _player);

        GD.Randomize();

        GameCamera.Instance.Init(_player);
        MusicPlayer.Instance.PlayGame();
        
        StateMachine_Game.Instance.SendInitialStateChange();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.DrawImGui += _OnImGuiLayout;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.DrawImGui -= _OnImGuiLayout;
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

    public void RegisterPickup(PickupMaterial pickup)
    {
        AddChild(pickup);
        Player.RegisterPickup(pickup);
    }

    private void _OnPlayerDestroyed(BoidBase player)
    {
        // TODO: game over
    }

    private void _OnImGuiLayout()
    {
        if (ImGui.BeginTabItem("Spawn"))
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

            ImGui.EndTabItem();
        }
    }
}