using Godot;
using System;
using ImGuiNET;

public class GameSettingsResource : Resource
{
    [Export] public float ExperiencePerLevelBase;
    [Export] public float ExperiencePerLevelExponent;

    public void _OnImGuiLayout()
    {
        ImGui.Text($"ExperiencePerLevelBase: {ExperiencePerLevelBase}");
        ImGui.Text($"ExperiencePerLevelBase: {ExperiencePerLevelExponent}");
        
        ImGui.Text("Level Curve");
        const int levelsToPlot = 50;
        float[] points = new float[levelsToPlot];
        for (int i = 0; i < levelsToPlot; i++)
        {
            points[i] = SaveDataPlayer.TotalExpRequiredForLevel(i);
        }
        ImGui.PlotHistogram("", ref points[0], levelsToPlot, 
            0, "", 0.0f, points[levelsToPlot - 1], new System.Numerics.Vector2(200, 200));
    }
}
