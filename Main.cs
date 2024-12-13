using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

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

    // Rolls use a DND-styled randomizer. A Vector2 that has (3, 8) is a roll of 3d8.
    private readonly Vector2 NONE_ROLL = new Vector2( 2, 8 );
    private readonly Vector2 GOOD_ROLL = new Vector2( 3, 2 );
    private readonly Vector2 BAD_ROLL = new Vector2( 2, 3 );
    private readonly Vector2 SHOP_ROLL = new Vector2( 2, 2 );
    private readonly Vector2 RANDOM_TELE_ROLL = new Vector2(1, 3);

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

    private int DNDRoll( Vector2 dice )
    {
        int result = 0;
        for ( int i = 0; i < dice.x; i++ )
            result += RNG.RandiRange( 1, (int)dice.y );
        return result;
    }
    private int DNDRoll( int quantity, int die ) { return DNDRoll( new Vector2(quantity, die) ); }

    private Vector2 GetRandomOpenCoord()
    {
        Vector2 result;
        do
            // Using DND rolls encourages rooms to appear in the center.
            result = new Vector2( DNDRoll(4, 3)-8, DNDRoll(4, 3)-8 );
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
        IndexInput.Value++;

        // Clear existing elements on the map, then setup for a new batch.
        foreach( Node connector in ConnectorLayer.GetChildren() )
            connector.QueueFree();
        foreach( Node room in RoomLayer.GetChildren() )
            room.QueueFree();
        Rooms = new Dictionary<Vector2, Room>();

/*
        Due to the nature of the random coordinate favoring center tiles,
        rooms that are generated near the end are more likely to appear on
        the outskirts of the board.
*/
        Vector2 randCoord;

        // None, Good, and Bad rooms
        for( int i = 0; i < DNDRoll(NONE_ROLL); i++ )
        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "none" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }

        for( int i = 0; i < DNDRoll(SHOP_ROLL); i++ )
        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "shop" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }

        for( int i = 0; i < DNDRoll(GOOD_ROLL); i++ )
        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "good" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }

        for( int i = 0; i < DNDRoll(BAD_ROLL); i++ )
        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "bad" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }

        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "start" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }
        
        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "goal" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }

        for( int i = 0; i < DNDRoll(RANDOM_TELE_ROLL); i++ )
        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "teleport_random" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }

        for ( int i = 0; i < 2; i++ )
        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "teleport_pair_a" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }

        for ( int i = 0; i < 2; i++ )
        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "teleport_pair_b" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }
        
        {
            randCoord = GetRandomOpenCoord();
            var room = CreateRoom( randCoord, "shadow_realm" );
            Rooms[ room.Coordinates ] = room;
            RoomLayer.AddChild( room );
        }
    }

    private void OnGenerateButtonPressed()
    {
        PopulateMap();
    }
}
