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

    private float Mix(float x, float y, float a)
    {
        return x * (1.0f - a) + y * a;
    }
    
    private float Remap(float x, float a, float b, float c, float d)
    {
        return (((x - a) / (b - a)) * (d - c)) + c;
    }

    private float Fract(float x)
    {
        return x - Mathf.Floor(x);
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
    
    // TODO: replace with tileable noise.
    // https://iquilezles.org/articles/morenoise/
    private float ValueNoise(Vector3 x)
    {
        // grid
        Vector3 i = Floor(x);
        Vector3 w = Fract(x);
    
        // quintic interpolant
        Vector3 u = w*w*w*(w*(w*6.0f-Vector3.One*15.0f)+Vector3.One*10.0f);
        Vector3 du = 30.0f*w*w*(w*(w-Vector3.One*2.0f)+Vector3.One*1.0f);  
    
        // gradients
        Vector3 ga = Hash( i+new Vector3(0.0f,0.0f,0.0f) );
        Vector3 gb = Hash( i+new Vector3(1.0f,0.0f,0.0f) );
        Vector3 gc = Hash( i+new Vector3(0.0f,1.0f,0.0f) );
        Vector3 gd = Hash( i+new Vector3(1.0f,1.0f,0.0f) );
        Vector3 ge = Hash( i+new Vector3(0.0f,0.0f,1.0f) );
        Vector3 gf = Hash( i+new Vector3(1.0f,0.0f,1.0f) );
        Vector3 gg = Hash( i+new Vector3(0.0f,1.0f,1.0f) );
        Vector3 gh = Hash( i+new Vector3(1.0f,1.0f,1.0f) );
    
        // projections
        float va = ga.Dot(w-new Vector3(0.0f,0.0f,0.0f) );
        float vb = gb.Dot( w-new Vector3(1.0f,0.0f,0.0f) );
        float vc = gc.Dot( w-new Vector3(0.0f,1.0f,0.0f) );
        float vd = gd.Dot( w-new Vector3(1.0f,1.0f,0.0f) );
        float ve = ge.Dot( w-new Vector3(0.0f,0.0f,1.0f) );
        float vf = gf.Dot( w-new Vector3(1.0f,0.0f,1.0f) );
        float vg = gg.Dot( w-new Vector3(0.0f,1.0f,1.0f) );
        float vh = gh.Dot( w-new Vector3(1.0f,1.0f,1.0f) );
	
        // interpolations
        return va + u.x * (vb - va) + u.y * (vc - va) + u.z * (ve - va) + u.x * u.y * (va - vb - vc + vd) +
                    u.y * u.z * (va - vc - ve + vg) + u.z * u.x * (va - vb - ve + vf) +
                    (-va + vb + vc - vd + ve - vf - vg + vh) * u.x * u.y * u.z;
    }

    float ValueFbm(Vector3 p, float freq, int octaves)
    {
        float g = Mathf.Pow(2.0f, -.85f);
        float amp = 1.0f;
        float noise = 0.0f;
        for (int i = 0; i < octaves; ++i)
        {
            noise += amp * ValueNoise(p * freq);
            freq *= 2.0f;
            amp *= g;
        }   
    
        return noise;
    }

    private void _OnButtonPressed()
    {
        const int sizeX = 128;
        const int sizeY = 128;
        const int sizeZ = 128;
        const float freq = 4.0f;

        Texture3D texture3D = new Texture3D();
        texture3D.Create(sizeX, sizeZ, sizeY, Image.Format.Rgba8);
        
        NativeScript anlScript = GD.Load("res://gdnative/bin/anl.gdns") as NativeScript;
        Debug.Assert(anlScript != null, "anlScript != null");
        Node anl = anlScript.New() as Node;
        Debug.Assert(anl != null, "anl != null");
        
        anl.Call("Generate3DGradientNoiseImage", sizeX, freq, DateTime.Now.Millisecond);

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
        }

        Directory dir = new Directory();
        dir.Remove("res://assets/textures/noise/cloud_noise.tex3d");
        ResourceSaver.Save("res://assets/textures/noise/cloud_noise.tex3d", texture3D);
    }
}
