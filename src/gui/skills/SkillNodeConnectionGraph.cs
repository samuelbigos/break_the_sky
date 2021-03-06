using Godot;
using Godot.Collections;

[Tool]
public class SkillNodeConnectionGraph : Control
{
    public override void _Process(float delta)
    {
        base._Process(delta);
        
        Update();
    }

    public override void _Draw()
    {
        base._Draw();

        for (int i = 0; i < GetParent().GetChildCount(); i++)
        {
            Node node = GetParent().GetChild<Node>(i);
            if (node is SkillNode skillNode1)
            {
                foreach (NodePath path1 in skillNode1.Connections)
                {
                    if (GetNode<Control>(path1) is SkillNode skillNode2)
                    {
                        // only draw connections if it goes both ways.
                        bool found = false;
                        foreach (NodePath path2 in skillNode2.Connections)
                        {
                            if (GetNode<Control>(path2) == skillNode1)
                                found = true;
                        }
                        if (found)
                            DrawLine(skillNode1.RectPosition + skillNode1.RectSize * 0.5f, 
                            skillNode2.RectPosition + skillNode2.RectSize * 0.5f, Colors.Black, 5.0f);
                    }
                }
            }
        }
    }
}
