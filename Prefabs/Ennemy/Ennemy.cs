using System;
using System.Linq;
using Godot;
using Test12.Prefabs.Explosion;
using CPlayer = Test12.Prefabs.Player.Player;

namespace Test12.Prefabs.Ennemy;
public partial class Ennemy: CharacterBody3D
{
    protected struct Runnable
    {
        public Action Callback { get; }
        public double CountDown { get; private set; }

        public Runnable(double time, Action action)
        {
            Callback = action;
            CountDown = time;
        }

        public void Decrement(double delta)
        {
            CountDown -= delta;
        }
    }

    [Export] public CPlayer Player;
    [Export] protected Sprite3D HpBar;
    
    // Get the gravity from the project settings to be synced with RigidBody nodes.
    [Export] protected double Gravity = (double)ProjectSettings.GetSetting("physics/3d/default_gravity");
    [Export] protected double Acceleration = 4;
    [Export] protected double MaxSpeed = 5.3;
    [Export] protected float AirbornePenalty = 4f;
    [Export] protected Particles ExplosionParticles;
    [Export] protected CollisionShape3D Collider;
    [Export] protected double MaxHealth = 2;
    [Export] protected RayCast3D FloorDetector;
    [Export] protected double Resistance = 1;
    [Export] protected float DamageValue = .05f;
    protected double Health;
    protected NavigationAgent3D NavigationAgent;
    protected Runnable[] DeferredActions = Array.Empty<Runnable>();
    protected Vector3 PreviousSafeVelocity = Vector3.Zero;

    private Vector3 MovementTarget
    {
        get => NavigationAgent.TargetPosition;
        set => NavigationAgent.TargetPosition = value;
    }

    public override void _Ready()
    {
        ExplosionParticles ??= GetNode<Particles>("GPUParticles3D");
        ExplosionParticles.Player = Player;
        NavigationAgent ??= GetNode<NavigationAgent3D>("NavigationAgent3D");
        FloorDetector ??= GetNode<RayCast3D>("FloorDetector");
        HpBar ??= GetNode<Sprite3D>("Sprite3D");
        Collider ??= GetNode<CollisionShape3D>("CollisionShape3D");
        Health = MaxHealth;
        Callable.From(ActorSetup).CallDeferred();
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity += Vector3.Down * (float)Gravity * (float)delta;

        if (NavigationAgent.IsNavigationFinished())
        {
            return;
        }

        var floor = FloorDetector.GetCollider();
        double floorFriction = 10;
        if (floor is not null) 
            floorFriction = floor.GetMeta("frictionFactor", 10).AsDouble();
        
        var currentAgentPosition = GlobalTransform.Origin;
        var nextPathPosition = NavigationAgent.GetNextPathPosition();
        
        var xzMask = new Vector3(1, 0, 1);
        var xzVelocity = Velocity * xzMask;       
        var movementDirection = (nextPathPosition - currentAgentPosition).Normalized();
        
        var movementAcceleration = xzVelocity.MoveToward(
            movementDirection.Normalized() * xzMask * (float)MaxSpeed, 
            (float)(delta * Acceleration * floorFriction / AirbornePenalty));
        
        Velocity += movementAcceleration - xzVelocity;
        
        MoveAndSlide();
    }

    public override void _Process(double delta)
    {
        for (var i = 0; i < DeferredActions.Length; i++)
        {
            DeferredActions[i].Decrement(delta);
            if (DeferredActions[i].CountDown < 0)
            {
                DeferredActions[i].Callback();
            }
        }
        
        MovementTarget = Player.GlobalTransform.Origin;

        var toPlayerV = Player.GlobalTransform.Origin - GlobalTransform.Origin;
        
        HpBar.GlobalTransform = HpBar.GlobalTransform with
        {
            Basis = HpBar.GlobalTransform.Basis with
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
        Velocity -= PreviousSafeVelocity;
        PreviousSafeVelocity = safeVelocity;
        Velocity += PreviousSafeVelocity;
    }

    public void Damage(float dmg)
    {
        Health -= dmg / Resistance;
        HpBar.RegionRect = HpBar.RegionRect with
        {
            Size = HpBar.RegionRect.Size with
            {
                X = (float)(Health / MaxHealth) * HpBar.RegionRect.Size.X
            }
        };
        if (Health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        ExplosionParticles.Emitting = true;
        Collider.Visible = false;
        Collider.Disabled = true;
        HpBar.Visible = false;
        Gravity = 0;

        DeferredActions = DeferredActions.Append(new Runnable(ExplosionParticles.Lifetime, QueueFree)).ToArray();
    }

    public void Push(Vector3 force)
    {
        Velocity += force;
    }
}