using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;
using ImGuiNET;
using Array = Godot.Collections.Array;
using Dictionary = Godot.Collections.Dictionary;

public partial class Game : Singleton<Game>
{
    [OnReadyGet] private StateMachine_Game _stateMachine;
    [OnReadyGet] private BoidPlayer _player;
    [OnReadyGet] private AISpawningDirector _aiSpawningDirector;
    [OnReadyGet] private HUD _hud;

    [Export] public Rect2 AreaRect;
    [Export] public float WaveCooldown = 5.0f;
    [Export] public int MaxDrones = 100;

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
    
    private int _score = 0;
    private float _scoreMulti = 1.0f;
    private float _scoreMultiTimer;

    private List<BoidBase> _allBoids = new();
    private List<BoidBase> _destroyedBoids = new();
    private List<BoidEnemyBase> _enemyBoids = new();
    private List<BoidAllyBase> _allyBoids = new();
    private int _addedAllyCounter;
    private bool _initialSpawn;

    public List<BoidBase> AllBoids => _allBoids;
    public List<BoidBase> DestroyedBoids => _destroyedBoids;
    public List<BoidAllyBase> AllyBoids => _allyBoids;
    public List<BoidEnemyBase> EnemyBoids => _enemyBoids;
    public int NumBoids => _allyBoids.Count;
    public BoidPlayer Player => _player;
    public Rect2 SpawningRect => new(Player.GlobalPosition - AreaRect.Size * 0.5f, AreaRect.Size);
    
    [OnReady] private void Ready()
    {
        base._Ready();
        
        _player.Init("player", _player, this, _OnBoidDestroyed);
        
        _aiSpawningDirector.Init(this, _player);

        //AddScore(0, _player.GlobalPosition, false);
        GD.Randomize();

        GameCamera.Instance.Init(_player);
        MusicPlayer.Instance.PlayGame();
        //PauseManager.Instance.Init(this);
        
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

        if (!_initialSpawn)
        {
            // for (int i = 0; i < 50; i++)
            // {
            //     BoidEnemyBase boid = _aiSpawningDirector.SpawnEnemyRandom(Database.EnemyBoid("driller"));
            // }
            
            _aiSpawningDirector.SpawnEnemy(Database.EnemyBoid("driller"), new Vector2(100.0f, 0.0f), new Vector2(-50.0f, 0.0f));
            _aiSpawningDirector.SpawnEnemy(Database.EnemyBoid("driller"), new Vector2(-100.0f, 0.0f), new Vector2(50.0f, 0.0f));
            
            _initialSpawn = true;
        }
        
        _scoreMultiTimer -= delta;
        if (_scoreMultiTimer < 0.0f)
        {
            _scoreMulti = 1.0f;
        }

        for (int i = _allBoids.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_allBoids[i]))
            {
                if (_allBoids[i] is BoidAllyBase)
                    _allyBoids.Remove(_allBoids[i] as BoidAllyBase);
                
                if (_allBoids[i] is BoidEnemyBase)
                    _enemyBoids.Remove(_allBoids[i] as BoidEnemyBase);
                
                _allBoids.Remove(_allBoids[i]);
            }
        }
        
        Debug.Assert(_allyBoids.Count + _enemyBoids.Count == _allBoids.Count, "Error in boid references.");
    }

    public void FreeBoid(BoidBase boid)
    {
        _destroyedBoids.Remove(boid);
        boid.QueueFree();
    }

    public bool AddAllyBoid(string boidId)
    {
        if (_allyBoids.Count >= SaveDataPlayer.MaxAllyCount)
            return false;
            
        DataAllyBoid droneData = Database.AllyBoids.FindEntry<DataAllyBoid>(boidId);
        BoidAllyBase boid = droneData.Scene.Instance<BoidAllyBase>();
        AddChild(boid);
        _allyBoids.Add(boid);
        _allBoids.Add(boid);
        boid.Init(droneData.Name, _player, this, _OnBoidDestroyed);
        boid.SetTarget(BoidBase.TargetType.Ally, _player);
        boid.GlobalPosition(_player.GlobalTransform.origin);
        //SteeringManager.Instance.AddBoid(boid, Vector2.Zero);

        return true;
    }

    public void RegisterEnemyBoid(BoidEnemyBase enemy)
    {
        _enemyBoids.Add(enemy);
        _allBoids.Add(enemy);
        enemy.OnBoidDestroyed += _OnBoidDestroyed;
    }

    private void _OnBoidDestroyed(BoidBase boid)
    {
        switch (boid)
        {
            case BoidAllyBase @base:
            {
                _allyBoids.Remove(@base);
                _scoreMulti = Mathf.Max(1.0f, _scoreMulti * 0.5f);
                break;
            }
            case BoidEnemyBase @base:
                _enemyBoids.Remove(@base);
                _aiSpawningDirector.OnEnemyDestroyed(@base);
                break;
        }
        _allBoids.Remove(boid);
        _destroyedBoids.Add(boid);
        
        //SteeringManager.Instance.RemoveBoid(boid);
    }
    
    private void _OnImGuiLayout()
    {
        if (ImGui.BeginTabItem("Spawn Fabricants"))
        {
            ImGui.Text("Enemies");
            foreach (DataAllyBoid boid in Database.AllyBoids.GetAllEntries<DataAllyBoid>())
            {
                if (ImGui.Button($"{boid.DisplayName}"))
                {
                    AddAllyBoid(boid.Name);
                }
            }
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Spawn Enemies"))
        {
            ImGui.Text("Enemies");
            foreach (DataEnemyBoid boid in Database.EnemyBoids.GetAllEntries<DataEnemyBoid>())
            {
                if (ImGui.Button($"{boid.DisplayName}"))
                {
                    _aiSpawningDirector.SpawnEnemyRandom(boid);
                }
            }
            ImGui.Text("Waves");
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