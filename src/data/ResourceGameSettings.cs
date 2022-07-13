using Godot;
using System;
using ImGuiNET;

public class ResourceGameSettings : Resource
{
    [Export] public int ExperiencePerLevelBase;
    [Export] public float ExperiencePerLevelExponent;

    public void _OnImGuiLayout()
    {
        ImGui.InputInt("ExperiencePerLevelBase", ref ExperiencePerLevelBase);
        ImGui.SliderFloat("ExperiencePerLevelExponent", ref ExperiencePerLevelExponent, 1.0f, 1.5f);
        
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
