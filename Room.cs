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

    public override void _Ready()
    {
        LabelBox = GetNode<NinePatchRect>("Label/MarginContainer/NinePatchRect");
        Label = GetNode<Label>("Label");

        RoomColor = ROOM_COLORS[ Type ];
        Text = Type == "none" ? "" : Type.Capitalize();
    }
}