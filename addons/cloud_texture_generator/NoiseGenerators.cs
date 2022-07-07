using Godot;
using System;

[Tool]
public class NoiseGenerators
{
    private static Vector3 Floor(Vector3 x)
    {
        Vector3 ret = new Vector3(Mathf.Floor(x.x),
            Mathf.Floor(x.y),
            Mathf.Floor(x.z));
        return ret;
    }
    
    private static Vector3 Fract(Vector3 x)
    {
        Vector3 ret = new Vector3(x.x - Mathf.Floor(x.x),
            x.y - Mathf.Floor(x.y),
            x.z - Mathf.Floor(x.z));
        return ret;
    }

    private static Vector3 Sin(Vector3 x)
    {
        Vector3 ret = new Vector3(Mathf.Sin(x.x),
            Mathf.Sin(x.y),
            Mathf.Sin(x.z));
        return ret;
    }

    private static Vector3 Mod(Vector3 a, Vector3 b)
    {
        return a - b * Floor(a / b);
    }
    
    private static Vector3 Mod(Vector3 a, float b)
    {
        return new Vector3(a.x % b, a.y % b, a.z % b);
    }
    
    private static Vector3 Hash(Vector3 p)
    {
        p = new Vector3(p.Dot(new Vector3(127.1f, 311.7f, 74.7f)),
            p.Dot(new Vector3(269.5f,183.3f,246.1f)),
            p.Dot(new Vector3(113.5f,271.9f,124.6f)));

        return new Vector3(-1.0f, -1.0f, -1.0f) + 2.0f * Fract(Sin(p) * 43758.5453123f);
    }

    public static float WorleyNoise(Vector3 uv, float freq)
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
        
        return minDist * 2.0f - 1.0f;
    }
    
    public static float WorleyFbm(Vector3 pos, int octaves, float freq, float lacunarity, float amplitude)
    {
        float worley = 0.0f;
        float gain = 1.0f;
        for (int oct = 0; oct < octaves; oct++)
        {
            worley += WorleyNoise(pos * freq, freq) * gain;
            freq *= lacunarity;
            gain *= amplitude;
        }
        return worley;
    }
}
