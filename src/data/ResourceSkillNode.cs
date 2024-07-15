using Godot;
using System;

//[Tool]
public partial class ResourceSkillNode : Resource
{
    [Export] public Texture2D Icon;
    [Export] public bool Major;
    
    [Export] public float AttackDamage = 1.0f;
    [Export] public float AttackCooldown = 1.0f;
    [Export] public float AttackSpread = 1.0f;
    [Export] public float AttackVelocity = 1.0f;
    [Export] public float MoveSpeed = 1.0f;
    [Export] public float MaxHealth = 1.0f;
    [Export] public float Regeneration = 0.0f;
    [Export] public float Size = 1.0f;
    [Export] public float MicroTurretRange = 1.0f;
    [Export] public float MicroTurretCooldown = 1.0f;
    [Export] public float MicroTurretDamage = 1.0f;
    [Export] public float MicroTurretVelocity = 1.0f;
    
    [Export] public bool MicroTurrets = false;
    [Export] public int Penetration = 0;
    [Export] public bool AreaDamage = false;
}