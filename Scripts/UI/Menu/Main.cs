using Godot;

namespace Test12.Scripts.UI.Menu;

public partial class Main : Node2D
{
    [Export] private Node3D _world;
    
    public override void _Ready()
    {
        Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventKey eventKey when eventKey.IsActionPressed("Menu"):
                Close();
                break;
        }
    }

    public void Close()
    {
        Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetTree().Paused = false;
    }

    public void Open()
    {
        Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = true;
    }
}