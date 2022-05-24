using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using ImGuiNET;
using Array = Godot.Collections.Array;
using Dictionary = Godot.Collections.Dictionary;

public class Game : Node
{
    public static Game Instance;

    public enum State
    {
        Play,
        Construct,
        Pause
    }
    
    public enum Formation
    {
        Balanced,
        Wide,
        Narrow,
    }

    [Export] private PackedScene _playerScene;
    private BoidPlayer _player;
    
    [Export] private NodePath _mouseCursorPath;
    private MeshInstance _mouseCursor;

    [Export] private NodePath _aiSpawningDirectorPath;
    private AISpawningDirector _aiSpawningDirector;

    [Export] private NodePath _hudPath;
    private HUD _hud;

    [Export] public float SpawningRadius = 500.0f;
    [Export] public float WaveCooldown = 5.0f;
    [Export] public int MaxDrones = 100;

    [Export] public float BaseBoidReload = 1.75f;
    [Export] public int BaseBoidReinforce = 3;
    [Export] public float BaseBoidGrouping = 10.0f;
    [Export] public float BaseBoidDamage = 1.0f;
    [Export] public float BaseSlowmoCD = 60.0f;
    [Export] public float BaseNukeCD = 120.0f;
    [Export] public float BaseBoidSpeed = 1500.0f;
    [Export] public float BasePlayerSpeed = 4.0f;
    [Export] public float BaseBoidSpread = 0.1f;
    [Export] public float BaseBulletSpeed = 500.0f;
    [Export] public bool BaseMicroturrets = false;

    [Export] public float ScoreMultiTimeout = 10.0f;
    [Export] public int ScoreMultiMax = 10;
    [Export] public float ScoreMultiIncrement = 0.5f;

    public Action<State, State> OnGameStateChanged;
    public State CurrentState => _state;
    public State PrevState => _prevState;

    private State _state;
    private State _prevState;
    
    private Formation _formation = Formation.Balanced;
    private int _score = 0;
    private float _scoreMulti = 1.0f;
    private float _scoreMultiTimer;

    private List<BoidBase> _allBoids = new List<BoidBase>();
    private List<BoidBase> _destroyedBoids = new List<BoidBase>();
    private List<BoidEnemyBase> _enemyBoids = new List<BoidEnemyBase>();
    private List<BoidAllyBase> _allyBoids = new List<BoidAllyBase>();
    private List<List<BoidBase>> _boidColumns = new List<List<BoidBase>>();
    private int _addedAllyCounter;
    private int _boidColCount;

    private bool _hasSlowmo = false;
    private bool _hasNuke = false;

    private float _prevSubwaveTime;
    private int _pendingBoidSpawn;

    public List<BoidBase> AllBoids => _allBoids;
    public List<BoidBase> DestroyedBoids => _destroyedBoids;
    public List<BoidAllyBase> AllyBoids => _allyBoids;
    public List<BoidEnemyBase> EnemyBoids => _enemyBoids;
    public int NumBoids => _allyBoids.Count;

    public Game()
    {
        Debug.Assert(Instance == null, "Attempting to create multiple Game instances!");
        Instance = this;
    }
    
    public override void _Ready()
    {
        base._Ready();
        
        _player = _playerScene.Instance<BoidPlayer>();
        AddChild(_player);
        _mouseCursor = GetNode<MeshInstance>(_mouseCursorPath);
        _aiSpawningDirector = GetNode<AISpawningDirector>(_aiSpawningDirectorPath);
        _hud = GetNode<HUD>(_hudPath);

        _player.Init("player", _player, this, null, _OnBoidDestroyed);
        
        _aiSpawningDirector.Init(this, _player);

        _pendingBoidSpawn = SaveDataPlayer.InitialAllyCount;

        //AddScore(0, _player.GlobalPosition, false);
        GD.Randomize();

        GameCamera.Instance.Init(_player);
        MusicPlayer.Instance.PlayGame();
        //PauseManager.Instance.Init(this);

        ChangeGameState(State.Play);
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
        _mouseCursor.GlobalTransform = new Transform(_mouseCursor.GlobalTransform.basis, GameCamera.Instance.MousePosition().To3D());

        while (_pendingBoidSpawn > 0 && _allyBoids.Count < SaveDataPlayer.MaxAllyCount)
        {
            AddAllyBoid(Database.AllyBoids.GetAllEntries<DataAllyBoid>()[0].Name);
            _pendingBoidSpawn--;
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

    public void ChangeFormation(Formation formation, bool setPos)
    {
        if (_allyBoids.Count == 0)
        {
            return;
        }

        switch (formation)
        {
            case (int) Formation.Balanced:
                SetColumns((int) (Mathf.Sqrt(_allyBoids.Count) + 0.5f), setPos);
                break;
            case Formation.Wide:
                SetColumns((int) (Mathf.Sqrt(_allyBoids.Count) + 0.5f) * 2, setPos);
                break;
            case Formation.Narrow:
                SetColumns((int) (Mathf.Sqrt(_allyBoids.Count + 0.5f) * 0.5f), setPos);
                break;
        }

        _formation = formation;
    }

    public void SetColumns(int numCols, bool setPos)
    {
        _boidColCount = Mathf.Clamp(numCols, 0, _allyBoids.Count);
        _boidColumns = new List<List<BoidBase>>();
        foreach (int i in GD.Range(0, _boidColCount))
        {
            _boidColumns.Add(new List<BoidBase>());
        }

        int perCol = _allyBoids.Count / _boidColCount;
        foreach (int i in GD.Range(0, _allyBoids.Count))
        {
            BoidBase boid = _allyBoids[i];
            int column = i / perCol;
            int colIdx = column;
            if (colIdx >= _boidColumns.Count)
            {
                colIdx = i % _boidColCount;
            }

            _boidColumns[colIdx].Add(boid);
            int columnIndex = _boidColumns[colIdx].IndexOf(boid);
            Vector2 offset = GetOffset(colIdx, columnIndex);
            boid.Offset = offset;

            if (setPos)
            {
                boid.GlobalPosition = _player.GlobalPosition + offset;
            }
        }
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
        boid.Init(droneData.Name, _player, this, _player, _OnBoidDestroyed);
        boid.GlobalPosition(_player.GlobalTransform.origin);
        
        ChangeFormation(_formation, false);

        return true;
    }

    private Vector2 GetOffset(int column, int columnIndex)
    {
        column -= (int) (_boidColumns.Count * 0.5f - _boidColumns.Count % 2 * 0.5f);
        int perCol = _allyBoids.Count / _boidColumns.Count;
        columnIndex -= (int) (perCol * 0.5f - perCol % 2 * 0.5f);
        Vector2 offset = new Vector2(column * BaseBoidGrouping, columnIndex * BaseBoidGrouping);
        offset += new Vector2(0.5f * ((_boidColumns.Count + 1) % 2), 0.5f * ((perCol + 1) % 2)) * BaseBoidGrouping;
        return offset;
    }

    public void AddEnemy(BoidEnemyBase enemy)
    {
        _enemyBoids.Add(enemy);
        _allBoids.Add(enemy);
        enemy.OnBoidDestroyed += _OnBoidDestroyed;
    }

    public void ChangeGameState(State state)
    {
        _prevState = _state;
        switch (state)
        {
            case State.Play:
                Engine.TimeScale = 1.0f;
                break;
            case State.Pause:
            case State.Construct:
                Engine.TimeScale = 0.0f;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        _state = state;
        
        OnGameStateChanged?.Invoke(state, _prevState);
    }
    
    public void PushBack(BoidBase boid)
    {
        foreach (int i in GD.Range(0, _boidColCount))
        {
            if (_boidColumns[i].Contains(boid))
            {
                _boidColumns[i].Remove(boid);
                _boidColumns[i].Insert(0, boid);
                foreach (int j in GD.Range(0, _boidColumns[i].Count))
                {
                    _boidColumns[i][j].Offset = GetOffset(_boidColCount - i - 1, _boidColumns[i].Count - j - 1);
                }

                break;
            }
        }
    }

    private void _OnBoidDestroyed(BoidBase boid)
    {
        switch (boid)
        {
            case BoidAllyBase @base:
            {
                _allyBoids.Remove(@base);
                ChangeFormation(_formation, false);
                _scoreMulti = Mathf.Max(1.0f, _scoreMulti * 0.5f);
                //_gui.SetScore(_score, _scoreMulti, _perks.GetNextThreshold(), _scoreMulti == ScoreMultiMax);
                break;
            }
            case BoidEnemyBase @base:
                _enemyBoids.Remove(@base);
                break;
        }
        _allBoids.Remove(boid);
        _destroyedBoids.Add(boid);
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
        if (ImGui.BeginTabItem("Director"))
        {
            _aiSpawningDirector._OnImGuiLayout();
            ImGui.EndTabItem();
        }
    }
}