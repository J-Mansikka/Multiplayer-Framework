using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerSettings
{
    [Flags]
    public enum PlayerInputs
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        Attack = 16,
        Activate = 32
    }

    [Flags]
    public enum PlayerActions
    {
        None = 0,
        Paused = 1,
        UsedItem = 2,
        DropItem = 3,

    }
}
