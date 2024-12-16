using Godot;
using System;
using System.Collections.Generic;

public class Room : Node2D
{
    public static readonly Dictionary<String, Color> ROOM_COLORS = new Dictionary<String, Color>
    {
        { "none", Colors.GhostWhite }
        ,{ "start", Colors.DarkGoldenrod }
        ,{ "goal", Colors.PeachPuff }
        ,{ "good", Colors.ForestGreen }
        ,{ "bad", Colors.LightCoral }
        ,{ "teleport_random", Colors.MediumOrchid }
        ,{ "teleport_pair_a", Colors.DarkTurquoise }
        ,{ "teleport_pair_b", Colors.LightGreen }
        ,{ "shop", Colors.RoyalBlue }
        ,{ "shadow_realm", Colors.MediumPurple }
    };

    public static readonly Dictionary<String, Vector2> COMPASS_DIRECTION = new Dictionary<String, Vector2>
    {
        { "north", new Vector2(0, -1) }
        ,{ "north_east", new Vector2(1, -1) }
        ,{ "east", new Vector2(1, 0) }
        ,{ "south_east", new Vector2(1, 1) }
        ,{ "south", new Vector2(0, 1) }
        ,{ "south_west", new Vector2(-1, 1) }
        ,{ "west", new Vector2(-1, 0) }
        ,{ "north_west", new Vector2(-1, -1) }
    };

    public static readonly Dictionary<String, String> COMPASS_OPPOSITE = new Dictionary<String, String>
    {
        { "north", "south" }
        ,{ "north_east", "south_west" }
        ,{ "east", "west" }
        ,{ "south_east", "north_west" }
        ,{ "south", "north" }
        ,{ "south_west", "north_east" }
        ,{ "west", "east" }
        ,{ "north_west", "south_east" }
    };

    public static readonly String[] COMPASS_INDEX = new String[]{ "north", "north_east", "east", "south_east", "south", "south_west", "west", "north_west" };

    public Vector2 Coordinates
    {
        get => _Coordinates;
        set
        {
            _Coordinates = value;
            Position = value * 100;
        }
    }
    public String Type
    {
        get => _Type;
        set
        {
            _Type = value;

            if ( !Room.ROOM_COLORS.ContainsKey( value ) )
            {
                GD.PushError("Unknown room type '" + value + "' was provided. Defaulting to 'none'.");
                _Type = "none";
            }

            //RoomColor = ROOM_COLORS[ _Type ];
        }
    }

    public int MaxConnections
    {
        get
        {
            if( _MaxConnections == 0 )
                switch( Type )
                {
                    case "goal":
                    case "shadow_realm":
                        _MaxConnections = 1;
                        break;
                    default:
                        _MaxConnections = Main.DNDRoll( 2, 4 );
                        break;
                }
            return _MaxConnections;
        }
    }

    public int RemainingConnectionSlots { get => MaxConnections - ConnectedRooms.Count; }

    public Dictionary< String, Room > ConnectedRooms
    {
        get => _ConnectedRooms;
    }

    public Color RoomColor
    {
        get => LabelBox.Modulate;
        private set => LabelBox.Modulate = value;
    }
    public String Text
    {
        get => Label.Text;
        private set => Label.Text = value;
    }

    private NinePatchRect LabelBox;
    private Label Label;

    private Vector2 _Coordinates;
    private String _Type = "none";
    private int _MaxConnections = 0;
    private Dictionary< String, Room > _ConnectedRooms = new Dictionary<String, Room>();

    public override void _Ready()
    {
        LabelBox = GetNode<NinePatchRect>("Label/MarginContainer/NinePatchRect");
        Label = GetNode<Label>("Label");

        RoomColor = ROOM_COLORS[ Type ];
        Text = Type == "none" ? "" : Type.Capitalize();
    }
}