using System;
using Godot;

namespace Test12.Prefabs.Ennemy.Gunner;

public partial class Gunner : Ennemy
{
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
        var bullet = new Bullet((Player.GlobalTransform.Origin - GlobalTransform.Origin).Normalized() * 3);
        bullet.Damage = DamageValue;
        AddChild(bullet);
    }
}