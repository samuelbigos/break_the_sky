using Godot;
using System;
using GodotOnReady.Attributes;

public partial class AudioManager : Singleton<AudioManager>
{
    [OnReadyGet] public AudioStreamPlayer2D SFXPickup;
}
