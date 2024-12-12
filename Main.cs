using Godot;
using System;
using System.Collections.Generic;

public class Main : Node2D
{
    // Rooms are organized onto a 2D grid.
    private Dictionary< Vector2, Room > Rooms;

    private PackedScene _Room;

    public override void _Ready()
    {
        _Room = GD.Load("res://Scenes/Room.tscn") as PackedScene;
    }

    private Room CreateRoom( Vector2 coord, String type )
    {
        if ( Rooms.ContainsKey( coord ) )
        {
            GD.PushError("Tried to generate a room at (" + coord + "), but it already has one.");
            return null;
        }

        if ( !Room.ROOM_COLORS.ContainsKey( type ) )
        {
            GD.PushError("Unknown room type '" + type + "' was provided. Defaulting to 'none'.");
            type = "none";
        }

        var room = _Room.Instance() as Room;
        room.Coordinates = coord;
        room.Type = type;

        return room;
    }

    private void PopulateMap()
    {
        
    }
}
