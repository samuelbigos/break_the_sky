using Godot;
using System;

public class Metagame : Node
{
    public static Metagame Instance;

    public enum GameState
    {
        Frontend,
        Deploy,
        InGame,
        Results
    }
    
    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }
}
