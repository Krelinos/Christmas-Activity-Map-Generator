using Godot;
using System;
using System.Collections.Generic;

public class Main : Node2D
{
    // Rooms are organized onto a 2D grid.
    private Dictionary< Vector2, Room > Rooms;

    private String Seed
    {
        get => SeedInput.Text;
    }


    private readonly RandomNumberGenerator RNG = new RandomNumberGenerator();

    private LineEdit SeedInput;
    private SpinBox IndexInput;
    private Button GenerateButton;
    private Node ConnectorLayer;
    private Node RoomLayer;

    private PackedScene _Room;

    public override void _Ready()
    {
        SeedInput = GetNode<LineEdit>("Toolbar/HBoxContainer/Seed");
        IndexInput = GetNode<SpinBox>("Toolbar/HBoxContainer/Index");
        GenerateButton = GetNode<Button>("Toolbar/HBoxContainer/Button");

        ConnectorLayer = GetNode("Origin/Connectors");
        RoomLayer = GetNode("Origin/Rooms");

        _Room = GD.Load("res://Scenes/Room.tscn") as PackedScene;
        
        RNG.Seed = Seed.Hash();
        RNG.State = (ulong)IndexInput.Value;

        GenerateButton.Connect( "pressed", this, nameof(OnGenerateButtonPressed) );
    }

    private Vector2 GetRandomOpenCoord()
    {
        Vector2 result;
        do
            result = new Vector2( RNG.RandiRange(-4, 4), RNG.RandiRange(-4, 4) );
        while ( Rooms.ContainsKey( result ) );

        return result;
    }

    private Room CreateRoom( Vector2 coord, String type )
    {
        if ( Rooms.ContainsKey( coord ) )
        {
            GD.PushError("Tried to generate a room at (" + coord + "), but one already exists there.");
            return null;
        }

        var room = _Room.Instance() as Room;
        room.Coordinates = coord;
        room.Type = type;

        return room;
    }

    private void PopulateMap()
    {
        RNG.State = (ulong)IndexInput.Value;

        // Clear existing elements on the map, then setup for a new batch.
        foreach( Node connector in ConnectorLayer.GetChildren() )
            connector.QueueFree();
        foreach( Node room in RoomLayer.GetChildren() )
            room.QueueFree();
        Rooms = new Dictionary<Vector2, Room>();

        // Every map has a max of one Start and one Goal.
        Vector2 randCoord = GetRandomOpenCoord();
        var start = CreateRoom( randCoord, "start" );
        Rooms[ start.Coordinates ] = start;

        randCoord = GetRandomOpenCoord();
        var goal = CreateRoom( randCoord, "goal" );
        Rooms[ goal.Coordinates ] = goal;

        RoomLayer.AddChild( start );
        RoomLayer.AddChild( goal );

        IndexInput.Value++;
    }

    private void OnGenerateButtonPressed()
    {
        PopulateMap();
    }
}
