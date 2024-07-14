using Godot;
using System;

public partial class GameState_Play : BaseState<StateMachine_Game.States>
{
    public GameState_Play()
    {
        PossibleExitStates.Add(StateMachine_Game.States.Construct);
        PossibleExitStates.Add(StateMachine_Game.States.TacticalPause);
    }

    public override bool ShouldEnter(StateMachine_Game.States currentState, StateMachine_Game.States prevState)
    {
        switch (currentState)
        {
            case StateMachine_Game.States.TacticalPause when Input.IsActionJustReleased("toggle_tactical_pause"):
                return true;
                
            case StateMachine_Game.States.Construct 
                when Input.IsActionJustReleased("toggle_construct_ui") && prevState != StateMachine_Game.States.TacticalPause:
                return true;
            
            default:
                return false;
        }
    }

    public override void Enter(StateMachine_Game.States prevState)
    {
        Engine.TimeScale = 1.0f;
    }

    public override void Exit(StateMachine_Game.States toState)
    {
    }

    public override void Update(double delta)
    {
    }
}
