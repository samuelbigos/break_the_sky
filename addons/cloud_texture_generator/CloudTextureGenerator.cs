using Godot;
using System;
using System.Drawing;
using ProceduralNoiseProject;
using Color = Godot.Color;
using Image = Godot.Image;

[Tool]
public class CloudTextureGenerator : EditorPlugin
{
    private Button _dock;

    public override void _EnterTree()
    {
        base._EnterTree();

        _dock = ResourceLoader.Load<PackedScene>("res://addons/cloud_texture_generator/TheButton.tscn").Instance<Button>();
        _dock.Connect("pressed", this, nameof(_OnButtonPressed));
        
        AddControlToDock(DockSlot.RightUr, _dock);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        RemoveControlFromDocks(_dock);
        
        _dock.Free();
    }
    
    float WorleyFbm(WorleyNoise worley, int x, int y, int z, float freq)
    {
        worley.Frequency = freq;
        float a = worley.Sample3D(x * freq, y * freq, z * freq) * .625f;
        worley.Frequency = freq * 2.0f;
        float b = worley.Sample3D(x * freq * 2.0f, y * freq * 2.0f, z * freq * 2.0f) * .25f;
        worley.Frequency = freq * 2.0f;
        float c = worley.Sample3D(x * freq * 4.0f, y * freq * 4.0f, z * freq * 4.0f) * .125f;
        return a + b + c;
    }

    private float Mix(float x, float y, float a)
    {
        return x * (1.0f - a) + y * a;
    }
    
    private float Remap(float x, float a, float b, float c, float d)
    {
        return (((x - a) / (b - a)) * (d - c)) + c;
    }

    private void _OnButtonPressed()
    {
        const int sizeX = 128;
        const int sizeY = 10;
        const int sizeZ = 128;
        
        OpenSimplexNoise simplexNoise = new OpenSimplexNoise();
        simplexNoise.Octaves = 1;
        simplexNoise.Period = 10.0f;

        float worleyFreq = 0.1f;
        WorleyNoise worley = new WorleyNoise(DateTime.Now.Millisecond, worleyFreq, 1.0f, 1.0f);

        Texture3D texture3D = new Texture3D();
        texture3D.Create(sizeX, sizeY, sizeZ, Image.Format.Rgbaf);

        for (int z = 0; z < sizeZ; z++)
        {
            Image layer = new Image();
            layer.Create(sizeX, sizeY, true, Image.Format.Rgbaf);
            layer.Lock();
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    Color col = new Color();

                    float simplex = simplexNoise.GetNoise3d(x, y, z) * 0.5f + 0.5f;
                    simplex = Mix(1.0f, simplex, 0.5f);
                    
                    col.g = 1.0f - WorleyFbm(worley, x, y, z, worleyFreq);
                    col.b = 1.0f - WorleyFbm(worley, x, y, z, worleyFreq * 2.0f);
                    col.a = 1.0f - WorleyFbm(worley, x, y, z, worleyFreq * 4.0f);
                    col.r = Remap(simplex, 0.0f, 1.0f, col.g, 1.0f);
                    
                    layer.SetPixel(x, y, col);
                }
            }
            texture3D.SetLayerData(layer, z);
            layer.Unlock();
        }

        ResourceSaver.Save("res://assets/textures/noise/cloud_noise.res", texture3D);
    }
}
