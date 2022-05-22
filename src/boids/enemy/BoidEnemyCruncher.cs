using Godot;

public class BoidEnemyCruncher : BoidEnemyBase
{
    private Skeleton _skeleton;
    private MeshInstance _tail;
    private float _time;
    private float headingDelta;
    
    public override void _Ready()
    {
        base._Ready();

        _skeleton = GetNode<Skeleton>("cruncher/Armature/Skeleton");
        _tail = GetNode<MeshInstance>("cruncher/Armature/Skeleton/Circle");

        _tail.SetSurfaceMaterial(0, _mesh.GetSurfaceMaterial(0));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        _time += delta;
        
        for (int i = 0; i < _skeleton.GetBoneCount(); i++)
        {
            float freq = 5.0f;
            float amp = 1.0f;
            float wave = Mathf.Sin(_time * freq + i * amp);
            float strength = ((float)i / _skeleton.GetBoneCount()) * 0.5f;
            _skeleton.SetBonePose(i, new Transform(new Basis(Vector3.Forward, wave * strength), Vector3.Zero));
        }
    }
}