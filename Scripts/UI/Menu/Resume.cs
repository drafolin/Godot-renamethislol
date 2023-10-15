using Godot;

namespace Test12.Scripts.UI.Menu;

public partial class Resume : Button
{
    [Export]
    private Main _menu;
    
    public override void _Pressed()
    {
        base._Pressed();
        _menu.Close();
    }
}