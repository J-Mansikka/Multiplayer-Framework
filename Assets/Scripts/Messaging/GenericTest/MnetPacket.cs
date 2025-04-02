using System;
using System.Buffers.Binary;
using UnityEngine;

public class MnetPacket
{
    private byte[] _data;
    public int currentLength;   // short
    public bool isActive;
    public bool isFull;
    //public int tickNumber;
    public MnetPacket nextPacket;
    public int extraPacketsInUpdate;
    public int headerLength;    // short

    public byte[] Data
    {
        get { return _data; }
    }

    public byte this[int key]
    {
        get { return _data[key]; }
        set { _data[key] = value;}
    }

    public int GetPacketNumber()
    {
        return MnetTools.BytesToInt(_data.AsSpan(MnetSettings.headerPacketNumberPosition, MnetSettings.headerPacketNumberLength)
            ,MnetSettings.headerPacketNumberLength);
        //get { return BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(MnetSettings.headerPacketNumberPosition, 4)); }
    }

    public int GetTickNumber()
    {
        return MnetTools.BytesToInt(_data.AsSpan(MnetSettings.headerTickNumberPosition, MnetSettings.headerTickNumberLength)
            , MnetSettings.headerTickNumberLength);
    }

    public int GetPacketSize()
    {
        return MnetTools.BytesToInt(_data.AsSpan(MnetSettings.headerSizePosition, MnetSettings.headerSizeLength)
            , MnetSettings.headerSizeLength);
        //get { return BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(MnetSettings.headerSizePosition, 2)); }
    }

    public float GetDeltaTime()
    {
        return MnetTools.BytesToFloat(_data.AsSpan(MnetSettings.headerDeltaTimePosition, MnetSettings.headerDeltaTimeLength));
    }

    public int GetTotalPacketsInUpdate()
    {
        return MnetTools.BytesToInt(_data.AsSpan(MnetSettings.headerPacketCountInfoPosition, MnetSettings.headerPacketCountInfoLength)
            , MnetSettings.headerPacketCountInfoLength);
    }

    public void GetObjectInfo(int readPos, out int objectID, out int objectSize)
    {
        objectID = MnetTools.BytesToInt(_data.AsSpan(readPos,MnetSettings.bytesReservedForObjectID), MnetSettings.bytesReservedForObjectID);
        objectSize = MnetTools.BytesToInt(_data.AsSpan(readPos + MnetSettings.bytesReservedForObjectID,
            MnetSettings.bytesReservedForSegmentSize), MnetSettings.bytesReservedForSegmentSize);
        Debug.Log("INFO IS: ID " + objectID + " SIZE " + objectSize);
    }

    public MessageType PacketType
    {
        get { return (MessageType)_data[0]; }
        set { _data[0] = (byte)value; }
    }

    public MnetPacket(bool isServerPacket)
    {
        _data = new byte[MnetSettings.maxPacketDataSize];
        headerLength = isServerPacket ? MnetSettings.headerCombinedLength : 0;
        Reset();
    }

    // !!!! Mit‰ jos ollaa luettu ihan ok mut jostain syyst‰ tarvittais uudestaan?
    // Millo k‰ytet‰‰n isActive? Resetoidaanko ku kirjotetaan vaan?
    public void Reset()
    {
        currentLength = headerLength;
        isActive = false;
        isFull = false;
        extraPacketsInUpdate = 0;
    }

    public void InitServerPacket()
    {
        isActive = true;
        currentLength = BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan().Slice(MnetSettings.headerSizePosition, 2));
        extraPacketsInUpdate = _data[MnetSettings.headerPacketCountInfoPosition + 1];
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
    public Span<byte> AvailableSpace()
    {
        // Get remaining space on the buffer
        return _data.AsSpan(currentLength,MnetSettings.maxPacketDataSize - currentLength);//, ServerSettings.maxPacketSize - currentLength);
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

    // Helppo tapa ottaa palanen tietyst‰ kohtaa. Voi menn‰ yli mutta userin pit‰is se huomioida kai?
    public Span<byte> Span(int start = 0, int length = 0)
    {
            //Debug.Log(nimi+" ASKED FOR " + length + " BYTES STARTING AT " + start+" WITH REMAINDING SPACE "+(MnetSettings.maxPacketDataSize-currentLength));
        if (length == 0) length = MnetSettings.maxPacketDataSize - currentLength;
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
