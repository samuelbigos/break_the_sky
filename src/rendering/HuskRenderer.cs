using Godot;
using System;
using System.Diagnostics;
using System.Numerics;
using Array = Godot.Collections.Array;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

public class HuskRenderer : MeshInstance
{
    public static HuskRenderer Instance;
    
    private int _v = 0;
    private int _i = 0;
    
    // TODO: Split into chunks so we limit the amount of verts processed when adding a new husk.
    // maintain maybe 100 husk chunks and destroy old ones when we've used all of them.
    private Vector3[] _vertList = new Vector3[100000];
    private Vector3[] _normList = new Vector3[100000];
    private float[] _tangentList = new float[100000 * 4];
    private Vector2[] _uvList = new Vector2[100000];
    private int[] _indexList = new int[200000];
    
    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;

        CastShadow = ShadowCastingSetting.On;
    }

    public void AddHusk(Mesh mesh, Transform transform)
    {
        Array meshArray = mesh.SurfaceGetArrays(0);
        Vector3[] meshVerts = meshArray[(int) ArrayMesh.ArrayType.Vertex] as Vector3[];
        Vector3[] meshNormals = meshArray[(int) ArrayMesh.ArrayType.Normal] as Vector3[];
        float[] meshTangents = meshArray[(int) ArrayMesh.ArrayType.Tangent] as float[];
        Vector2[] meshUvs = meshArray[(int) ArrayMesh.ArrayType.TexUv] as Vector2[];
        int[] meshIndices = meshArray[(int) ArrayMesh.ArrayType.Index] as int[];
        
        if (_v + meshVerts.Length >= _vertList.Length || _i + meshIndices.Length >= _indexList.Length)
        {
            return;
        }

        for (int v = 0; v < meshVerts.Length; v++)
        {
            _vertList[_v + v] = transform.Xform(meshVerts[v]);
            _normList[_v + v] = meshNormals[v];
            _tangentList[_v + ToTangentIndex(v, 0)] = meshTangents[ToTangentIndex(v, 0)];
            _tangentList[_v + ToTangentIndex(v, 1)] = meshTangents[ToTangentIndex(v, 1)];
            _tangentList[_v + ToTangentIndex(v, 2)] = meshTangents[ToTangentIndex(v, 2)];
            _tangentList[_v + ToTangentIndex(v, 3)] = meshTangents[ToTangentIndex(v, 3)];
            _uvList[_v + v] = meshUvs[v];
        }

        for (int i = 0; i < meshIndices.Length; i++)
        {
            _indexList[_i + i] = meshIndices[i] + _v;
        }

        _v += meshVerts.Length;
        _i += meshIndices.Length;
        
        Debug.Assert(_v < _vertList.Length, "v < _vertList.Length");
        Debug.Assert(_i < _indexList.Length, "i < _indexList.Length");

        Span<Vector3> verts = _vertList.AsSpan(0, _v);
        Span<Vector3> normals = _normList.AsSpan(0, _v);
        Span<float> tangents = _tangentList.AsSpan(0, _v * 4);
        Span<Vector2> uvs = _uvList.AsSpan(0, _v);
        Span<int> indices = _indexList.AsSpan(0, _i);
        
        Array arrays = new();
        arrays.Resize((int) ArrayMesh.ArrayType.Max);
        arrays[(int) ArrayMesh.ArrayType.Vertex] = verts.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Normal] = normals.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Tangent] = tangents.ToArray();
        arrays[(int) ArrayMesh.ArrayType.TexUv] = uvs.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Index] = indices.ToArray();

        ArrayMesh outMesh = new();
        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Mesh = outMesh;
    }

    private int ToTangentIndex(int i, int t)
    {
        return i * 4 + t;
    }
}
