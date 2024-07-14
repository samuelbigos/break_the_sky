using Godot;
using System;

public partial class GameState_Construct : BaseState<StateMachine_Game.States>
{
    public GameState_Construct()
    {
        PossibleExitStates.Add(StateMachine_Game.States.Play);
        PossibleExitStates.Add(StateMachine_Game.States.TacticalPause);
    }

    public override bool ShouldEnter(StateMachine_Game.States currentState, StateMachine_Game.States prevState)
    {
        switch (currentState)
        {
            case StateMachine_Game.States.TacticalPause 
                when Input.IsActionJustReleased("toggle_construct_ui"):
                return true;
            
            case StateMachine_Game.States.Play 
                when Input.IsActionJustReleased("toggle_construct_ui"):
                return true;
            
            case StateMachine_Game.States.Play
                when HUD.Instance.RequestShowConstructMenu:
                return true;
            
            default:
                return false;
        }
    }

    public override void Enter(StateMachine_Game.States prevState)
    {
        Engine.TimeScale = 0.0f;
    }

    public override void Exit(StateMachine_Game.States toState)
    {
    }

    public override void Update(double delta)
    {
    }
}
