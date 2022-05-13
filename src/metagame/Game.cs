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

    [Export] private NodePath _aiSpawningDirectorPath;
    private AISpawningDirector _aiSpawningDirector;

    [Export] private NodePath _cloudsPath;
    private Clouds _clouds;

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
    
    private Formation _formation = Formation.Balanced;
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

    private float _prevSubwaveTime;
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
        _aiSpawningDirector = GetNode<AISpawningDirector>(_aiSpawningDirectorPath);
        _clouds = GetNode<Clouds>(_cloudsPath);

        _player.Init("player", _player, this, null);
        DebugImGui.DrawImGui += _OnImGuiLayout;
        _aiSpawningDirector.Init(this, _player);

        _pendingBoidSpawn = SaveDataPlayer.Instance.InitialAllyCount;

        AddScore(0, _player.GlobalPosition, false);
        GD.Randomize();

        GlobalCamera.Instance.Init(_player);
        MusicPlayer.Instance.PlayGame();
        //PauseManager.Instance.Init(this);
    }
    
    public override void _Process(float delta)
    {
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
        DataAllyBoid droneData = Database.AllyBoids.FindEntry<DataAllyBoid>(droneId);
        BoidAllyBase boid = droneData.Scene.Instance<BoidAllyBase>();
        AddChild(boid);
        _allyBoids.Add(boid);
        _allBoids.Add(boid);
        boid.Init(droneData.Name, _player, this, _player);
        
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
        if (ImGui.BeginTabItem("Spawn"))
        {
            foreach (DataEnemyBoid boid in Database.EnemyBoids.GetAllEntries<DataEnemyBoid>())
            {
                if (ImGui.Button($"Spawn {boid.DisplayName}"))
                {
                    _aiSpawningDirector.SpawnEnemy(boid);
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