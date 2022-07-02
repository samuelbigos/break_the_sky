#if TOOLS
using Godot;
using System;
using System.Diagnostics;
using Color = Godot.Color;
using Image = Godot.Image;
using Vector3 = Godot.Vector3;

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

    private Vector3 Floor(Vector3 x)
    {
        Vector3 ret = new Vector3(Mathf.Floor(x.x),
            Mathf.Floor(x.y),
            Mathf.Floor(x.z));
        return ret;
    }
    
    private Vector3 Fract(Vector3 x)
    {
        Vector3 ret = new Vector3(x.x - Mathf.Floor(x.x),
            x.y - Mathf.Floor(x.y),
            x.z - Mathf.Floor(x.z));
        return ret;
    }

    private Vector3 Sin(Vector3 x)
    {
        Vector3 ret = new Vector3(Mathf.Sin(x.x),
            Mathf.Sin(x.y),
            Mathf.Sin(x.z));
        return ret;
    }

    private Vector3 Mod(Vector3 a, Vector3 b)
    {
        return a - b * Floor(a / b);
    }
    
    private Vector3 Mod(Vector3 a, float b)
    {
        return new Vector3(a.x % b, a.y % b, a.z % b);
    }
    
    private Vector3 Hash(Vector3 p)
    {
        p = new Vector3(p.Dot(new Vector3(127.1f, 311.7f, 74.7f)),
            p.Dot(new Vector3(269.5f,183.3f,246.1f)),
            p.Dot(new Vector3(113.5f,271.9f,124.6f)));

        return new Vector3(-1.0f, -1.0f, -1.0f) + 2.0f * Fract(Sin(p) * 43758.5453123f);
    }
    
    float WorleyNoise(Vector3 uv, float freq)
    {    
        Vector3 id = Floor(uv);
        Vector3 p = Fract(uv);
    
        float minDist = 10000.0f;
        for (int x = -1; x <= 1; ++x)
        {
            for(int y = -1; y <= 1; ++y)
            {
                for(int z = -1; z <= 1; ++z)
                {
                    Vector3 offset = new Vector3(x, y, z);
                    Vector3 h = Hash(Mod(id + offset, new Vector3(freq, freq, freq))) * 0.5f + Vector3.One * 0.5f;
                    h += offset;
                    Vector3 d = p - h;
                    minDist = Mathf.Min(minDist, d.Dot(d));
                }
            }
        }
    
        // inverted worley noise
        return 1.0f - minDist;
    }
    
    float WorleyFbm(Vector3 pos, float freq)
    {
        float a = WorleyNoise(pos * freq, freq) * .625f;
        float b = WorleyNoise(pos * freq * 2.0f, freq * 2.0f) * .25f;
        float c = WorleyNoise(pos * freq * 4.0f, freq * 4.0f) * .125f;
        return a + b + c;
    }
    
    private void _OnButtonPressed()
    {
        const int sizeX = 32;
        const int sizeY = 32;
        const int sizeZ = 32;
        const float freq = 4.0f;

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        float tick = 1.0f / Stopwatch.Frequency;

        Texture3D texture3D = new Texture3D();
        texture3D.Create(sizeX, sizeZ, sizeY, Image.Format.Rgba8, (uint) (TextureLayered.FlagsEnum.FlagFilter | TextureLayered.FlagsEnum.FlagRepeat));
        
        NativeScript anlScript = GD.Load("res://gdnative/bin/anl.gdns") as NativeScript;
        Debug.Assert(anlScript != null, "anlScript != null");
        Node anl = anlScript.New() as Node;
        Debug.Assert(anl != null, "anl != null");
        
        anl.Call("Generate3DGradientNoiseImage", sizeX, freq, freq, freq, DateTime.Now.Millisecond);
        
        GD.Print($"Generate3DGradientNoiseImage {stopwatch.ElapsedTicks * tick:F2} seconds.");
        stopwatch.Restart();
        
        for (int y = 0; y < sizeY; y++)
        {
            Image layer = new Image();
            layer.Create(sizeX, sizeZ, true, Image.Format.Rgba8);
            layer.Lock();
            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Color col = new Color();
                    Vector3 pos = new Vector3(x, z, y) / sizeX;
                    
                    col.g = WorleyFbm(pos, freq);
                    col.b = WorleyFbm(pos, freq * 2.0f);
                    col.a = WorleyFbm(pos, freq * 4.0f);

                    col.r = (float)Convert.ToDouble(anl.Call("SampleGradientImage", x, y, z)) * 0.5f + 0.5f;
                    
                    layer.SetPixel(x, z, col);
                }
            }
            layer.Unlock();
            texture3D.SetLayerData(layer, y);
            
            GD.Print($"Y {y} - {stopwatch.ElapsedTicks * tick:F2} seconds.");
            stopwatch.Restart();
        }

        Directory dir = new Directory();
        dir.Remove("res://assets/textures/noise/cloud_noise.tex3d");
        ResourceSaver.Save("res://assets/textures/noise/cloud_noise.tex3d", texture3D);
        
        GD.Print($"Save - {stopwatch.ElapsedTicks * tick:F2} seconds.");
        
        anlScript.Free();
        anl.Free();
    }
}
#endif