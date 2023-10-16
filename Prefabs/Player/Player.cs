using System;
using Godot;
using Animation = Test12.Prefabs.CameraOverlay.Animation;
using Main = Test12.Scripts.UI.Menu.Main;
using Pancake = Test12.Prefabs.PancakeBomb.Pancake;

namespace Test12.Prefabs.Player;

public partial class Player : CharacterBody3D
{
    // Get the gravity from the project settings to be synced with RigidBody nodes.
    [Export] private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").As<float>();
    [Export] private Node3D _pivot;
    [Export] private Main _menu;
    [Export] private Camera3D _camera;
    [Export] private double _maxSpeed = 5.0f;
    [Export] private double _acceleration = 4.5f;
    [Export] private double _jumpVelocity = 4.5f;
    [Export] private double _airbornePenalty = 4.5f;
    [Export] private Node3D _throwingEnv;
    [Export] private PackedScene _pancake;
    [Export] private Node2D _healthBar;
    [Export] private double _gunDamage = 1;
    [Export] private Node3D _overlay;
    [Export] private float _meleeDamage = 1;
    [Export] private ShapeCast3D _meleeHitBox;
    [Export] private double _maxHealth = 50;
    [Export] private double _regeneration = .1;
    [Export] private double _regenCooldown = 5;
    private double _currentRegenCooldown;
    private double _health;
    private RayCast3D _floorDetector;
    private Animation _overlayAnimation;
    private Sprite2D _healthBarFill;

    private float _rotationX;
    private float _rotationY;
    
    private GpuParticles3D _bullet;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _floorDetector = GetNode<RayCast3D>("RayCast3D");
        _bullet = GetNode<GpuParticles3D>("Pivot/GPUParticles3D");
        _overlayAnimation = _overlay.GetNode<Animation>("Overlay");
        _meleeHitBox ??= GetNode<ShapeCast3D>("MeleeHitBox");
        _healthBarFill ??= _healthBar.GetNode<Sprite2D>("HealthFill");
        _health = _maxHealth;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (@event)
        {
            // modify accumulated mouse rotation
            case InputEventMouseMotion mouseMotion when Input.MouseMode != Input.MouseModeEnum.Visible:
            {
                _rotationX += mouseMotion.Relative.X / 360;
                _rotationY += mouseMotion.Relative.Y / -360;
                _rotationY = (float)Math.Clamp(_rotationY, -Math.Tau / 4, Math.Tau / 4);
                
                // reset rotation
                Transform = Transform with
                {
                    Basis = Basis.Identity with
                    {
                        Z = new Vector3((float)-Math.Sin(_rotationX), 0, (float)Math.Cos(_rotationX)),
                        Y = new Vector3(0, 1, 0),
                        X = new Vector3((float)Math.Cos(_rotationX), 0, (float)Math.Sin(_rotationX)),
                    }
                };

                _pivot.Transform = _pivot.Transform with
                {
                    Basis = Basis.Identity with
                    {
                        X = new Vector3(1, 0, 0),
                        Y = new Vector3(0, (float)Math.Cos(_rotationY), (float)Math.Sin(_rotationY)),
                        Z = new Vector3(0, (float)-Math.Sin(_rotationY), (float)Math.Cos(_rotationY))
                    }
                };
                
                break;
            }
            case InputEventKey eventKey when eventKey.IsActionPressed("bomb"):
                var newBomb = _pancake.Instantiate<Pancake>();
                newBomb.LinearVelocity = _pivot.GlobalTransform.Basis.Z * -12;
                newBomb.Transform = newBomb.Transform with
                {
                    Origin = _pivot.GlobalTransform.Origin
                };
                newBomb.Set("_player", this);
                _throwingEnv.AddChild(newBomb);
                break;
            case InputEventKey eventKey when eventKey.IsActionPressed("ui_cancel"):
                if(_menu.Get("visible").AsBool())
                    _menu.Close();
                else
                    _menu.Open();
                return;
            case not null when @event.IsActionPressed("melee"):
                Melee();
                break;
            case InputEventMouseButton when Input.MouseMode == Input.MouseModeEnum.Visible:
                return;
            case InputEventMouseButton eventMb when eventMb.IsActionPressed("shoot") :
                Shoot();
                break;
            case InputEventMouseButton eventMb when eventMb.IsActionPressed("aim"):
                Aim();
                break;
            case InputEventMouseButton eventMb when eventMb.IsActionReleased("aim"):
                Aim(false);
                break;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        Regen(delta);
        GD.Print(_health);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.MouseMode == Input.MouseModeEnum.Visible)
        {
            return;
        }
        
        Velocity += Vector3.Down * _gravity * (float)delta;
        
        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            Velocity += Vector3.Up * (float)_jumpVelocity;

        var isSprinting = Input.IsActionPressed("sprint");
        
        var inputDir = Input.GetVector("Strf L", "Strf R", "fwd", "bwd");
        var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        var floor = _floorDetector.GetCollider();
        double floorFriction = 10;
        if (floor is not null) 
            floorFriction = floor.GetMeta("frictionFactor", 10).AsDouble();

        var xzMask = new Vector3(1, 0, 1);
        var xzVelocity = Velocity * xzMask;

        var maxSpeed = (float)_maxSpeed;
        var acceleration = _acceleration;

        if (isSprinting)
        {
            maxSpeed *= 2;
            acceleration *= 2;
        }

        if (!IsOnFloor())
        {
            acceleration /= _airbornePenalty;
        }
        
        var movementAcceleration = xzVelocity.MoveToward(
            direction.Normalized() * xzMask * maxSpeed, 
            (float)(delta * acceleration * floorFriction));
        
        Velocity += movementAcceleration - xzVelocity;

        MoveAndSlide();
    }

    private void Shoot()
    {
        _bullet.Restart();
        var spaceState = GetWorld3D().DirectSpaceState;

        var from = _camera.ProjectRayOrigin(_camera.GetViewport().GetVisibleRect().GetCenter());
        var to = from + _camera.ProjectRayNormal(_camera.GetViewport().GetVisibleRect().GetCenter()) * 400;

        var result = spaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from, to));

        if (result.Count <= 0) return;
        
        var target = result["collider"].AsGodotObject();
        
        if (target is not Ennemy.Ennemy ennemy) return;
        ennemy.Damage((float)_gunDamage);
        ennemy.Push((to - from).Normalized() * 10);
    }

    private void Aim(bool zIn = true)
    {
        if (zIn)
            _camera.Fov /= 2;
        else
            _camera.Fov *= 2;
    }

    private void Melee()
    {
        if (_overlayAnimation.IsPlaying()) return;
        Ennemy.Ennemy firstEnemy = null;

        for (var i = 0; i < _meleeHitBox.GetCollisionCount(); i++)
        {
            var collided = _meleeHitBox.GetCollider(i);
            if (collided is not Ennemy.Ennemy enemy) continue;
            firstEnemy ??= enemy;
            enemy.Damage(_meleeDamage);
            var toEnemyV = enemy.GlobalTransform.Origin - GlobalTransform.Origin;
            enemy.Push(toEnemyV * 2f + Vector3.Up * 2f);
        }


        if (firstEnemy is not null)
        {
            _overlayAnimation.Play("melee_hit");
            var toFirstEnemyVector = GlobalTransform.Origin - firstEnemy.GlobalTransform.Origin;

            GlobalTransform = GlobalTransform with
            {
                Basis = GlobalTransform.Basis with
                {
                    X = toFirstEnemyVector.Rotated(Vector3.Up, (float)Math.Tau / 4).Normalized(),
                    Z = toFirstEnemyVector.Normalized()
                }
            };
        }
        else
        {
            _overlayAnimation.Play("melee");
        }
    }

    public void Damage(float dmg)
    {
        _health -= dmg;
        _currentRegenCooldown = _regenCooldown;
        UpdateHealthBar();
        
        if (_health < 0)
        {
            GetTree().Quit();
        }
    }

    private void Regen(double delta)
    {
        if (_currentRegenCooldown > 0)
            _currentRegenCooldown -= delta;
        else if (_health < _maxHealth)
            _health += _regeneration * delta;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        _healthBarFill.RegionRect = _healthBarFill.RegionRect with
        {
            Size = _healthBarFill.RegionRect.Size with
            {
                X = (float)_health
            }
        };

        if (_health < 10)
        {
            var texture = (GradientTexture1D)_healthBarFill.Texture;
            texture.Gradient.SetColor(0, Colors.Red);
            texture.Gradient.SetColor(1, Colors.Red);
        }
        else
        {
            var texture = (GradientTexture1D)_healthBarFill.Texture;
            texture.Gradient.SetColor(0, Colors.White);
            texture.Gradient.SetColor(1, Colors.White);
        }
    }
}