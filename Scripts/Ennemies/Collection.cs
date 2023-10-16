using Godot;
using System;
using Test12.Prefabs.Ennemy;
using Test12.Prefabs.Player;

namespace Test12.Scripts.Ennemies;

public partial class Collection : Node3D
{
	[Export] private Player _player;
	[Export] private PackedScene _enemyPrefab;
	[Export] private Label _hudPrompt;

	private double _secondCounter;
	private double _totalTime;
	private readonly Random _randomGen = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_player ??= GetNode<Player>("%player");
		Spawn();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_totalTime += delta;
		if (Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			return;
		}
		_secondCounter += delta;

		if (_secondCounter > new Random().NextDouble() * 10/ (_totalTime * .2))
		{
			_secondCounter = 0;
			if(GetChildren().Count <0)
				Spawn();
		}

		var enemiesRemaining = GetChildren().Count;

		_hudPrompt.Text = $"You survived for {_totalTime:00:00.00}\n" +
		                  $"{enemiesRemaining} enemies remaining";
	}

	private void Spawn()
	{
		var ennemy = _enemyPrefab.Instantiate<Ennemy>();
		ennemy.Player = _player;
		float x, z;
		var playerX = _player.GlobalTransform.Origin.X;
		var playerZ = _player.GlobalTransform.Origin.Z;
		do
		{
			x = (float)_randomGen.NextDouble() * 200 - 100;
			z = (float)_randomGen.NextDouble() * 200 - 100;
		} while (playerX + 10 > x && x > playerX - 10 && playerZ + 10 > z && z > playerZ - 10);
		ennemy.Transform = ennemy.Transform with
		{
			Origin = new Vector3(x, 2f, z)
		};
		AddChild(ennemy);
	}
}