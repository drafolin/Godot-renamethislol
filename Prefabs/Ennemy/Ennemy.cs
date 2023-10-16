using System;
using System.Linq;
using Godot;
using Test12.Prefabs.Explosion;
using CPlayer = Test12.Prefabs.Player.Player;

namespace Test12.Prefabs.Ennemy;

internal struct Runnable
{
    public Action Callback { get; }
    private double _timeLeft;
    public double CountDown => _timeLeft;

    public Runnable(double time, Action action)
    {
        Callback = action;
        _timeLeft = time;
    }

    public void Decrement(double delta)
    {
        _timeLeft -= delta;
    }
}

public partial class Ennemy: CharacterBody3D
{
    [Export] public CPlayer Player;
    [Export] public CsgBox3D Floor;
    [Export] private Sprite3D _hpBar;
    
    // Get the gravity from the project settings to be synced with RigidBody nodes.
    [Export] private double _gravity = (double)ProjectSettings.GetSetting("physics/3d/default_gravity");
    [Export] private double _acceleration = 4;
    [Export] private double _maxSpeed = 5.3;
    [Export] private Particles _explosionParticles;
    [Export] private CollisionShape3D _collider;
    [Export] private double _maxHealth = 2;
    private double _health;
    [Export] private double _resistance = 1;
    private NavigationAgent3D _navigationAgent;
    private Runnable[] _deferredActions = Array.Empty<Runnable>();

    private Vector3 MovementTarget
    {
        get => _navigationAgent.TargetPosition;
        set => _navigationAgent.TargetPosition = value;
    }

    public override void _Ready()
    {
        _explosionParticles ??= GetNode<Particles>("GPUParticles3D");
        _explosionParticles.Player = Player;
        _navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _health = _maxHealth;
        Callable.From(ActorSetup).CallDeferred();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.MouseMode == Input.MouseModeEnum.Visible)
        {
            return;
        }
        
        Velocity += Vector3.Down * (float)_gravity * (float)delta;

        if (_navigationAgent.IsNavigationFinished())
        {
            return;
        }

        var currentAgentPosition = GlobalTransform.Origin;
        var nextPathPosition = _navigationAgent.GetNextPathPosition();
        const float airBornePenalty = 1000f;
        
        var xzMask = new Vector3(1, 0, 1);
        var xzVelocity = Velocity * xzMask;       
        var movementDirection = (nextPathPosition - currentAgentPosition).Normalized();
        
        var movementAcceleration = xzVelocity.MoveToward(
            movementDirection.Normalized() * xzMask * (float)_maxSpeed, 
            (float)(delta * _acceleration * floorFriction));
        
        Velocity += movementAcceleration - xzVelocity;
        
        MoveAndSlide();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        GetSlideCollisionCount();
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            if (GetSlideCollision(i).GetCollider() is not CPlayer) continue;
            GD.Print("player");
            GetTree().Quit();
            return;
        }

        for (var i = 0; i < _deferredActions.Length; i++)
        {
            _deferredActions[i].Decrement(delta);
            if (_deferredActions[i].CountDown < 0)
            {
                _deferredActions[i].Callback();
            }
        }
        
        MovementTarget = Player.GlobalTransform.Origin;

        var toPlayerV = Player.GlobalTransform.Origin - GlobalTransform.Origin;
        
        _hpBar.GlobalTransform = _hpBar.GlobalTransform with
        {
            Basis = _hpBar.GlobalTransform.Basis with
            {
                X = toPlayerV.Rotated(Vector3.Up, (float)Math.Tau / 4).Normalized() * 0.082f,
                Y = Vector3.Up * 0.082f,
                Z = toPlayerV.Normalized() * 0.082f
            }
        };
    }

    private async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        // Now that the navigation map is no longer empty, set the movement target.
        MovementTarget = Player.GlobalTransform.Origin;
    }

    private void _OnNavigationAgent3dVelocityComputed(Vector3 safeVelocity)
    {
        Velocity += safeVelocity;
    }

    public void Damage(float dmg)
    {
        _health -= dmg / _resistance;
        _hpBar.RegionRect = _hpBar.RegionRect with
        {
            Size = _hpBar.RegionRect.Size with
            {
                X = (float)(_health / _maxHealth) * _hpBar.RegionRect.Size.X
            }
        };
        if (_health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _explosionParticles.Emitting = true;
        _collider.Visible = false;
        _collider.Disabled = true;
        _hpBar.Visible = false;
        _gravity = 0;

        _deferredActions = _deferredActions.Append(new Runnable(_explosionParticles.Lifetime, QueueFree)).ToArray();
    }

    public void Push(Vector3 force)
    {
        Velocity += force;
    }
}