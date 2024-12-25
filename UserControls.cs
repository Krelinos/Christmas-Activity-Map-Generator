using Godot;
using System;
using System.Diagnostics.Tracing;

public class UserControls : Node
{
    [Signal] public delegate void SelectEntity( Vector2 position );
    [Signal] public delegate void DragEntity( Vector2 position );
    [Signal] public delegate void DragEntityStart( );
    [Signal] public delegate void DragEntityStop( );
    [Signal] public delegate void ContextMenu( Vector2 position );
    [Signal] public delegate void SpawnRoom( Vector2 position );
    [Signal] public delegate void SpawnToken( Vector2 position );
    [Signal] public delegate void DeleteEntity( );
    [Signal] public delegate void SaveToSlot( int slot );
    [Signal] public delegate void LoadFromSlot( int slot );
    [Signal] public delegate void ToggleToolbar( );

    private bool shouldDrag = false;
    private bool draggingStarted = false;

    public override void _Input(InputEvent @event)
    {
        if ( @event is InputEventMouseButton mouse )
        {
            if ( mouse.Pressed )
            {
                if ( mouse.ButtonIndex == (int)ButtonList.Left )
                {
                    if ( Input.IsActionPressed( "SpawnRoom" ) )
                        EmitSignal( nameof(SpawnRoom), GetViewport().GetMousePosition() );

                    else if ( Input.IsActionPressed( "SpawnToken" ) )
                        EmitSignal( nameof(SpawnToken), GetViewport().GetMousePosition() );
                    
                    else
                    {
                        EmitSignal( nameof(SelectEntity), GetViewport().GetMousePosition() );
                        shouldDrag = true;
                    }
                }

                else if ( mouse.ButtonIndex == (int)ButtonList.Right )
                    EmitSignal( nameof(ContextMenu), GetViewport().GetMousePosition() );
            }

            else
            {
                shouldDrag = false;
                if ( draggingStarted )
                {
                    draggingStarted = false;
                    EmitSignal( nameof(DragEntityStop) );
                }
            }
        }

        if ( @event is InputEventMouseMotion motion && shouldDrag )
        {
            EmitSignal( nameof(DragEntity), motion.Position );
            if ( draggingStarted != true )
            {
                draggingStarted = true;
                EmitSignal( nameof(DragEntityStart) );
            }
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
