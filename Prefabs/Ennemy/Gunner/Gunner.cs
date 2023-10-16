using System;
using Godot;

namespace Test12.Prefabs.Ennemy.Gunner;

public partial class Gunner : Ennemy
{
    [Export] protected double BulletSpeed = 15;
    private double _secondCounter;
    public override void _Process(double delta)
    {
        base._Process(delta);

        _secondCounter += delta;
        if (_secondCounter < 1) return;
        _secondCounter = 0;
        Shoot();
    }

    private void Shoot()
    {
        var bullet = new Bullet((Player.GlobalTransform.Origin - GlobalTransform.Origin).Normalized() * (float)BulletSpeed);
        bullet.Damage = DamageValue;
        AddChild(bullet);
    }
}