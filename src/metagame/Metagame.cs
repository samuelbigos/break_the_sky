using Godot;
using System;
using Godot.Collections;

public class Metagame : Saveable
{
    public static Metagame Instance;

    [Export] public int InitialCitiesUnderAttack = 2;

    [Export] public int BaseMaxAllyCount = 50;
    [Export] public int BaseInitialAllyCount = 10;

    public enum GameState
    {
        Map,
        Deploy,
        InGame,
        Results
    }

    public GameState CurrentState
    {
        get => (GameState) Convert.ToInt32(_data["currentState"]);
        private set => _data["currentState"] = value;
    }
    
    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }

    protected override void Validate()
    {
    }

    public override void DoLoad(Dictionary data)
    {
        base.DoLoad(data);
    }

    public override void InitialiseSaveData()
    {
        CurrentState = GameState.Map;
    }

    public void ChangeState(GameState state)
    {
        switch (CurrentState)
        {
            case GameState.Map:
                ExitMap(state);
                break;
            case GameState.Deploy:
                ExitDeploy(state);
                break;
            case GameState.InGame:
                ExitInGame(state);
                break;
            case GameState.Results:
                ExitResults(state);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        switch (state)
        {
            case GameState.Map:
                EnterMap(CurrentState);
                break;
            case GameState.Deploy:
                EnterDeploy(CurrentState);
                break;
            case GameState.InGame:
                EnterInGame(CurrentState);
                break;
            case GameState.Results:
                EnterResults(CurrentState);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        CurrentState = state;
        SaveManager.DoSave();
    }

    private void EnterMap(GameState from)
    {
    }
    
    private void EnterDeploy(GameState from)
    {
    }
    
    private void EnterInGame(GameState from)
    {
    }
    
    private void EnterResults(GameState from)
    {
    }
    
    private void ExitMap(GameState to)
    {
    }
    
    private void ExitDeploy(GameState to)
    {
    }
    
    private void ExitInGame(GameState to)
    {
    }
    
    private void ExitResults(GameState to)
    {
    }
}
