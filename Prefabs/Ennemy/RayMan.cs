using System;
using Godot;
using CPlayer = Test12.Prefabs.Player.Player;

namespace Test12.Prefabs.Ennemy;

public partial class RayMan: Ennemy
{
    [Export] protected RayCast3D PlayerRay;
    [Export] protected CsgCylinder3D VisibleRay;
    public override void _Ready()
    {
        base._Ready();
        PlayerRay ??= GetNode<RayCast3D>("PlayerRay");
        VisibleRay ??= PlayerRay.GetNode<CsgCylinder3D>("Ray");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        var relativePlayerPos = Player.GlobalTransform.Origin - GlobalTransform.Origin;
        PlayerRay.TargetPosition = relativePlayerPos;

        VisibleRay.Transform = VisibleRay.Transform with
        {
            Origin = relativePlayerPos / 2
        };

        VisibleRay.GlobalTransform = VisibleRay.GlobalTransform with
        {
            Basis = VisibleRay.GlobalTransform.Basis with
            {
                X = relativePlayerPos.Rotated(Vector3.Up, (float)(Math.Tau/4)).Normalized(),
                Y = relativePlayerPos / 2,
                Z = relativePlayerPos.Rotated(Vector3.Right, (float)(Math.Tau/4)).Normalized()
            }
        };
        
        if (PlayerRay.IsColliding() && PlayerRay.GetCollider() is CPlayer player)
        {
            player.Damage(DamageValue);
            VisibleRay.Visible = true;
        }
        else
        {
            VisibleRay.Visible = false;
        }
    }

    protected override void Die()
    {
        base.Die();
        PlayerRay.Enabled = false;
    }
}