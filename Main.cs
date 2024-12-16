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


    private static readonly RandomNumberGenerator RNG = new RandomNumberGenerator();

    private LineEdit SeedInput;
    private SpinBox IndexInput;
    private Button GenerateButton;
    private Node ConnectorLayer;
    private Node RoomLayer;

    private PackedScene _Room;
    private PackedScene _Connector;
    private PackedScene _ConnectorOneWay;

    // Rolls use a DND-styled randomizer. A Vector2 that has (3, 8) is a roll of 3d8.
    private readonly Vector2 NONE_ROLL = new Vector2( 2, 8 );
    private readonly Vector2 GOOD_ROLL = new Vector2( 3, 2 );
    private readonly Vector2 BAD_ROLL = new Vector2( 2, 3 );
    private readonly Vector2 SHOP_ROLL = new Vector2( 2, 2 );
    private readonly Vector2 RANDOM_TELE_ROLL = new Vector2(1, 3);
    private const float CONNECTOR_ONE_WAY_RATE = 0.2f;

    public override void _Ready()
    {
        SeedInput = GetNode<LineEdit>("Toolbar/HBoxContainer/Seed");
        IndexInput = GetNode<SpinBox>("Toolbar/HBoxContainer/Index");
        GenerateButton = GetNode<Button>("Toolbar/HBoxContainer/Button");

        ConnectorLayer = GetNode("Origin/Connectors");
        RoomLayer = GetNode("Origin/Rooms");

        _Room = GD.Load("res://Scenes/Room.tscn") as PackedScene;
        _Connector = GD.Load("res://Scenes/Connector.tscn") as PackedScene;
        _ConnectorOneWay = GD.Load("res://Scenes/ConnectorOneWay.tscn") as PackedScene;
        
        RNG.Seed = Seed.Hash();
        RNG.State = (ulong)IndexInput.Value;

        GenerateButton.Connect( "pressed", this, nameof(OnGenerateButtonPressed) );
    }

    public static int DNDRoll( Vector2 dice )
    {
        int result = 0;
        for ( int i = 0; i < dice.x; i++ )
            result += RNG.RandiRange( 1, (int)dice.y );
        return result;
    }
    public static int DNDRoll( int quantity, int die ) { return DNDRoll( new Vector2(quantity, die) ); }

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

        ConnectRooms();
    }

    private Room FindRoomToward( Room room, String direction )
    {
        var coordinates = room.Coordinates + Room.COMPASS_DIRECTION[ direction ];

        while( coordinates.x > -4 && coordinates.x < 4 && coordinates.y > -4 && coordinates.y < 4 )
        {
            if ( Rooms.ContainsKey( coordinates) )
                return Rooms[ coordinates ];
            coordinates += Room.COMPASS_DIRECTION[ direction ];
        }

        return null;
    }

    private void ConnectRooms( Room from, Room to, String direction, bool oneway = false )
    {
        var connector = oneway ? _ConnectorOneWay.Instance() as Line2D : _Connector.Instance() as Line2D;
        ConnectorLayer.AddChild( connector );
        connector.Points = new Vector2[] { from.Position, to.Position };

        from.ConnectedRooms[ direction ] = to;
        to.ConnectedRooms[ Room.COMPASS_OPPOSITE[direction] ] = from;
    }

    private void ConnectRooms()
    {
        for ( int y = -4; y <= 4; y++ )
            for ( int x = -4; x <= 4; x++ )
                if ( Rooms.ContainsKey( new Vector2(x, y) ) )
                {
                    var room = Rooms[ new Vector2(x, y) ];

                    // This is used so that each direction is considered only once, which also prevents
                    // an infinite loop where there is not enough rooms to satisfy the max amount.
                    var validDirections = new List<int>{ 0,1,2,3,4,5,6,7 };

                    while( room.RemainingConnectionSlots > 0 && validDirections.Count > 0 )
                    {
                        var directionIndex = validDirections[ RNG.RandiRange(0, validDirections.Count-1) ];
                        validDirections.Remove(directionIndex);

                        var direction = Room.COMPASS_INDEX[ directionIndex ];
                        
                        // Does the room already have a connection in that direction?
                        if ( room.ConnectedRooms.ContainsKey(direction) )
                            continue;
                        
                        var otherRoom = FindRoomToward( room, direction );
                        if ( otherRoom == null || otherRoom.RemainingConnectionSlots == 0 )
                            continue;
                        
                        // We can assume that if this direction is empty, then the equal opposite
                        // direction of the otherRoom is also empty.
                        ConnectRooms( room, otherRoom, direction, RNG.Randf() < CONNECTOR_ONE_WAY_RATE );
                    }
                }
    }

    private void OnGenerateButtonPressed()
    {
        PopulateMap();
    }
}
