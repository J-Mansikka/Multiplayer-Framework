using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonniStoredPlayerStep
{
    public bool freeSlot = true;
    public byte id { get; private set; }
    public byte input { get; private set; }
    public Vector3 pos { get; private set; }
    public Vector3 rot { get; private set; }
    public float velocity { get; private set; }

    public MonniStoredPlayerStep(byte id, byte input, Vector3 pos)//, Vector3 rot, float velocity)
    {
        this.id = id;
        this.input = input;
        this.pos = pos;
        this.rot = Vector3.zero;
        this.velocity = 0f;
    }
}
