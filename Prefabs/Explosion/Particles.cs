using Godot;

namespace Test12.Prefabs.Explosion;

public partial class Particles : GpuParticles3D
{
	public Node3D Player;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var pivot = Player.GetNode<Node3D>("Pivot");
		
		GlobalTransform = GlobalTransform with
		{
			Basis = new Basis(
						pivot.GlobalTransform.Basis.X.Normalized(),
						Vector3.Up,
						pivot.GlobalTransform.Basis.Z.Normalized()
					)
		};
	}
}