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

    public enum SpawningState
    {
        Idle,
        Swarming,
        Wave,
        //Patrol, TODO: Implement
        Scripted
    }
    
    private Game _game;
    private Player _player;

    private SpawningState _state;
    private float _totalTime;
    private float _timeInState;
    private List<BoidEnemyBase> _activeEnemies = new List<BoidEnemyBase>();
    private List<BoidEnemyBase> _waveEnemies = new List<BoidEnemyBase>();
    private List<BoidEnemyBase> _swarmingEnemies = new List<BoidEnemyBase>();
 
    public void Init(Game game, Player player)
    {
        _game = game;
        _player = player;
    }
    
    public override void _Ready()
    {
        ChangeState(SpawningState.Idle);
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

            activeBudget += Database.EnemyBoids.FindEntry<DataEnemyBoid>(_swarmingEnemies[i].ID).SpawningCost;
        }
        
        while (activeBudget < budgetGoal)
        {
            List<DataEnemyBoid> list = ListPossibleEnemies();
            DataEnemyBoid enemy = list[(int) (Utils.Rng.Randi() % list.Count)];
            activeBudget += enemy.SpawningCost;
            _swarmingEnemies.Add(SpawnEnemy(enemy));
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
        List<DataEnemyBoid> list = ListPossibleEnemies();
        float budget = CalcBudget(CalcIntensity(_totalTime));
        while (budget >= 0.0f)
        {
            DataEnemyBoid enemy = list[(int) (Utils.Rng.Randi() % list.Count)];
            budget -= enemy.SpawningCost;
            _waveEnemies.Add(SpawnEnemy(enemy));
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
        List<DataEnemyBoid> list = ListPossibleEnemies();
        float budget = CalcBudget(CalcIntensity(_totalTime));
        while (budget >= 0.0f)
        {
            DataEnemyBoid enemy = list[(int) (Utils.Rng.Randi() % list.Count)];
            budget -= enemy.SpawningCost;
            SpawnEnemy(enemy);
        }
    }
    private void ProcessStatePatrol(float delta, float intensity)
    {
        
    }
    
    private void ProcessStateScripted(float delta, float intensity)
    {
        
    }

    private void ChangeState(SpawningState to)
    {
        GD.Print($"Switching spawning state to: {to}");
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
        return _baseBudget * (SaveDataPlayer.Instance.Level * _levelScale) * intensity;
    }

    private SpawningState CalcExitState(float intensity, List<SpawningState> exitStates, List<float> exitStateWeights)
    {
        float rng = Utils.Rng.Randf();
        for (int i = 0; i < exitStates.Count; i++)
        {
            if (rng < exitStateWeights[i])
            {
                return (SpawningState) exitStates[i];
            }
            rng -= exitStateWeights[i];
        }

        return SpawningState.Idle;
    }

    private List<DataEnemyBoid> ListPossibleEnemies()
    {
        List<DataEnemyBoid> possibleTypes = new List<DataEnemyBoid>();
        foreach (DataEnemyBoid enemy in Database.EnemyBoids.GetAllEntries<DataEnemyBoid>())
        {
            if (SaveDataPlayer.Instance.HasSeenEnemy(enemy.Name))
                possibleTypes.Add(enemy);
        }

        return possibleTypes;
    }
    
    public BoidEnemyBase SpawnEnemy(DataEnemyBoid id)
    {
        BoidEnemyBase enemy = id.Scene.Instance<BoidEnemyBase>();

        float f = (float) GD.RandRange(0.0f, Mathf.Pi * 2.0f);
        
        // spawn at a random location around the spawning circle centred on the player
        Vector2 spawnPos = _player.GlobalPosition + new Vector2(Mathf.Sin(f), -Mathf.Cos(f)).Normalized() * _game.SpawningRadius;
        AddChild(enemy);
        enemy.GlobalPosition = spawnPos;
        enemy.Init(id.Name, _player, _game, _player);
        _game.AddEnemy(enemy);
        _activeEnemies.Add(enemy);
        return enemy;
    }

    public void _OnImGuiLayout()
    {
        int secondsToPlot = 60 * 10;
        float[] points = new float[secondsToPlot];
        for (int i = 0; i < secondsToPlot; i++)
        {
            points[i] = CalcIntensity(i);
        }

        ImGui.Text($"[{_state}] for {_timeInState:F2}");
        ImGui.Text($"{CalcIntensity(_totalTime):F2} Intensity");
        ImGui.Text($"{_totalTime:F2} TotalTime");
        ImGui.Text($"{CalcBudget(CalcIntensity(_totalTime)):F2} Budget");
        
        ImGui.Spacing();
        
        ImGui.PlotHistogram("", ref points[0], secondsToPlot, 
            0, "", 0.0f, 1.0f, new System.Numerics.Vector2(200, 200));

        ImGui.SliderFloat("Wavelength", ref _intensityWavelength, 0.0f, 0.1f);
        ImGui.SliderFloat("WavelengthScale", ref _intensityWavelengthScaling, -0.1f, 0.1f);
        
        ImGui.SliderFloat("Amplitude", ref _intensityAmplitude, 0.0f, 10.0f);
        ImGui.SliderFloat("AmplitudeScale", ref _intensityAmplitudeScaling, 0.0f, 0.5f);
        
        ImGui.SliderFloat("Offset", ref _intensityOffset, -1.0f, 1.0f);
        ImGui.SliderFloat("OffsetScale", ref _intensityOffsetScale, 0.0f, 0.01f);
    }
}
