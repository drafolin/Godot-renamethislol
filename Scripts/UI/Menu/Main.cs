using Godot;

namespace Test12.Scripts.UI.Menu;

public partial class Main : Node
{
    [Export] private Node3D _world;
    public override void _Ready()
    {
        Set("visible", false);
    }


    public void Close()
    {
        Set("visible", false);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public void Open()
    {
        Set("visible", true);
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}