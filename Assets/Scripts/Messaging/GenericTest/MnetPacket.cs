using System;
using System.Buffers.Binary;

public class MnetPacket
{
    private byte[] _data;
    public short currentLength;
    public bool isActive;
    public int tickNumber;
    public MnetPacket nextPacket;
    public int extraPacketsInUpdate;
    public short headerLength;

    public byte[] Data
    {
        get { return _data; }
    }

    public int ServerPacketNumber
    {
        get { return BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(ServerSettings.headerServerPacketNumberPosition, 4)); }
    }

    public short ServerPacketLength
    {
        get { return BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(ServerSettings.headerServerSizePosition, 2)); }
    }
    public MessageType PacketType
    {
        get { return (MessageType)_data[0]; }
        set { _data[0] = (byte)value; }
    }

    public MnetPacket(bool isServerPacket)
    {
        _data = new byte[ServerSettings.maxPacketSize];
        headerLength = isServerPacket ? ServerSettings.headerServerCombinedLength : (short)0;
        Reset();
    }

    public void Reset()
    {
        currentLength = headerLength;
        isActive = false;
        extraPacketsInUpdate = 0;
    }

    public void InitServerPacket()
    {
        isActive = true;
        currentLength = BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan().Slice(ServerSettings.headerServerSizePosition, 2));
        extraPacketsInUpdate = _data[ServerSettings.headerServerTickSplitInfoPosition + 1];
    }

    public void SetupServerPacket()
    {
        // Aseta length etc
    }

    // public void InitClientPacket()
    // public void SetupClientPacket()

    public void CopyDataFrom(MnetPacket packetWithNewData)
    {
        currentLength = packetWithNewData.currentLength;
        packetWithNewData.PacketData().CopyTo(PacketData());
    }
    /*
    public void Add(Span<byte> incoming)
    {
        for (int i = 0; i < incoming.Length; i++)
        {
            data[i+Length] = incoming[i];
            Length++;
        }
    }
    */

    // Typer‰ nimi.. GetRemainingSpace ?
    public Span<byte> RemainingPacketSpace()
    {
        // Get remaining space on the buffer
        return _data.AsSpan(currentLength..);//, ServerSettings.maxPacketSize - currentLength);
    }

    /*  OBJECTIT HALUAA VAAN SPANNIN
    public void SetData(Span<byte> newData)
    {
        int writeIndex = ServerSettings.maxPacketSize - spaceRemaining;
        for (int i = 0; i < newData.Length; i++)
        {
            data[writeIndex+i] = newData[i];
        }
        spaceRemaining -= newData.Length;
    }
    */

    public Span<byte> Span(int start = 0, int length = ServerSettings.maxPacketSize)
    {
        return _data.AsSpan(start, length);
    }

    public Span<byte> WholePacket()
    {
        return _data.AsSpan(0,currentLength);
    }

    public Span<byte> PacketHeader()
    {
        return _data.AsSpan(0, headerLength);
    }

    public Span<byte> PacketData()
    {
        return _data.AsSpan(headerLength, currentLength-headerLength);
    }

    /// V‰h‰n ep‰ilytt‰v‰ ku palauttaa kaks eri asiaa, joko uuden tauluko tai fieldin
    /// Ehk‰ pit‰is vaa spannin kautta ottaa osa? Varmsita et jos joku t‰t‰ k‰ytt‰‰ segmentill‰ ni onko parempi vaihtoehto <summary>
    /// Ei ees vittu toimi n‰i ku ei uude tauluko p‰‰lle voi mit‰‰n tallettaa, pelk‰st‰‰ fieldi no huhuhuhuhuhuh
    /*
    public byte[] GetBytes(int start = 0, int end = ServerSettings.maxPacketSize)
    {
        if(start != 0 || end != ServerSettings.maxPacketSize)
        {
            byte[] segment = new byte[end - start];
            for(int i = 0; i < segment.Length; i++)
            {
                segment[i] = data[i+start];
            }
            return segment;
        }
        return data;
    }
    */

    /*
    public void SwapBytes(byte[] newBytes, short newLength, bool active = true)
    {
        _data = newBytes;
        currentLength = newLength;
        isActive = active;
    }
    */

    public void SwapData(MnetPacket otherPacket)
    {
        byte[] tempPointer = otherPacket.Data;
        otherPacket.OverrideData(this);
        _data = tempPointer;

    }

    public void OverrideData(MnetPacket overridingPacket)
    {
        _data = overridingPacket.Data;
    }

}
