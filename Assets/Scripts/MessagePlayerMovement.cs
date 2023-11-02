using System.Collections;
using System.Collections.Generic;
using TMPro.Examples;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class MessagePlayerMovement : MessageBase
{

    //public MByte id = new MByte();
    public MByte move = new MByte();
    public MFloat distance = new MFloat();
    //public MonniMessageSegment[] messageSegments;
    /*
    public byte id;
    public byte move;
    public float xRot;
    public float yRot;
    */
    //public float inputDelta;

    public override void Reset()
    {
        base.Reset();
    }

    public void DoShit()
    {
        foreach (MessageSegmentBase segment in messageSegments)
        {
            //Debug.Log(segment.Bytes[1]);

        }
    }

    public MessagePlayerMovement()
    {
        messageType = MessageType.ClientInput;
        messageSegments = new MessageSegmentBase[] { move,distance };
        AutoSetup();
        /*
        id = packetnumber;
        move = (byte)input;
        xRot = rotX;
        yRot = rotY;
        */
    }

    public void Setup(byte packetnumber, byte input, float moveTime)
    {
        messageHasChanged = true;
        sequenceNumber = packetnumber;
        move.Item = input;
        distance.Item = moveTime;
    }

    public override void ToData(byte[] incoming, int messageStartPosition = 0)
    {
        base.ToData(incoming, messageStartPosition);

    }

}
