using Godot;
using System;
using System.Diagnostics.Tracing;

public class UserControls : Node
{
    [Signal] public delegate void SelectEntity( Vector2 position );
    [Signal] public delegate void ContextMenu( Vector2 position );
    [Signal] public delegate void SpawnRoom( Vector2 position );
    [Signal] public delegate void SpawnToken( Vector2 position );
    [Signal] public delegate void DeleteEntity( );
    [Signal] public delegate void SaveToSlot( int slot );
    [Signal] public delegate void LoadFromSlot( int slot );
    [Signal] public delegate void ToggleToolbar( );

    public override void _UnhandledInput(InputEvent @event)
    {
        if ( @event is InputEventMouseButton mouse 
        && mouse.Pressed )
        {
            if ( mouse.ButtonIndex == (int)ButtonList.Left )
            {
                if ( Input.IsActionPressed( "SpawnRoom" ) )
                    EmitSignal( nameof(SpawnRoom), GetViewport().GetMousePosition() );

                else if ( Input.IsActionPressed( "SpawnToken" ) )
                    EmitSignal( nameof(SpawnToken), GetViewport().GetMousePosition() );
                
                else
                    EmitSignal( nameof(SelectEntity), GetViewport().GetMousePosition() );
            }

            if ( mouse.ButtonIndex == (int)ButtonList.Right )
                EmitSignal( nameof(ContextMenu), GetViewport().GetMousePosition() );
        }

        if ( @event is InputEventKey key
        && key.Pressed )
        {
            if ( key.Scancode == (int)KeyList.Backspace )
                EmitSignal( nameof(DeleteEntity) );
            
            else if ( key.Scancode >= (int)KeyList.Key0 && key.Scancode <= (int)KeyList.Key9 )
            {
                if ( Input.IsActionPressed("SaveToSlot") )
                    EmitSignal( nameof(SaveToSlot), key.Scancode - (int)KeyList.Key0 );

                else if ( Input.IsActionPressed("LoadFromSlot") )
                    EmitSignal( nameof(LoadFromSlot), key.Scancode - (int)KeyList.Key0 );
            }
            
            else if ( Input.IsActionJustPressed("ToggleToolbar") )
                EmitSignal( nameof(ToggleToolbar) );
        }
    }
}
