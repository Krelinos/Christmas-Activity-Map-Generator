using Godot;
using System;
using System.Collections.Generic;

public class Room : Node2D
{
    public static readonly Dictionary<String, Color> ROOM_COLORS = new Dictionary<String, Color>
    {
        { "none", Colors.GhostWhite }
        ,{ "start", Colors.Honeydew }
        ,{ "goal", Colors.FloralWhite }
        ,{ "good", Colors.ForestGreen }
        ,{ "bad", Colors.LightCoral }
        ,{ "teleport", Colors.MediumOrchid }
        ,{ "shop", Colors.RoyalBlue }
        ,{ "shadowrealm", Colors.MediumPurple }
    };

    public Vector2 Coordinates { get; set; }
    public String Type { get; set; }
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

    public override void _Ready()
    {
        LabelBox = GetNode<NinePatchRect>("Label/MarginContainer/NinePatchRect");
        Label = GetNode<Label>("Label");
    }
}