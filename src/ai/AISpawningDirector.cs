using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;

public class AISpawningDirector : Node
{
    [Export] private bool _enabled = true;
    [Export] private float _intensityWavelength = 0.036f;
    [Export] private float _intensityWavelengthScaling = 0.0f;
    [Export] private float _intensityAmplitude = 0.206f;
    [Export] private float _intensityAmplitudeScaling = 0.1f;
    [Export] private float _intensityOffset = 0.124f;
    [Export] private float _intensityOffsetScale = 0.002f;

    [Export] private float _baseBudget = 10;
    [Export] private float _levelScale = 1.1f;

    [Export] private float _swarmRampUpTime = 15.0f; // time it takes to fill the budget when swarming
    
    [Export] private ResourceBoidEnemy _firstEnemy;
    [Export] private List<ResourceBoidEnemy> _enemyBoidPool = new();

    private enum SpawningState
    {
        Idle,
        Swarming,
        Wave,
        //Patrol, TODO: Implement
        Scripted
    }
    
    private Game _game;
    private BoidPlayer _player;

    private SpawningState _state;
    private float _totalTime;
    private float _totalBudgetDestoyed;
    private float _timeInState;
    private List<BoidEnemyBase> _activeEnemies = new List<BoidEnemyBase>();
    private List<BoidEnemyBase> _waveEnemies = new List<BoidEnemyBase>();
    private List<BoidEnemyBase> _swarmingEnemies = new List<BoidEnemyBase>();
    private List<BoidEnemyBase> _scriptedWaveEnemies = new List<BoidEnemyBase>();
    private List<DataWave> _triggeredWaves = new List<DataWave>();
    private DataWave _scriptedWaveToTrigger;
 
    public void Init(Game game, BoidPlayer player)
    {
        _game = game;
        _player = player;
    }
    
    public override void _Ready()
    {
        Debug.Assert(_firstEnemy != null, "_firstEnemy != null");
        SaveDataPlayer.SetSeenEnemy(_firstEnemy);
        
        ChangeState(SpawningState.Idle);
    }
    
    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.Instance.RegisterWindow("aidirector", "AI Director", _OnImGuiLayout);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.Instance.UnRegisterWindow("aidirector", _OnImGuiLayout);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!_enabled)
            return;

        _totalTime += delta;
        _timeInState += delta;
        
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_activeEnemies[i]))
                _activeEnemies.Remove(_activeEnemies[i]);
        }
        
        // intensity is a tool to create peaks and troughs in intensity
        // it is a value between 0-1
        // the value of intensity will increase over the course of the game
        float intensity = CalcIntensity(_totalTime);

        switch (_state)
        {
            case SpawningState.Idle:
                ProcessStateIdle(delta, intensity);
                break;
            case SpawningState.Swarming:
                ProcessStateSwarming(delta, intensity);
                break;
            case SpawningState.Wave:
                ProcessStateWave(delta, intensity);
                break;
            // case SpawningState.Patrol:
            //     ProcessStatePatrol(delta, intensity);
            //     break;
            case SpawningState.Scripted:
                ProcessStateScripted(delta, intensity);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ProcessStateIdle(float delta, float intensity)
    {
        // randomly break out of idle
        bool exitState = Utils.Rng.Randf() < delta / 10.0f * intensity;
        
        // force break out of idle after a maximum time
        exitState |= _timeInState > 5.0f + 5.0f * (1.0f - intensity);

        // shorten the time of the first idle state
        exitState |= _totalTime < 10.0f && _timeInState > 1.0f;
            
        if (exitState)
        {
            ChangeState(CalcExitState(intensity, 
                new List<SpawningState>() {SpawningState.Swarming, SpawningState.Wave}, 
                new List<float>(){0.5f, 0.5f}));
        }
    }

    private void EnterStateSwarming()
    {
        _swarmingEnemies = new List<BoidEnemyBase>();
    }
    private void ProcessStateSwarming(float delta, float intensity)
    {
        float budgetGoal = Mathf.Clamp(_timeInState / _swarmRampUpTime, 0.0f, 1.0f) * CalcBudget(intensity);
        
        // add up total swarming enemy budget on the field
        float activeBudget = 0.0f;
        for (int i = _swarmingEnemies.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_swarmingEnemies[i]))
            {
                _swarmingEnemies.Remove(_swarmingEnemies[i]);
                continue;
            }

            activeBudget += _swarmingEnemies[i].Data.SpawningCost;
        }
        
        while (activeBudget < budgetGoal)
        {
            List<ResourceBoidEnemy> list = ListPossibleEnemies();
            ResourceBoidEnemy enemy = list[(int) (Utils.Rng.Randi() % list.Count)];
            activeBudget += enemy.SpawningCost;
            _swarmingEnemies.Add(SpawnEnemyRandom(enemy));
        }
        
        // exit swarming once a given time has passed
        bool exitState = _timeInState > 30.0f + 30.0f * (1.0f - intensity);
        
        if (exitState)
        {
            float iInvSq = Mathf.Pow(1.0f - intensity, 2.0f);
            ChangeState(CalcExitState(intensity, 
                new List<SpawningState>() {SpawningState.Idle, SpawningState.Swarming, SpawningState.Wave}, 
                new List<float>(){
                    iInvSq,
                    (1.0f - iInvSq) * 0.5f,
                    (1.0f - iInvSq) * 0.5f
                }));
        }
    }

    private void EnterStateWave()
    {
        _waveEnemies = new List<BoidEnemyBase>();
        
        // get a list of enemies to make a wave from
        List<ResourceBoidEnemy> list = ListPossibleEnemies();
        float budget = CalcBudget(CalcIntensity(_totalTime));
        while (budget >= 0.0f)
        {
            ResourceBoidEnemy enemy = list[(int) (Utils.Rng.Randi() % list.Count)];
            budget -= enemy.SpawningCost;
            _waveEnemies.Add(SpawnEnemyRandom(enemy));
        }
    }
    private void ProcessStateWave(float delta, float intensity)
    {
        for (int i = _waveEnemies.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_waveEnemies[i]))
                _waveEnemies.Remove(_waveEnemies[i]);
        }
        
        // exit if all enemies in the wave are killed
        bool exitState = _waveEnemies.Count == 0;
        
        // exit if too much time has passed
        exitState  |= _timeInState > 30.0f + 30.0f * (1.0f - intensity);
        
        if (exitState)
        {
            float iInvSq = Mathf.Pow(1.0f - intensity, 2.0f);
            ChangeState(CalcExitState(intensity, 
                new List<SpawningState>() {SpawningState.Idle, SpawningState.Swarming, SpawningState.Wave}, 
                new List<float>(){
                    iInvSq,
                    (1.0f - iInvSq) * 0.5f,
                    (1.0f - iInvSq) * 0.5f
                }));
        }
    }

    private void EnterStatePatrol()
    {
        // get a list of enemies to patrol
        List<ResourceBoidEnemy> list = ListPossibleEnemies();
        float budget = CalcBudget(CalcIntensity(_totalTime));
        while (budget >= 0.0f)
        {
            ResourceBoidEnemy enemy = list[(int) (Utils.Rng.Randi() % list.Count)];
            budget -= enemy.SpawningCost;
            SpawnEnemyRandom(enemy);
        }
    }
    private void ProcessStatePatrol(float delta, float intensity)
    {
    }

    private void EnterStateScripted()
    {
        SpawnWave(_scriptedWaveToTrigger, _scriptedWaveEnemies, new List<BoidEnemyBase>());
        
        _triggeredWaves.Add(_scriptedWaveToTrigger);
        _scriptedWaveToTrigger = null;
    }
    
    private void ProcessStateScripted(float delta, float intensity)
    {
        for (int i = _scriptedWaveEnemies.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_scriptedWaveEnemies[i]) || _scriptedWaveEnemies[i].Destroyed)
                _scriptedWaveEnemies.Remove(_scriptedWaveEnemies[i]);
        }
        
        // exit if all enemies in the wave are killed
        bool exitState = _scriptedWaveEnemies.Count == 0;
        if (exitState)
        {
            float iInvSq = Mathf.Pow(1.0f - intensity, 2.0f);
            ChangeState(CalcExitState(intensity, 
                new List<SpawningState>() {SpawningState.Idle, SpawningState.Swarming, SpawningState.Wave}, 
                new List<float>(){
                    iInvSq,
                    (1.0f - iInvSq) * 0.5f,
                    (1.0f - iInvSq) * 0.5f
                }));
        }
    }

    private void ChangeState(SpawningState to)
    {
        GD.Print($"Switching spawning state from {_state} to: {to}");
        _state = to;
        _timeInState = 0.0f;

        switch (to)
        {
            case SpawningState.Idle:
                break;
            case SpawningState.Swarming:
                EnterStateSwarming();
                break;
            case SpawningState.Wave:
                EnterStateWave();
                break;
            // case SpawningState.Patrol:
            //     EnterStatePatrol();
            //     break;
            case SpawningState.Scripted:
                EnterStateScripted();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(to), to, null);
        }
    }
    
    private float CalcIntensity(float time)
    {
        float wavelength = _intensityWavelength + (time * _intensityWavelengthScaling * 0.01f);
        float amplitude = _intensityAmplitude + (time * _intensityAmplitudeScaling * 0.01f);

        wavelength = Mathf.Clamp(wavelength, 0.0f, 1.0f);
        
        float intensity = Mathf.Sin(time * wavelength) * amplitude;

        intensity += _intensityOffset + _intensityOffsetScale * time;
        
        return Mathf.Clamp(intensity, 0.0f, 1.0f);
    }

    private float CalcBudget(float intensity)
    {
        // budget is a factor of intensity and player level
        return _baseBudget * Mathf.Max((float) Math.Log(SaveDataPlayer.Level + 1, 2), 1.0f) * intensity;
    }

    private SpawningState CalcExitState(float intensity, List<SpawningState> exitStates, List<float> exitStateWeights)
    {
        // check for triggered scripted waves
        List<DataWave> waves = Database.Waves.GetAllEntries<DataWave>();
        foreach (DataWave wave in waves)
        {
            if (_triggeredWaves.Contains(wave))
                continue;
            
            if (wave.Introduction && 
                // trigger on time or total budget destroyed, whichever comes first.
                (_totalTime > wave.TriggerTimeMinutes * 60.0f || _totalBudgetDestoyed > wave.TriggerBudget))
            {
                // trigger this enemy intro wave.
                _scriptedWaveToTrigger = wave;
                return SpawningState.Scripted;
            }
        }
        
        float rng = Utils.Rng.Randf();
        for (int i = 0; i < exitStates.Count; i++)
        {
            if (rng < exitStateWeights[i])
            {
                return exitStates[i];
            }
            rng -= exitStateWeights[i];
        }

        return SpawningState.Idle;
    }

    private List<ResourceBoidEnemy> ListPossibleEnemies()
    {
        List<ResourceBoidEnemy> possibleTypes = new();
        foreach (ResourceBoidEnemy data in _enemyBoidPool)
        {
            if (SaveDataPlayer.HasSeenEnemy(data))
                possibleTypes.Add(data);
        }

        return possibleTypes;
    }

    public BoidEnemyBase SpawnEnemyRandom(ResourceBoidEnemy enemyData)
    {
        // spawn at a random location around the spawning circle centred on the player
        Vector2 spawnPos = _game.SpawningRect.RandPointOnEdge();
        Vector2 spawnVel = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()).Normalized() * 100.0f;
        return SpawnEnemy(enemyData, spawnPos, spawnVel);
    }

    public BoidEnemyBase SpawnEnemy(ResourceBoidEnemy enemyData, Vector2 pos, Vector2 vel)
    {
        BoidEnemyBase enemy = BoidFactory.Instance.CreateEnemyBoid(enemyData, pos, vel);
        _activeEnemies.Add(enemy);
        enemy.OnBoidDestroyed += _OnEnemyDestroyed;
        return enemy;
    }

    private void SpawnEnemyEscort(ResourceBoidEnemy leaderData, List<ResourceBoidEnemy> escortDatas, out BoidEnemyBase leaderBoid, out List<BoidEnemyBase> escortBoids)
    {
        escortBoids = new List<BoidEnemyBase>();
        BoidEnemyBase leader = SpawnEnemyRandom(leaderData);
        foreach (ResourceBoidEnemy escortData in escortDatas)
        {
            float f = (float) GD.RandRange(0.0f, Mathf.Pi * 2.0f);
            float escortRadius = 50.0f;
            Vector2 spawnPos = leader.GlobalPosition + new Vector2(Mathf.Sin(f), -Mathf.Cos(f)).Normalized() * escortRadius;
            BoidEnemyBase escort = SpawnEnemy(escortData, spawnPos, Vector2.Zero);
            escortBoids.Add(escort);
            //escort.SetupEscort(leader);
            Debug.Assert(false, "Escort currently not supported.");
        }

        leaderBoid = leader;
    }

    public void SpawnWave(DataWave wave, List<BoidEnemyBase> primaryList, List<BoidEnemyBase> secondaryList)
    {
        switch (wave.WaveType)
        {
            case DataWave.Type.Standard:
            {
                // spawn
                foreach (ResourceBoidEnemy enemy in wave.PrimarySpawns)
                {
                    primaryList.Add(SpawnEnemyRandom(enemy));
                }
                break;
            }
            case DataWave.Type.Escort:
            {
                Debug.Assert(wave.PrimarySpawns.Count > 0, "Invalid wave setup");

                // spawn
                SpawnEnemyEscort(wave.PrimarySpawns[0], wave.SecondarySpawns,
                    out BoidEnemyBase spawnedLeader,
                    out secondaryList);
                primaryList.Add(spawnedLeader);
                break;
            }
            case DataWave.Type.Swarm:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void _OnEnemyDestroyed(BoidBase boid)
    {
        BoidEnemyBase enemy = boid as BoidEnemyBase;
        Debug.Assert(enemy != null, "enemy != null");
        _totalBudgetDestoyed += enemy.Data.SpawningCost;
    }

    private void _OnImGuiLayout()
    {
        ImGui.Text($"[{_state}] for {_timeInState:F2}");
        ImGui.Text($"{CalcIntensity(_totalTime):F2} Intensity");
        ImGui.Text($"{_totalTime:F2} TotalTime");
        ImGui.Text($"{CalcBudget(CalcIntensity(_totalTime)):F2} Current Budget");
        ImGui.Text($"{_totalBudgetDestoyed:F1} Total Budget Destroyed");
    
        ImGui.Spacing();

        if (ImGui.Button("+10s"))
        {
            _totalTime += 10.0f;
            _timeInState += 10.0f;
        }
        ImGui.SameLine();
        if (ImGui.Button("+60s"))
        {
            _totalTime += 60.0f;
            _timeInState += 60.0f;
        }

        ImGui.Checkbox("Enabled", ref _enabled);
    
        int secondsToPlot = 60 * 10;
        float[] points = new float[secondsToPlot];
        for (int i = 0; i < secondsToPlot; i++)
        {
            points[i] = CalcIntensity(i);
        }
        ImGui.PlotHistogram("", ref points[0], secondsToPlot, 
            0, "", 0.0f, 1.0f, new System.Numerics.Vector2(200, 200));

        ImGui.SliderFloat("Wavelength", ref _intensityWavelength, 0.0f, 0.1f);
        ImGui.SliderFloat("WavelengthScale", ref _intensityWavelengthScaling, -0.1f, 0.1f);
    
        ImGui.SliderFloat("Amplitude", ref _intensityAmplitude, 0.0f, 10.0f);
        ImGui.SliderFloat("AmplitudeScale", ref _intensityAmplitudeScaling, 0.0f, 0.5f);
    
        ImGui.SliderFloat("Offset", ref _intensityOffset, -1.0f, 1.0f);
        ImGui.SliderFloat("OffsetScale", ref _intensityOffsetScale, 0.0f, 0.01f);
        
        ImGui.Text(" ### Spawn");
        foreach (ResourceBoidEnemy enemy in _enemyBoidPool)
        {
            if (ImGui.Button($"{enemy.DisplayName}"))
            {
                BoidFactory.Instance.CreateEnemyBoid(enemy, Vector2.Zero, Vector2.Zero);
            }

            if (ImGui.Button($"{enemy.DisplayName} x10"))
            {
                for (int i = 0; i < 10; i++)
                    BoidFactory.Instance.CreateEnemyBoid(enemy, Vector2.Zero, Vector2.Zero);
            }
        }
    }
}
