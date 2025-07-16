using System;

public class MnetActions
{
    private byte[] tickedActions;
    private int currentLength;
    //private int readPos;
    public int Length
    {
        get { return currentLength; }
    }
    public MnetActions()
    {
        tickedActions = new byte[8];
    }

    public Span<byte> GetActions()
    {
        return tickedActions.AsSpan(0,currentLength);
    }

    public void Add(byte action)
    {
        if(currentLength==tickedActions.Length)
        {
            byte[] newArray = new byte[tickedActions.Length * 2];
            for(int i = 0; i < currentLength; i++)
            {
                newArray[i] = tickedActions[i];
            }
            tickedActions = newArray;
        }
        tickedActions[currentLength++] = action;
    }
    
    public void Serialize(MnetPacket packet)
    {
        packet.WriteSingleByte((byte)currentLength);
        for (int i = 0; i < currentLength; i++)
        {
            packet.WriteSingleByte(tickedActions[i]);
        }
        currentLength = 0;
    }

    public void Deserialize(MnetPacket packet)
    {
        currentLength = packet.ReadSingleByte();
        while (tickedActions.Length < currentLength)
        {
            tickedActions = new byte[tickedActions.Length * 2];
        }
        for(int i = 0;i < currentLength; i++)
        {
            tickedActions[i] = packet.ReadSingleByte();
        }
    }

    /*
    public byte GetNext()
    {
        return actions[readPos++];
    }
    */

}
