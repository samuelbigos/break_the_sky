using System;
using System.Collections.Generic;
using Godot;
using ImGuiNET;
using Dictionary = Godot.Collections.Dictionary;

public class Game : Node2D
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

    [Export] public NodePath ImGuiNodePath;
    private ImGuiNode _imGuiNode;

    [Export] public int DebugWave = -1;

    [Export] private PackedScene BoidScene;
    [Export] PackedScene PickupAddScene;
    [Export] PackedScene EnemyDrillerScene;
    [Export] PackedScene EnemyLaserScene;
    [Export] PackedScene EnemyBeaconScene;
    [Export] PackedScene EnemyCarrierScene;

    [Export] public int InitialPickupAddCount = 20;
    [Export] public int InitialBoidCount = 100;
    [Export] public float PlayRadius = 1000.0f;
    [Export] public float WaveCooldown = 5.0f;
    [Export] public int MaxDrones = 100;

    [Export] public float BaseBoidReload = 1.75f;
    [Export] public int BaseBoidReinforce = 3;
    [Export] public float BaseBoidGrouping = 25.0f;
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
    public int _boidColCount;
    private List<List<BoidBase>> _boidColumns = new List<List<BoidBase>>();
    private List<BoidBase> _allBoids = new List<BoidBase>();
    private Leader _player = null;
    public Formation _formation = Formation.Balanced;
    private List<Node2D> _pickups = new List<Node2D>();
    private int _spawnPickups = 0;
    private bool _started = false;
    private int _score = 0;
    private float _scoreMulti = 1.0f;
    public float _scoreMultiTimer;
    public float _loseTimer;
    private bool _pendingLose = false;
    public List<BoidBase> Enemies { get; } = new List<BoidBase>();
    public int _numBoids = 0;

    private bool _hasSlowmo = false;
    private bool _hasNuke = false;

    private WaveState _waveState = WaveState.Cooldown;
    public float _waveTimer = 0.0f;
    public int _currentWave = -1;
    public int _currentSubWave = 0;
    public int _numWaves = 0;
    public float _prevSubwaveTime;
    private float _fps = 0.0f;

    private int _pendingBoidSpawn;

    public Node _Hud;
    public PerkManager _perks;

    public List<BoidBase> GetBoids()
    {
        return _allBoids;
    }

    public Node2D GetPlayer()
    {
        return _player;
    }

    public int GetNumBoids()
    {
        return _numBoids;
    }

    public List<BoidBase> GetEnemies()
    {
        return Enemies;
    }

    public override void _Ready()
    {
        _Hud = GetNode("Hud");
        _perks = GetNode("PerkManager") as PerkManager;

        _player = GetNode("Leader") as Leader;
        _player.Init(_player, this, null);

        foreach (int i in GD.Range(0, InitialBoidCount))
        {
            BoidBase boid = BoidScene.Instance() as BoidBase;
            AddChild(boid);
            _allBoids.Add(boid);
            boid.Init(_player, this, _player);
        }

        if (_allBoids.Count > 0)
        {
            ChangeFormation(Formation.Balanced, true);

            // spawn pickups
        }

        foreach (int i in GD.Range(0, InitialPickupAddCount))
        {
            float f = i * Mathf.Pi * 2.0f / InitialPickupAddCount;
            SpawnPickupAdd(new Vector2(Mathf.Sin(f), -Mathf.Cos(f)).Normalized() * 80.0f, true);
        }

        _levels = Levels.Instance.Waves;
        if (DebugWave >= 0)
        {
            _levels = Levels.Instance.Waves;
            _currentWave = DebugWave - 1;
            _started = true;
            BaseBoidDamage = 5.0f;
            BaseBoidReload = 1.0f;
            BaseBoidSpeed = 1000.0f;
            BasePlayerSpeed = 10.0f;
            BaseMicroturrets = true;
            foreach (Node2D pickup in _pickups)
            {
                pickup.QueueFree();
                AddBoids(new Vector2(0.0f, 0.0f));
            }
        }

        AddScore(0, _player.GlobalPosition, false);
        GD.Randomize();

        _numWaves = 1; //_levels[0]["waves"].Size();

        GlobalCamera.Instance.Init(_player);
        PauseManager.Instance.Init(this);

        MusicPlayer.Instance.PlayGame();

        //_score += 10000;
        //addScore(10000, new Vector2(0.0, 0.0), false)
        //lose()

        _imGuiNode = GetNode<ImGuiNode>(ImGuiNodePath);
        _imGuiNode.Connect("IGLayout", this, nameof(_OnImGuiLayout));
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
                    _Spawn(0);
                }
                if (ImGui.Button("Spawn 2"))
                {
                    _Spawn(1);
                }
                if (ImGui.Button("Spawn 3"))
                {
                    _Spawn(2);
                }
                if (ImGui.Button("Spawn 4"))
                {
                    _Spawn(3);
                }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
            ImGui.End();
        }
    }

    public void ChangeFormation(Formation formation, bool setPos)
    {
        if (_allBoids.Count == 0)
        {
            return;
        }

        if (formation == (int) Formation.Balanced)
        {
            SetColumns((int) (Mathf.Sqrt(_allBoids.Count) + 0.5f), setPos);
        }

        if (formation == Formation.Wide)
        {
            SetColumns((int) (Mathf.Sqrt(_allBoids.Count) + 0.5f) * 2, setPos);
        }

        if (formation == Formation.Narrow)
        {
            SetColumns((int) (Mathf.Sqrt(_allBoids.Count + 0.5f) * 0.5f), setPos);
        }

        _formation = formation;
    }

    public void SetColumns(int numCols, bool setPos)
    {
        _boidColCount = Mathf.Clamp(numCols, 0, _allBoids.Count);
        _boidColumns = new List<List<BoidBase>>();
        foreach (int i in GD.Range(0, _boidColCount))
        {
            _boidColumns.Add(new List<BoidBase>());
        }

        int perCol = _allBoids.Count / _boidColCount;
        foreach (int i in GD.Range(0, _allBoids.Count))
        {
            BoidBase boid = _allBoids[i];
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

    private void AddBoidsInternal()
    {
        bool addedBoid = false;
        foreach (int i in GD.Range(0, BaseBoidReinforce))
        {
            if (_allBoids.Count > MaxDrones)
            {
                break;
            }

            BoidBase boid = BoidScene.Instance() as BoidBase;
            AddChild(boid);
            _allBoids.Add(boid);
            boid.Init(_player, this, _player);
            boid.GlobalPosition = _player.GlobalPosition;
            addedBoid = true;
        }

        InitialPickupAddCount -= 1;
        if (InitialPickupAddCount == 0)
        {
            Start();
        }

        if (addedBoid)
        {
            ChangeFormation(_formation, false);
            _numBoids = _allBoids.Count;
        }
    }

    public void RemoveBoid(BoidBase boid)
    {
        _allBoids.Remove(boid);
        if (_allBoids.Count == 0)
        {
            _loseTimer = 2.0f;
            _pendingLose = true;
            _player.Destroy();
        }

        ChangeFormation(_formation, false);
        _numBoids = _allBoids.Count;
        _scoreMulti = Mathf.Max(1.0f, _scoreMulti * 0.5f);
        //_gui.SetScore(_score, _scoreMulti, _perks.GetNextThreshold(), _scoreMulti == ScoreMultiMax);
    }

    public void SpawnPickupAdd(Vector2 pos, bool persistent)
    {
        PickupAdd pickup = PickupAddScene.Instance() as PickupAdd;
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
        int perCol = (int) (_allBoids.Count / _boidColumns.Count);
        columnIndex -= (int) (perCol * 0.5f - perCol % 2 * 0.5f);
        Vector2 offset = new Vector2(column * BaseBoidGrouping, columnIndex * BaseBoidGrouping);
        offset += new Vector2(0.5f * ((_boidColumns.Count + 1) % 2), 0.5f * ((perCol + 1) % 2)) * BaseBoidGrouping;
        return offset;
    }

    public override void _Process(float delta)
    {
        float fps = 1.0f / delta;
        _fps = 0.033f * fps + 0.966f * _fps;
        
        while (_pendingBoidSpawn > 0)
        {
            AddBoidsInternal();
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

        foreach (int i in GD.Range(Enemies.Count, 0, -1))
        {
            if (!IsInstanceValid(Enemies[i - 1]))
            {
                Enemies.RemoveAt(i - 1);

                // waves
            }
        }

        int enemyCount = Enemies.Count;
        _waveTimer -= delta;
        if (_started)
        {
            switch (_waveState)
            {
                case WaveState.Cooldown:
                    if (_waveTimer < 0.0f && enemyCount == 0)
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
                            _Spawn(t);
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

    public void _Spawn(int id)
    {
        BoidBase enemy = null;
        switch (id)
        {
            case 0:
                enemy = EnemyDrillerScene.Instance() as BoidBase;
                break;
            case 1:
                enemy = EnemyLaserScene.Instance() as BoidBase;
                break;
            case 2:
                enemy = EnemyBeaconScene.Instance() as BoidBase;
                break;
            case 3:
                enemy = EnemyCarrierScene.Instance() as BoidBase;
                break;
        }

        float f = (float) GD.RandRange(0.0f, Mathf.Pi * 2.0f);
        enemy.GlobalPosition = new Vector2(Mathf.Sin(f), -Mathf.Cos(f)).Normalized() * PlayRadius;
        //enemy.global_position = new Vector2(100.0, 0.0);
        enemy.Init(_player, this, _player);
        AddChild(enemy);
        Enemies.Add(enemy);
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
        _perks.PickPerk(perk);
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
                    _boidColumns[i][j].SetOffset(GetOffset(i, j));
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
}