using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using GodotOnReady.Attributes;
using ImGuiNET;
using Array = Godot.Collections.Array;
using Color = Godot.Color;

public partial class FlowFieldCreator : Spatial
{
    [OnReadyGet] private MeshInstance _mesh;
    [OnReadyGet] private Camera _camera;
    [OnReadyGet] private FileDialog _dialogSave;
    [OnReadyGet] private FileDialog _dialogLoad;
    
    private Vector2 _fieldSize = new(64, 64);
    private float _cellSize = 14.0f;
    private float _brushSize = 3.0f;
    
    private Vector2[,] _vectors;

    private bool _wasMousePressed;
    private Vector2 _mousePosLast;
    private Vector2 _mouseMoveVector;
    private bool _saving;
    
    private List<Vector3> _vertList = new();
    private List<Color> _colList = new();
    private List<int> _indexList = new();

    [OnReady] private void Ready()
    {
        base._Ready();

        _vectors = new Vector2[(int)_fieldSize.x, (int)_fieldSize.y];
        
        Vector2 size = GetViewport().Size;
        _cellSize = size.y / _fieldSize.y;
        
        _camera.Size = size.y;
        _camera.GlobalTransform = new Transform(_camera.GlobalTransform.basis, new Vector3(_fieldSize.x * _cellSize * 0.5f, 10.0f, size.y * 0.5f));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (_saving)
        {
            
        }

        DrawVectors();
    }

    private Vector2 MousePos()
    {
        Vector3 mousePos3D = _camera.ProjectPosition(GetViewport().GetMousePosition(), 0.0f);
        return new Vector2(mousePos3D.x, mousePos3D.z);
    }
    
    public override void _Input(InputEvent evt)
    {
        if (ImGuiGD.ProcessInput(evt))
        {
            GetTree().SetInputAsHandled();
            return;
        }
        
        // process vectors
        Vector2 mousePos = MousePos();
        if (Input.IsActionPressed("flowfield_tool_mouse") && !_saving)
        {
            if (!_wasMousePressed)
            {
                _mousePosLast = mousePos;
                _wasMousePressed = true;
            }
            else
            {
                float smoothing = 0.5f;
                _mouseMoveVector = _mouseMoveVector * (1.0f - smoothing) + (mousePos - _mousePosLast).Normalized() * smoothing;
                Vector2 mC = mousePos / _cellSize;

                // set vector for all cells covered by brush.
                for (int x = (int) (mC.x - _brushSize * 0.5f); x < mC.x + _brushSize * 0.5f; x++)
                {
                    for (int y = (int) (mC.y - _brushSize * 0.5f); y < mC.y + _brushSize * 0.5f; y++)
                    {
                        if (x < 0 || x >= _fieldSize.x ||
                            y < 0 || y >= _fieldSize.y)
                            continue;
                        
                        if ((new Vector2(x, y) - mousePos / _cellSize).Length() > _brushSize * 0.5f)
                            continue;
                        
                        _vectors[x, y] = _mouseMoveVector.Normalized();
                    }
                }
                _mousePosLast = mousePos;
            }
        }
        else
        {
            _wasMousePressed = false;
        }
    }

    private void DrawVectors()
    {
        ArrayMesh outMesh = new();
        int v = 0;

        // flow field
        for (int x = 0; x < _fieldSize.x; x++)
        {
            for (int y = 0; y < _fieldSize.y; y++)
            {
                Color lineCol = Color.Color8(30, 30, 30);
                Line(new Vector2(x - 0.5f, y - 0.5f) * _cellSize, new Vector2(x + 0.5f, y - 0.5f) * _cellSize, lineCol, -1.0f, ref v);
                Line(new Vector2(x - 0.5f, y - 0.5f) * _cellSize, new Vector2(x - 0.5f, y + 0.5f) * _cellSize, lineCol, -1.0f, ref v);
                
                if (_vectors[x, y] == Vector2.Zero)
                    continue;
                
                Vector2 dir = _vectors[x, y];
                Color col = new(
                    Mathf.Lerp(0.0f, 1.0f, dir.x * 0.5f + 0.5f),
                    Mathf.Lerp(0.0f, 1.0f, dir.y * 0.5f + 0.5f),
                    Mathf.Lerp(0.0f, 1.0f, 1.0f - (dir.y * 0.5f + 0.5f)));
                Vector2 pos = new Vector2(x + 0.5f, y + 0.5f) * _cellSize;
                Vector2 end = pos + dir * _cellSize;
                Line(pos, end, col, 0.0f, ref v);
                Circle(pos, 4, 1.0f, Color.Color8(225, 225, 225), 1.0f, ref v);
            }
        }
        
        // brush
        Color brushCol = Color.Color8(100, 100, 100);
        Circle(MousePos(), 32, _brushSize * _cellSize * 0.5f, brushCol, 1.0f, ref v);
        
        Array arrays = new();
        arrays.Resize((int) ArrayMesh.ArrayType.Max);
        arrays[(int) ArrayMesh.ArrayType.Vertex] = _vertList;
        arrays[(int) ArrayMesh.ArrayType.Color] = _colList;
        arrays[(int) ArrayMesh.ArrayType.Index] = _indexList;
        
        if (v != 0)
        {
            outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        }
        
        _vertList.Clear();
        _colList.Clear();
        _indexList.Clear();

        _mesh.Mesh = outMesh;
    }

    private void SaveFlowField()
    {
        _saving = true;
        _dialogSave.PopupCentered();
        _dialogSave.Connect("file_selected", this, nameof(_on_FileDialogSave_file_selected));
    }
    
    private void LoadFlowField()
    {
        _saving = true;
        _dialogLoad.PopupCentered();
        _dialogLoad.Connect("file_selected", this, nameof(_on_FileDialogLoad_file_selected));
    }

    public void _on_FileDialogSave_file_selected(string path)
    {
        CSharpScript script = ResourceLoader.Load<CSharpScript>("res://src/tools/FlowFieldResource.cs");
        FlowFieldResource res = script.New() as FlowFieldResource;
        res.X = (int) _fieldSize.x;
        res.Y = (int) _fieldSize.y;
        res.Vectors = new Vector2[res.X * res.Y];
        for (int x = 0; x < res.X; x++)
        {
            for (int y = 0; y < res.Y; y++)
            {
                res.Vectors[y * res.X + x] = _vectors[x,y];
            }
        }
        string ext = path.Contains(".tres") ? "" : ".tres";
        GD.Print(ResourceSaver.Save($"{path}{ext}", res));
        
        _saving = false;
    }
    
    public void _on_FileDialogLoad_file_selected(string path)
    {
        FlowFieldResource res = ResourceLoader.Load<FlowFieldResource>(path);
        _fieldSize = new Vector2(res.X, res.Y);
        _vectors = new Vector2[res.X, res.Y];
        for (int x = 0; x < res.X; x++)
        {
            for (int y = 0; y < res.Y; y++)
            {
                _vectors[x, y] = res.Vectors[y * res.X + x];
            }
        }
        
        _saving = false;
    }
    
    private void Line(Vector2 p1, Vector2 p2, Color col, float z, ref int v)
    {
        _colList.Add(col);
        _vertList.Add(new Vector3(p1.x, z, p1.y));
        _colList.Add(col);
        _vertList.Add(new Vector3(p2.x, z, p2.y));
        _indexList.Add(v++);
        _indexList.Add(v++);
    }
    
    private void Circle(Vector2 pos, int segments, float radius, Color col, float z, ref int v)
    {
        for (int s = 0; s < segments; s++)
        {
            _colList.Add(col);
            float rad = Mathf.Pi * 2.0f * ((float) s / segments);
            Vector3 vert = (pos.To3D() + new Vector3(Mathf.Sin(rad), 0.0f, Mathf.Cos(rad)) * radius);
            vert += Vector3.Up * z;
            _vertList.Add(vert);
            _indexList.Add(v + s);
            _indexList.Add(v + (s + 1) % segments);
        }
        v += segments;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.DrawImGui += _OnImGuiLayout;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.DrawImGui -= _OnImGuiLayout;
    }

    private void _OnImGuiLayout()
    {
        if (ImGui.BeginTabItem("FlowField"))
        {
            ImGui.Text("Modify");
            ImGui.SliderFloat("Brush size", ref _brushSize, 1.0f, 16.0f);
            if (ImGui.Button("Clear"))
            {
                _vectors = new Vector2[(int) _fieldSize.x, (int) _fieldSize.y];
            }
            ImGui.Text("File");
            if (ImGui.Button("Open"))
            {
                LoadFlowField();
            }
            if (ImGui.Button("Save As"))
            {
                SaveFlowField();
            }
            ImGui.EndTabItem();
        }
    }
}
