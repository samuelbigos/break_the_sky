using Godot;
using System;
using Array = Godot.Collections.Array;

public class HuskRenderer : Singleton<HuskRenderer>
{
    [Export] private int _maxChunks = 20;
    [Export] private int _vertsPerChunk = 50000;
    [Export] private int _indicesPerChunk = 100000;

    [Export] private Material _huskMaterial;
    
    private struct HuskChunk
    {
        public int V;
        public int I;
        public Vector3[] VertList;
        public Vector3[] NormList;
        public float[] TangentList;
        public Vector2[] UvList;
        public int[] IndexList;
        public MeshInstance MeshInstance;
    }

    private HuskChunk[] _chunks;
    private int _currentChunk;

    public override void _Ready()
    {
        base._Ready();
        
        // Initialise all chunks up-front.
        _chunks = new HuskChunk[_maxChunks];
        for (int i = 0; i < _maxChunks; i++)
        {
            MeshInstance mi = new();
            AddChild(mi);
            mi.MaterialOverride = _huskMaterial;
            _chunks[i] = new HuskChunk()
            {
                VertList = new Vector3[_vertsPerChunk],
                NormList = new Vector3[_vertsPerChunk],
                TangentList = new float[_vertsPerChunk * 4],
                UvList = new Vector2[_vertsPerChunk],
                IndexList = new int[_indicesPerChunk],
                MeshInstance = mi,
            };
        }

        _currentChunk = 0;
    }

    public void AddHusk(Mesh mesh, Transform transform)
    {
        Array meshArray = mesh.SurfaceGetArrays(0);
        Vector3[] meshVerts = meshArray[(int) ArrayMesh.ArrayType.Vertex] as Vector3[];
        Vector3[] meshNormals = meshArray[(int) ArrayMesh.ArrayType.Normal] as Vector3[];
        float[] meshTangents = meshArray[(int) ArrayMesh.ArrayType.Tangent] as float[];
        Vector2[] meshUvs = meshArray[(int) ArrayMesh.ArrayType.TexUv] as Vector2[];
        int[] meshIndices = meshArray[(int) ArrayMesh.ArrayType.Index] as int[];
        
        // Do we need to switch to the next chunk?
        ref HuskChunk chunk = ref _chunks[_currentChunk];
        if (chunk.V + meshVerts.Length >= _vertsPerChunk || chunk.I + meshIndices.Length >= _indicesPerChunk)
        {
            _currentChunk = (_currentChunk + 1) % _maxChunks;
            chunk = ref _chunks[_currentChunk];
            chunk.V = 0;
            chunk.I = 0;
        }

        Transform normTrans = new Transform(transform.basis, Vector3.Zero);
        for (int v = 0; v < meshVerts.Length; v++)
        {
            chunk.VertList[chunk.V + v] = transform.Xform(meshVerts[v]);
            chunk.NormList[chunk.V + v] = normTrans.Xform(meshNormals[v]);
            chunk.TangentList[chunk.V + ToTangentIndex(v, 0)] = meshTangents[ToTangentIndex(v, 0)];
            chunk.TangentList[chunk.V + ToTangentIndex(v, 1)] = meshTangents[ToTangentIndex(v, 1)];
            chunk.TangentList[chunk.V + ToTangentIndex(v, 2)] = meshTangents[ToTangentIndex(v, 2)];
            chunk.TangentList[chunk.V + ToTangentIndex(v, 3)] = meshTangents[ToTangentIndex(v, 3)];
            chunk.UvList[chunk.V + v] = meshUvs[v];
        }

        for (int i = 0; i < meshIndices.Length; i++)
        {
            chunk.IndexList[chunk.I + i] = meshIndices[i] + chunk.V;
        }

        chunk.V += meshVerts.Length;
        chunk.I += meshIndices.Length;

        Span<Vector3> verts = chunk.VertList.AsSpan(0, chunk.V);
        Span<Vector3> normals = chunk.NormList.AsSpan(0, chunk.V);
        Span<float> tangents = chunk.TangentList.AsSpan(0, chunk.V * 4);
        Span<Vector2> uvs = chunk.UvList.AsSpan(0, chunk.V);
        Span<int> indices = chunk.IndexList.AsSpan(0, chunk.I);
        
        Array arrays = new();
        arrays.Resize((int) ArrayMesh.ArrayType.Max);
        arrays[(int) ArrayMesh.ArrayType.Vertex] = verts.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Normal] = normals.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Tangent] = tangents.ToArray();
        arrays[(int) ArrayMesh.ArrayType.TexUv] = uvs.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Index] = indices.ToArray();

        ArrayMesh outMesh = new();
        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        chunk.MeshInstance.Mesh = outMesh;
    }

    private int ToTangentIndex(int i, int t)
    {
        return i * 4 + t;
    }
}
