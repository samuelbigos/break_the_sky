using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;
using ImGuiNET;
using Dictionary = Godot.Collections.Dictionary;

public class Game : Node
{
    public enum Formation
    {
        Balanced,
        Wide,
        Narrow,
    }

    enum WaveState
    {
        Wave,
        Cooldown,
        Finished
    }

    [Export] private NodePath _playerPath;
    private Player _player;
    
    [Export] private NodePath _mouseCursorPath;
    private MeshInstance _mouseCursor;
    
    [Export] private NodePath _waterPath;
    private MeshInstance _water;

    [Export] private PackedScene _pickupAddScene;
    [Export] private PackedScene _enemyDrillerScene;
    [Export] private PackedScene _enemyLaserScene;
    [Export] private PackedScene _enemyBeaconScene;
    [Export] private PackedScene _enemyCarrierScene;

    [Export] public int InitialPickupAddCount = 20;
    [Export] public float PlayRadius = 1000.0f;
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

    private List<Levels.Wave> _levels;
    
    
    private Formation _formation = Formation.Balanced;
    private List<PickupAdd> _pickups = new List<PickupAdd>();
    private int _spawnPickups = 0;
    private bool _started = false;
    private int _score = 0;
    private float _scoreMulti = 1.0f;
    private float _scoreMultiTimer;
    private float _loseTimer;
    private bool _pendingLose = false;

    private List<BoidBase> _allBoids = new List<BoidBase>();
    private List<BoidEnemyBase> _enemyBoids = new List<BoidEnemyBase>();
    private List<BoidAllyBase> _allyBoids = new List<BoidAllyBase>();
    private List<List<BoidBase>> _boidColumns = new List<List<BoidBase>>();
    private int _addedAllyCounter;
    private int _boidColCount;

    private bool _hasSlowmo = false;
    private bool _hasNuke = false;

    private WaveState _waveState = WaveState.Cooldown;
    private float _waveTimer = 0.0f;
    private int _currentWave = -1;
    private int _currentSubWave = 0;
    private int _numWaves = 0;
    private float _prevSubwaveTime;
    private float _fps = 0.0f;
    private int _pendingBoidSpawn;

    public List<BoidBase> AllBoids => _allBoids;
    public List<BoidAllyBase> AllyBoids => _allyBoids;
    public List<BoidEnemyBase> EnemyBoids => _enemyBoids;
    public Player Player => _player;
    public int NumBoids => _allyBoids.Count;

    public override void _Ready()
    {
        _player = GetNode<Player>(_playerPath);
        _mouseCursor = GetNode<MeshInstance>(_mouseCursorPath);
        _water = GetNode<MeshInstance>(_waterPath);

        _player.Init(_player, this, null);
        DebugImGui.DebugImGuiNode.Connect("IGLayout", this, nameof(_OnImGuiLayout));

        _pendingBoidSpawn = SaveDataPlayer.Instance.InitialAllyCount;
            
        // foreach (int i in GD.Range(0, InitialPickupAddCount))
        // {
        //     float f = i * Mathf.Pi * 2.0f / InitialPickupAddCount;
        //     SpawnPickupAdd(new Vector2(Mathf.Sin(f), -Mathf.Cos(f)).Normalized() * 80.0f, true);
        // }

        _levels = Levels.Instance.Waves;
        // if (DebugWave >= 0)
        // {
        //     _levels = Levels.Instance.Waves;
        //     _currentWave = DebugWave - 1;
        //     _started = true;
        //     BaseBoidDamage = 5.0f;
        //     BaseBoidReload = 1.0f;
        //     BaseBoidSpeed = 1000.0f;
        //     BasePlayerSpeed = 10.0f;
        //     BaseMicroturrets = true;
        //     foreach (PickupAdd pickup in _pickups)
        //     {
        //         pickup.QueueFree();
        //         AddBoids(new Vector2(0.0f, 0.0f));
        //     }
        // }

        AddScore(0, _player.GlobalPosition, false);
        GD.Randomize();

        _numWaves = 1; //_levels[0]["waves"].Size();

        GlobalCamera.Instance.Init(_player);
        //PauseManager.Instance.Init(this);

        MusicPlayer.Instance.PlayGame();

        //_score += 10000;
        //addScore(10000, new Vector2(0.0, 0.0), false)
        //lose()
        
        //(_water.GetActiveMaterial(0) as ShaderMaterial).SetShaderParam("u_water_col", ColourManager.Instance.Primary);
    }
    
    public override void _Process(float delta)
    {
        float fps = 1.0f / delta;
        _fps = 0.033f * fps + 0.966f * _fps;

        _mouseCursor.GlobalTransform = new Transform(_mouseCursor.GlobalTransform.basis, GlobalCamera.Instance.MousePosition().To3D());
        
        while (_pendingBoidSpawn > 0 && _allyBoids.Count < SaveDataPlayer.Instance.MaxAllyCount)
        {
            AddAllyBoidsInternal();
            _pendingBoidSpawn--;
        }
        
        if (_pendingLose)
        {
            _loseTimer -= delta;
            if (_loseTimer < 0.0f)
            {
                Lose();
            }

            return;
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
        
        _waveTimer -= delta;
        if (_started)
        {
            switch (_waveState)
            {
                case WaveState.Cooldown:
                    if (_waveTimer < 0.0f && _enemyBoids.Count == 0)
                    {
                        EnterWave();
                    }

                    break;
                case WaveState.Wave:
                    if (_waveTimer < 0.0f)
                    {
                        //_gui.SetWave(_currentWave, _currentSubWave)
                        List<int> spawns = GetNextSubwaveSpawn();
                        if (spawns.Count == 0)
                            _waveState = WaveState.Finished;
                        
                        foreach (int t in spawns)
                        {
                            SpawnEnemy(t);
                        }

                        _currentSubWave += 1;
                        int subwaveCount = _levels[0].SpawnSets.Count;
                        if (_currentSubWave >= subwaveCount)
                        {
                            EnterWaveCooldown();
                        }
                        else
                        {
                            float subwaveTime = GetNextSubwaveTime();
                            _waveTimer = subwaveTime - _prevSubwaveTime;
                            _prevSubwaveTime = subwaveTime;
                        }
                    }
                    break;
                case WaveState.Finished:
                    _started = false;
                    break;
            }
        }
    }

    public void ChangeFormation(Formation formation, bool setPos)
    {
        if (_allyBoids.Count == 0)
        {
            return;
        }

        if (formation == (int) Formation.Balanced)
        {
            SetColumns((int) (Mathf.Sqrt(_allyBoids.Count) + 0.5f), setPos);
        }

        if (formation == Formation.Wide)
        {
            SetColumns((int) (Mathf.Sqrt(_allyBoids.Count) + 0.5f) * 2, setPos);
        }

        if (formation == Formation.Narrow)
        {
            SetColumns((int) (Mathf.Sqrt(_allyBoids.Count + 0.5f) * 0.5f), setPos);
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
            boid.SetOffset(offset);

            if (setPos)
            {
                boid.GlobalPosition = _player.GlobalPosition + offset;
            }
        }
    }

    public void AddBoids(Vector2 pos)
    {
        _pendingBoidSpawn++;
    }

    private void AddAllyBoidsInternal()
    {
        Array activeAllyTypes = SaveDataPlayer.Instance.ActiveDrones;
        
        // spawn ally using _addedAllyCounter to determine type
        string droneId = activeAllyTypes[_addedAllyCounter++ % activeAllyTypes.Count] as string;
        DataAllyBoid droneData = Database.AllyDrones.FindEntry<DataAllyBoid>(droneId);
        BoidAllyBase boid = droneData.Scene.Instance<BoidAllyBase>();
        AddChild(boid);
        _allyBoids.Add(boid);
        _allBoids.Add(boid);
        boid.Init(_player, this, _player);
        
        ChangeFormation(_formation, false);
    }

    public void RemoveBoid(BoidBase boid)
    {
        switch (boid)
        {
            case BoidAllyBase @base:
            {
                _allyBoids.Remove(@base);
            
                ChangeFormation(_formation, false);
                _scoreMulti = Mathf.Max(1.0f, _scoreMulti * 0.5f);
                //_gui.SetScore(_score, _scoreMulti, _perks.GetNextThreshold(), _scoreMulti == ScoreMultiMax);
            
                if (_allyBoids.Count == 0)
                {
                    _loseTimer = 2.0f;
                    _pendingLose = true;
                    //_player.QueueFree();
                }
                break;
            }
            case BoidEnemyBase @base:
                _enemyBoids.Remove(@base);
                break;
        }
        _allBoids.Remove(boid);
    }

    public void SpawnPickupAdd(Vector2 pos, bool persistent)
    {
        PickupAdd pickup = _pickupAddScene.Instance() as PickupAdd;
        pickup.GlobalPosition = pos;
        pickup.Init(_player);
        if (persistent)
        {
            pickup.Lifetime = 9999999.0f;
        }

        AddChild(pickup);
        _pickups.Add(pickup);
    }

    public void Start()
    {
        _started = true;
    }

    private Vector2 GetOffset(int column, int columnIndex)
    {
        column -= (int) (_boidColumns.Count * 0.5f - _boidColumns.Count % 2 * 0.5f);
        int perCol = (int) (_allyBoids.Count / _boidColumns.Count);
        columnIndex -= (int) (perCol * 0.5f - perCol % 2 * 0.5f);
        Vector2 offset = new Vector2(column * BaseBoidGrouping, columnIndex * BaseBoidGrouping);
        offset += new Vector2(0.5f * ((_boidColumns.Count + 1) % 2), 0.5f * ((perCol + 1) % 2)) * BaseBoidGrouping;
        return offset;
    }

    public void EnterWaveCooldown()
    {
        _waveTimer = WaveCooldown;
        _waveState = WaveState.Cooldown;
    }

    public void EnterWave()
    {
        _waveState = WaveState.Wave;
        if (_currentWave != -1)
        {
            DoPerk();
        }
        //_gui.SetScore(_score, _scoreMulti, _perks.GetNextThreshold(), _scoreMulti == ScoreMultiMax);

        _currentSubWave = 0;
        _currentWave += 1;
        if (_currentWave >= _numWaves)
        {
            GenerateNewWave();
        }

        _prevSubwaveTime = 0.0f;
        _waveTimer = GetNextSubwaveTime();
    }

    private float GetNextSubwaveTime()
    {
        if (_currentWave >= _levels.Count)
            return 0.0f;
        
        if (_currentSubWave >= _levels[_currentWave].SpawnSets.Count)
            return 0.0f;
        
        return _levels[_currentWave].SpawnSets[_currentSubWave].Time;
    }

    private List<int> GetNextSubwaveSpawn()
    {
        if (_currentWave >= _levels.Count)
            return new List<int>();
        
        if (_currentSubWave >= _levels[_currentWave].SpawnSets.Count)
            return new List<int>();
        
        return _levels[_currentWave].SpawnSets[_currentSubWave].Spawns;
    }

    private void GenerateNewWave()
    {
        GD.Print("Implement me.");
        
        // List<Dictionary> wave = new List<Dictionary>() { };
        // // generate a new wave by combining two previous waves together
        // int metaWave = _currentWave / _numWaves;
        // long waveId1 = GD.Randi() % _numWaves;
        // long waveId2 = _numWaves - waveId1 - 1;
        // if (metaWave > 1)
        // {
        //     waveId2 = GD.Randi() % _numWaves;
        // }
        //
        // var timeMod = Mathf.Pow(0.75f, Mathf.Max(0, _currentWave / _numWaves - 1));
        // foreach (var subWave in _levels[0]["waves"][waveId1])
        // {
        //     Dictionary newSubWave = new Dictionary()
        //     {
        //         {"time", subWave["time"] * timeMod},
        //         {
        //             "spawns", subWave["spawns"].Duplicate()
        //         }
        //     };
        //     wave.Add(newSubWave);
        // }
        //
        // foreach (var subWave in _levels[0]["waves"][waveId2])
        // {
        //     Dictionary newSubWave = new Dictionary()
        //     {
        //         {"time", subWave["time"] * timeMod},
        //         {
        //             "spawns", subWave["spawns"].Duplicate()
        //         }
        //     };
        //     wave.Add(newSubWave);
        // }
        //
        // wave.Sort(MyCustomSorter, "sort_ascending");
        // _levels[0]["waves"].Add(wave);
    }

    private int MyCustomSorter(Dictionary x, Dictionary y)
    {
        if ((float) x["time"] < (float) y["time"])
        {
            return 1;
        }
        return 0;
    }

    public void AddEnemy(BoidEnemyBase enemy)
    {
        _enemyBoids.Add(enemy);
        _allBoids.Add(enemy);
    }

    private void SpawnEnemy(int id)
    {
        BoidEnemyBase enemy = null;
        switch (id)
        {
            case 0:
                enemy = _enemyDrillerScene.Instance() as BoidEnemyBase;
                break;
            case 1:
                enemy = _enemyLaserScene.Instance() as BoidEnemyBase;
                break;
            case 2:
                enemy = _enemyBeaconScene.Instance() as BoidEnemyBase;
                break;
            case 3:
                enemy = _enemyCarrierScene.Instance() as BoidEnemyBase;
                break;
        }

        float f = (float) GD.RandRange(0.0f, Mathf.Pi * 2.0f);
        enemy.GlobalPosition = new Vector2(Mathf.Sin(f), -Mathf.Cos(f)).Normalized() * PlayRadius;
        //enemy.global_position = new Vector2(100.0, 0.0);
        enemy.Init(_player, this, _player);
        AddChild(enemy);
        AddEnemy(enemy);
    }

    public void AddScore(int score, Vector2 pos, bool show)
    {
        // _gui.SetScore(_score, _scoreMulti, _perks.GetNextThreshold(), _scoreMulti == ScoreMultiMax);
        // if(show)
        // {
        // 	_gui.ShowFloatingScore(score * _scoreMulti, pos, this);
        // 	_score += score * _scoreMulti;
        // 	_scoreMulti = Mathf.Clamp(_scoreMulti + ScoreMultiIncrement, 0, ScoreMultiMax);
        // 	_scoreMultiTimer = ScoreMultiTimeout;
        //
        // }
    }

    public void DoPerk()
    {
        GetTree().Paused = true;
        // _gui.ShowPerks(_perks.GetRandomPerks(3));
        // if(!_gui.IsConnected("onPerkSelected", this, "onPerkSelected"))
        // {
        // 	_gui.Connect("onPerkSelected", this, "onPerkSelected");
        // }
    }

    public void OnPerkSelected(Perk perk)
    {
        PerkManager.Instance.PickPerk(perk);
        GetTree().Paused = false;
        BaseBoidReload *= perk.reloadMod;
        BaseBoidReinforce += perk.reinforceMod;
        BaseBoidGrouping += perk.groupingMod;
        BaseBoidDamage += perk.damageMod;
        BaseSlowmoCD *= perk.slowmoMod;
        BaseNukeCD *= perk.nukeMod;
        BaseBoidSpeed += perk.boidSpeedMod;
        BasePlayerSpeed += perk.playerSpeedMod;
        BaseBoidSpread *= perk.spreadMod;
        BaseBulletSpeed += perk.bulletSpeedMod;
        if (perk.microturrets)
        {
            BaseMicroturrets = true;
        }

        ChangeFormation(Formation.Balanced, false);
        //_gui.SetWave(_currentWave, _currentSubWave);
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
                    _boidColumns[i][j].SetOffset(GetOffset(_boidColCount - i - 1, _boidColumns[i].Count - j - 1));
                }

                break;
            }
        }
    }

    public void Lose()
    {
        GetTree().Paused = true;
        //_gui.ShowLoseScreen();
    }
    
    private void _OnImGuiLayout()
    {
        if (ImGui.Begin("Debug Menu", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"FPS: {_fps:F0}");
            ImGui.BeginTabBar("Debug Menu#left_tabs_bar");
            if (ImGui.BeginTabItem("Spawning"))
            {
                if (ImGui.Button("Spawn 1"))
                {
                    SpawnEnemy(0);
                }
                if (ImGui.Button("Spawn 2"))
                {
                    SpawnEnemy(1);
                }
                if (ImGui.Button("Spawn 3"))
                {
                    SpawnEnemy(2);
                }
                if (ImGui.Button("Spawn 4"))
                {
                    SpawnEnemy(3);
                }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
            ImGui.End();
        }
    }
}