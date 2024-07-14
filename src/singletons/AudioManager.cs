using Godot;
using System;

public partial class AudioManager : Singleton<AudioManager>
{
    [Export] public AudioStreamPlayer2D SFXPickup;
}
