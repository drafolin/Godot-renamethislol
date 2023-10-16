using System;
using Godot;

namespace Test12.Prefabs.Ennemy.Gunner;

public partial class Bullet: RigidBody3D
{
    private KinematicCollision3D _lastCollision;
    private CsgCylinder3D _shape;
    private CollisionShape3D _collider;
    public float Damage;
    
    public Bullet(Vector3 force)
    {
        LinearVelocity = force;
        GravityScale = 0;
    }

    public override void _Ready()
    {
        base._Ready();
        
        AddCollisionExceptionWith(GetParent<Node3D>());

        _collider = new CollisionShape3D();
        var colliderShape = new CylinderShape3D();
        _collider.Shape = colliderShape;
        AddChild(_collider);
            
        _shape = new CsgCylinder3D();
        _shape.Basis = Basis.Identity with
        {
            X = LinearVelocity.Rotated(Vector3.Up, (float)(Math.Tau/4)).Normalized(),
            Y = LinearVelocity.Normalized(),
            Z = LinearVelocity.Rotated(Vector3.Right, (float)(Math.Tau/4)).Normalized()
        };
        var bulletMaterial = new StandardMaterial3D();
        bulletMaterial.AlbedoColor = Colors.Black;
        _shape.Material = bulletMaterial;
        _collider.AddChild(_shape);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        _lastCollision = MoveAndCollide(LinearVelocity * (float)delta);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_lastCollision is null) return;

        GD.Print("I just touched something!");
        
        if (_lastCollision.GetCollider() is Player.Player player)
        {
            GD.Print("and it's a player!");
            player.Damage(Damage);
        }
        
        QueueFree();
    }
}