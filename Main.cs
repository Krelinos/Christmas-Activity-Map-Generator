using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Xml;

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
    private Label SeedHint;
    private SpinBox IndexInput;
    private Button GenerateButton;
    private Button ControlsButton;
    private Control ControlsHints;
    private Control Toolbar;
    private Node ConnectorLayer;
    private Node RoomLayer;
    private Node TokenLayer;
    private UserControls UserControls;

    private Area2D MouseCollision;
    private PhysicsBody2D SelectedEntity;
    private Node2D SelectedEntityMarker;

    private PackedScene _Room;
    private PackedScene _Connector;
    private PackedScene _ConnectorOneWay;
    private PackedScene _Token;
    private ulong SavedRNGState;

    // Rolls use a DND-styled randomizer. A Vector2 that has (3, 8) is a roll of 3d8.
    private readonly Vector2 NONE_ROLL = new Vector2( 2, 8 );
    private readonly Vector2 GOOD_ROLL = new Vector2( 3, 2 );
    private readonly Vector2 BAD_ROLL = new Vector2( 2, 3 );
    private readonly Vector2 SHOP_ROLL = new Vector2( 2, 2 );
    private readonly Vector2 RANDOM_TELE_ROLL = new Vector2(1, 3);
    private const float CONNECTOR_ONE_WAY_RATE = 0.2f;
    private const String SEED_EMPTY_WARN = "Seed cannot be empty!";
    private const String SEED_CONFIRM = "Press enter after editing to seed to confirm.";

    public override void _Ready()
    {
        var toolbarPath = "Toolbar/VBoxContainer/HBoxContainer/";
        SeedInput = GetNode<LineEdit>(toolbarPath +"Seed");
        SeedHint = SeedInput.GetNode<Label>("Hint");
        IndexInput = GetNode<SpinBox>(toolbarPath + "Index");
        GenerateButton = GetNode<Button>(toolbarPath + "Button");
        ControlsButton = GetNode<Button>(toolbarPath + "ControlHintsButton");
        ControlsHints = GetNode<Control>("Toolbar/VBoxContainer/ControlHints");
        Toolbar = GetNode<Control>("Toolbar");

        ConnectorLayer = GetNode("Origin/Connectors");
        RoomLayer = GetNode("Origin/Rooms");
        TokenLayer = GetNode("Origin/Tokens");
        UserControls = GetNode<UserControls>("UserControls");

        MouseCollision = GetNode<Area2D>("MouseCollision");
        SelectedEntityMarker = GetNode<Node2D>("SelectedEntityMarker");

        _Room = GD.Load("res://Scenes/Room.tscn") as PackedScene;
        _Connector = GD.Load("res://Scenes/Connector.tscn") as PackedScene;
        _ConnectorOneWay = GD.Load("res://Scenes/ConnectorOneWay.tscn") as PackedScene;
        _Token = GD.Load("res://Scenes/Token.tscn") as PackedScene;
        
        RNG.Seed = Seed.Hash();
        SavedRNGState = RNG.State;

        SeedInput.Connect( "text_changed", this, nameof(OnSeedInputChanged) );
        SeedInput.Connect( "text_entered", this, nameof(OnSeedInputEntered) );
        GenerateButton.Connect( "pressed", this, nameof(OnGenerateButtonPressed) );
        ControlsButton.Connect( "pressed", this, nameof(OnControlsHintPressed) );

        // I know I can just connect via the editor, but I prefer seeing the connections in code.
        UserControls.Connect( nameof(UserControls.SelectEntity), this, nameof(OnSelectEntitySignaled) );
        UserControls.Connect( nameof(UserControls.DragEntity), this, nameof(OnDragEntitySignaled) );
        UserControls.Connect( nameof(UserControls.DragEntityStart), this, nameof(OnDragEntityStartSignaled) );
        UserControls.Connect( nameof(UserControls.DragEntityStop), this, nameof(OnDragEntityStopSignaled) );
        UserControls.Connect( nameof(UserControls.ContextMenu), this, nameof(OnContextMenuSignaled) );
        UserControls.Connect( nameof(UserControls.SpawnRoom), this, nameof(OnSpawnRoomSignaled) );
        UserControls.Connect( nameof(UserControls.SpawnToken), this, nameof(OnSpawnTokenSignaled) );
        UserControls.Connect( nameof(UserControls.DeleteEntity), this, nameof(OnDeleteEntitySignaled) );
        UserControls.Connect( nameof(UserControls.SaveToSlot), this, nameof(OnSaveToSlotSignaled) );
        UserControls.Connect( nameof(UserControls.LoadFromSlot), this, nameof(OnLoadFromSlotSignaled) );
        UserControls.Connect( nameof(UserControls.ToggleToolbar), this, nameof(OnToggleToolbarSignaled) );
    }

    public override void _PhysicsProcess(float delta)
    {
        MouseCollision.Position = GetGlobalMousePosition();
        if ( SelectedEntity != null )
            SelectedEntityMarker.Position = SelectedEntity.GlobalPosition;
    }

    public static int DNDRoll( Vector2 dice )
    {
        int result = 0;
        for ( int i = 0; i < dice.x; i++ )
            result += RNG.RandiRange( 1, (int)dice.y );
        return result;
    }
    public static int DNDRoll( int quantity, int die ) { return DNDRoll( new Vector2(quantity, die) ); }

    private Vector2 ViewportToGrid( Vector2 position )
    {
        position -= GetViewport().Size/2 - new Vector2(50,50);
        position /= 100;
        position = new Vector2(
            Math.Max( -4, Math.Min((float)Math.Floor(position.x), 4) )
            ,Math.Max( -4, Math.Min((float)Math.Floor(position.y), 4) ) );

        return position;
    }

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
        RNG.State = SavedRNGState + (ulong)IndexInput.Value;
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
                        ConnectRooms( room, otherRoom, direction, RNG.Randf() < CONNECTOR_ONE_WAY_RATE && room.Type != "goal" );
                    }
                }
    }

    private void OnGenerateButtonPressed()
    {
        PopulateMap();
    }

    private void OnSeedInputChanged( String newText )
    {
        SeedHint.Text = Seed == "" ? SEED_EMPTY_WARN : SEED_CONFIRM;
        SeedHint.Visible = true;
    }

    private void OnSeedInputEntered( String newText )
    {
        if ( Seed != "" )
        {
            RNG.Seed = Seed.Hash();
            SavedRNGState = RNG.State;
            SeedHint.Visible = false;
        }
    }

    private void OnControlsHintPressed()
    {
        ControlsHints.Visible = !ControlsHints.Visible;
        ControlsButton.Text = ControlsHints.Visible ? "Hide Controls" : "Show Controls";
    }

    // USER CONTROLS CLASS RESPONSES

    private void OnSelectEntitySignaled( Vector2 position )
    {
        GD.Print("Select entity");
        var entities = MouseCollision.GetOverlappingBodies();
        GD.Print( entities.Count );
        if ( entities.Count > 0 )
        {
            var first = entities[0] as PhysicsBody2D;
            GD.Print( first );
            SelectedEntity = first;
            SelectedEntityMarker.Visible = true;
        }
        else
        {
            SelectedEntity = null;
            SelectedEntityMarker.Visible = false;
        }
    }

    private void OnDragEntitySignaled( Vector2 position )
    {
        if ( SelectedEntity == null ) return;

        SelectedEntity.GlobalPosition = position;
    }

    private void OnDragEntityStartSignaled( )
    {
        if ( SelectedEntity == null ) return;
        
        SelectedEntity.CollisionLayer = 0;
        SelectedEntity.CollisionMask = 0;
    }

    private void OnDragEntityStopSignaled( )
    {
        if ( SelectedEntity == null ) return;

        SelectedEntity.CollisionLayer = 1;
        SelectedEntity.CollisionMask = 1;
    }

    private void OnContextMenuSignaled( Vector2 position )
    {
        GD.Print("Context menu ");
    }
    
    private void OnSpawnRoomSignaled( Vector2 position )
    {
        GD.Print("Spawn room " + ViewportToGrid(position));
    }
    
    private void OnSpawnTokenSignaled( Vector2 position )
    {
        var token = _Token.Instance() as Node2D;
        token.Position = position - GetNode<Node2D>("Origin").Position;
        TokenLayer.AddChild( token );
    }

    private void OnDeleteEntitySignaled( )
    {
        GD.Print("Delete entity");
    }

    private void OnSaveToSlotSignaled( int slot )
    {
        GD.Print("Saved to slot " + slot);
    }
    
    private void OnLoadFromSlotSignaled( int slot )
    {
        GD.Print("Loaded from slot " + slot);
    }

    private void OnToggleToolbarSignaled( )
    {
        // GD.Print("Toolbar toggled");
        Toolbar.Visible = !Toolbar.Visible;
    }
}
