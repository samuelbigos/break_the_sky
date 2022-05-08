using System;
using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

public class CommandCentreDeploy : Node
{
    [Export] private NodePath _droneSelectSpatialPath;
    [Export] private Vector3 _droneSelectSpacing;
    [Export] private Material _droneMaterial;
    [Export] private float _droneScale = 1.0f;
    [Export] private float _droneSelectedHeightDelta = 0.5f;
    [Export] private float _droneRotateSpeed = 3.0f;
    [Export] private float _droneActiveSpeed = 5.0f;

    private Spatial _droneSelectSpatial;

    private List<DataAllyBoid> _droneList = new List<DataAllyBoid>();
    private List<MeshInstance> _droneMeshes = new List<MeshInstance>();
    private MeshInstance _droneHovered;

    public override void _Ready()
    {
        base._Ready();

        _droneSelectSpatial = GetNode<Spatial>(_droneSelectSpatialPath);

        int i = 0;
        foreach (DataAllyBoid drone in Database.AllyDrones.GetAllEntries<DataAllyBoid>())
        {
            _droneList.Add(drone);
            
            MeshInstance meshInstance = new MeshInstance();
            meshInstance.Mesh = drone.Mesh;
            meshInstance.SetSurfaceMaterial(0, _droneMaterial);
            meshInstance.Scale = new Vector3(_droneScale, _droneScale, _droneScale);
            AddChild(meshInstance);
            meshInstance.GlobalTransform = _droneSelectSpatial.GlobalTransform.Translated(_droneSelectSpacing * i);

            Area area = new Area();
            CollisionShape collisionShape = new CollisionShape();
            SphereShape sphere = new SphereShape();
            sphere.Radius = 2.0f;
            meshInstance.AddChild(area);
            area.AddChild(collisionShape);
            area.Transform = Transform.Identity;
            collisionShape.Shape = sphere;
            collisionShape.Transform = Transform.Identity;
            
            area.Connect("mouse_entered", this, nameof(_OnDroneMouseEntered), new Array(){i});
            area.Connect("mouse_exited", this, nameof(_OnDroneMouseExited), new Array(){i});
            area.Connect("input_event", this, nameof(_OnDroneInputEvent), new Array(){i});
            
            _droneMeshes.Add(meshInstance);
            i++;
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (_droneHovered != null)
            _droneHovered.Rotation += Vector3.Up * delta * _droneRotateSpeed;

        // highlight active drones
        for (int i = 0; i < _droneMeshes.Count; i++)
        {
            MeshInstance droneMesh = _droneMeshes[i];
            float yBot = _droneSelectSpatial.GlobalTransform.Translated(_droneSelectSpacing * i).origin.y;
            float yTop = yBot + _droneSelectedHeightDelta;
            Vector3 pos = droneMesh.GlobalTransform.origin;
            bool active = SaveDataPlayer.Instance.ActiveDrones.Contains(_droneList[i].Name);
            float d = _droneActiveSpeed * delta;
            pos.y = active ? Mathf.Min(pos.y + d, yTop) : Mathf.Max(pos.y - d, yBot);
            droneMesh.GlobalTransform = new Transform(droneMesh.GlobalTransform.basis, pos);
            
            if (active && droneMesh != _droneHovered)
                droneMesh.Rotation += Vector3.Up * delta * _droneRotateSpeed;
        }
    }

    private void _OnDroneMouseEntered(int i)
    {
        if (SaveDataPlayer.Instance.ActiveDrones.Contains(_droneList[i].Name))
            return;
        
        _droneHovered = _droneMeshes[i];
    }
    
    private void _OnDroneMouseExited(int i)
    {
        if (SaveDataPlayer.Instance.ActiveDrones.Contains(_droneList[i].Name) || _droneHovered == null)
            return;
        
        _droneHovered.Rotation = Vector3.Zero;
        _droneHovered = null;
    }

    private void _OnDroneInputEvent(Node camera, InputEvent e, Vector3 position, Vector3 normal, int shape, int i)
    {
        if (e.IsPressed())
        {
            if (SaveDataPlayer.Instance.ActiveDrones.Contains(_droneList[i].Name))
            {
                SaveDataPlayer.Instance.ActiveDrones.Remove(_droneList[i].Name);
            }
            else
            {
                SaveDataPlayer.Instance.ActiveDrones.Add(_droneList[i].Name);
            }
            SaveManager.Instance.DoSave();
        }
    }
}
