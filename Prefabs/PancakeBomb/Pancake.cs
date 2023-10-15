using Godot;
using Test12.Prefabs.Explosion;

namespace Test12.Prefabs.PancakeBomb;

public partial class Pancake: RigidBody3D
{
    private double _destroyIn;
    private bool _startCountDown;
    [Export] private CharacterBody3D _player;
    [Export] private NavigationObstacle3D _obstacle;
    [Export] private Particles _explosionParticles;
    [Export] private ShapeCast3D _killCast;
    [Export] private CollisionShape3D _collider;
    
    public override void _Ready()
    {
        base._Ready();
        _explosionParticles ??= GetNode<Particles>("GPUParticles3D");
        _killCast ??= GetNode<ShapeCast3D>("killArea");
        _collider ??= GetNode<CollisionShape3D>("CollisionShape3D");
        _explosionParticles.Player = _player;
        
        var mapRid = NavigationServer3D.GetMaps()[0];
        NavigationServer3D.ObstacleSetMap(_obstacle.GetRid(), mapRid);
        NavigationServer3D.ObstacleSetAvoidanceEnabled(_obstacle.GetRid(), true);
    }

    public void Explode()
    {
        _obstacle.AvoidanceEnabled = false;
        _obstacle.QueueFree();
        _explosionParticles.Emitting = true;
        _destroyIn = _explosionParticles.Lifetime;
        _startCountDown = true;
        _collider.Visible = false;
        _collider.Disabled = true;
        for (var i = 0; i < _killCast.GetCollisionCount(); i++)
        {
            var explodedCollider = _killCast.GetCollider(i);
            if (explodedCollider is Ennemy.Ennemy ennemy)
            {
                ennemy.Die();
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_startCountDown)
        {
            _destroyIn -= delta;
        }

        if (_destroyIn < 0)
        {
            QueueFree();
        }
    }
}