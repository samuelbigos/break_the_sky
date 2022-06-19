using Godot;
using System;

[Tool]
public class SkillNodeResource : Resource
{
    [Export] public Texture Icon;
    
    [Export] public float AttackDamage = 1.0f;
    [Export] public float AttackCooldown = 1.0f;
    [Export] public float AttackSpread = 1.0f;
    [Export] public float AttackVelocity = 1.0f;
    [Export] public float MoveSpeed = 1.0f;
    [Export] public float MaxHealth = 1.0f;
    [Export] public float Regeneration = 0.0f;
    [Export] public float Size = 1.0f;

    [Export] public bool Microbullets = false;
    [Export] public int Penetration = 0;
    [Export] public bool AreaDamage = false;
}