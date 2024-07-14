using Godot;
using System;
using System.Collections.Generic;

public partial class StateMachine_Game : BaseStateMachine<StateMachine_Game, StateMachine_Game.States>
{
    public enum States
    {
        Play,
        Construct,
        TacticalPause,
        COUNT
    }
    
    public static Action<States, States> OnGameStateChanged;
    public static States CurrentState => Instance._currentState;
    public static States PrevState => Instance._prevState;
    
    private Dictionary<States, BaseState<States>> _states = new();
    private States _currentState;
    private States _prevState;

    public override void _Ready()
    {
        base._Ready();

        _states[States.Play] = new GameState_Play();
        _states[States.Construct] = new GameState_Construct();
        _states[States.TacticalPause] = new GameState_TacticalPause();

        _currentState = States.Play;
    }

    public void SendInitialStateChange()
    {
        OnGameStateChanged?.Invoke(_currentState, States.COUNT);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _states[_currentState].Update(delta);
        
        foreach (States state in _states[_currentState].PossibleExitStates)
        {
            if (state == States.COUNT)
                continue;
            
            if (_states[state].ShouldEnter(_currentState, _prevState))
            {
                _states[_currentState].Exit(state);
                _states[state].Enter(_currentState);
                OnGameStateChanged?.Invoke(state, _currentState);
                _prevState = _currentState;
                _currentState = state;
            }
        }
    }
}
