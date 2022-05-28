using Godot;
using System;
using System.Collections.Generic;

public abstract class BaseState<T>
{
    public List<T> PossibleExitStates = new();

    public abstract bool ShouldEnter(T currentState, T prevState);
    public abstract void Enter(T prevState);
    public abstract void Exit(T toState);
    public abstract void Update(float delta);
}
