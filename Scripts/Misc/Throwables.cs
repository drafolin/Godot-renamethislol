using Godot;

namespace Test12.Scripts.Misc;

public partial class Throwables : Node3D
{
	public override void _Input(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventKey action when action.IsActionPressed("explode"):
				GD.Print("boom!");
				foreach (var child in GetChildren())
				{
					if (child is not Prefabs.PancakeBomb.Pancake pancake) return;
					pancake.Explode();
				}
				break;
		}
	}
}