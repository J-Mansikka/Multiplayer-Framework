using System;
using System.Buffers.Binary;
using UnityEngine;

public class MnetPacket
{
    private byte[] _data;
    public int currentLength;   // short
    public int readPosition;
    //public int writePosition; Ei tarvita koska headerin osat vaihdetaan suoraan ja dataa ku kirjotetaan ni kirjotetaan vaan aina kerran ja olemassa olevan per‰‰n
    public int packetNumber;
    //public bool isActive;
    public bool isFull;
    //public int tickNumber;
    public MnetPacket nextPacket;
    //      public int extraPacketsInUpdate;
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

    public int SpaceRemaining
    {
        get { return Mnet.maxPacketDataSize - currentLength; }
    }

    public int GetPacketNumber()
    {
        return MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerPacketNumberPosition, Mnet.headerPacketNumberLength));
            //,Mnet.headerPacketNumberLength);
        //get { return BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(MnetSettings.headerPacketNumberPosition, 4)); }
    }

    public void SetPacketNumber(int packetNumber)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerPacketNumberPosition, Mnet.headerPacketNumberLength), packetNumber);
    }

    public int GetTickNumber()
    {
        return MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerTickNumberPosition, Mnet.headerTickNumberLength));
            //, Mnet.headerTickNumberLength);
    }

    public void SetTickNumber(int tickNumber)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerTickNumberPosition, Mnet.headerTickNumberLength), tickNumber);
    }

    public void UpdatePacketSize()
    {

        currentLength = MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerSizePosition, Mnet.headerSizeLength));
            //, Mnet.headerSizeLength);
        //get { return BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(MnetSettings.headerSizePosition, 2)); }
    }

    public void SetPacketSize(int packetSize)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerSizePosition, Mnet.headerSizeLength), packetSize);
    }

    public float GetDeltaTime()
    {
        return MnetTools.BytesToFloat(_data.AsSpan(Mnet.headerDeltaTimePosition, Mnet.headerDeltaTimeLength));
    }

    public void SetDeltaTime(float deltaTime)
    {
        MnetTools.FloatToBytes(_data.AsSpan(Mnet.headerDeltaTimePosition, Mnet.headerDeltaTimeLength), deltaTime);
    }

    public int GetSequenceNumber()
    {
        return MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerSequencePosition, Mnet.headerSequenceLength));
    //, Mnet.headerSequenceLength);
    }
    public void SetSequenceNumber(int sequenceNumber)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerSequencePosition, Mnet.headerSequenceLength), sequenceNumber);
    }

    public int GetTotalPacketsInUpdate()
    {
        return MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerPacketCountPosition, Mnet.headerPacketCountLength));
            //, Mnet.headerPacketCountLength);
    }

    public void SetTotalPacketsInUpdate(int packetCount)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerPacketCountPosition, Mnet.headerPacketCountLength), packetCount);
    }

    public int GetObjectID()
    {
        return MnetTools.BytesToInteger(Read(Mnet.bytesReservedForObjectID));//_data.AsSpan(readPos, Mnet.bytesReservedForObjectID));//, Mnet.bytesReservedForObjectID);
        //objectSize = MnetTools.BytesToInteger(Read(Mnet.bytesReservedForSegmentSize));//_data.AsSpan(readPos + Mnet.bytesReservedForObjectID,
            //Mnet.bytesReservedForSegmentSize));//, Mnet.bytesReservedForSegmentSize);
        //Debug.Log("INFO IS: ID " + objectID + " SIZE " + objectSize);
    }

    public MessageType Message
    {
        get { return (MessageType)_data[0]; }
        set { _data[0] = (byte)value; }
    }

    public MnetPacket(bool reserveSpaceForFullHeader)
    {
        _data = new byte[Mnet.maxPacketDataSize];
        headerLength = reserveSpaceForFullHeader ? Mnet.headerCombinedLength : 0;
        //      Reset();
    }


    public void Reset()
    {
        Array.Clear(_data, 0, headerLength);    // Onko pakollinen? Eikˆs vastaanotossa aina ylikirjoteta kaikki?
        currentLength = headerLength;
        readPosition = 0;
        isFull = false;
        //      extraPacketsInUpdate = 0;
    }

    /*
    public void InitServerPacket()
    {
        isActive = true;
        currentLength = BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan().Slice(MnetSettings.headerSizePosition, 2));
        extraPacketsInUpdate = _data[MnetSettings.headerPacketCountPosition + 1];
    }
    */

    public string ReadMessage()
    {
        readPosition += Mnet.messageLength;
        return System.Text.Encoding.ASCII.GetString(_data.AsSpan(readPosition-Mnet.messageLength, Mnet.messageLength));
    }

    public void WriteMessage(string message)
    {
        if(currentLength < headerLength) currentLength = headerLength;
        System.Text.Encoding.ASCII.GetBytes(message, _data.AsSpan(currentLength, Mnet.messageLength));
        currentLength += Mnet.messageLength;
    }


    public void SetupServerPacket()
    {
        // Aseta length etc
    }

    // public void InitClientPacket()
    // public void SetupClientPacket()

    // !!! OLIKO Tƒƒ VAI SWAP KƒYT÷S WHAT!   Copies the data portion from another packet.
    public void CopyDataFrom(MnetPacket packetWithNewData)
    {
        currentLength = packetWithNewData.currentLength;
        packetWithNewData.DataOnly().CopyTo(DataOnly());
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

    // Returns the available space in the packet as a span
    public Span<byte> AvailableSpace()
    {
        // Get remaining space on the buffer
        return _data.AsSpan(currentLength,Mnet.maxPacketDataSize - currentLength);//, ServerSettings.maxPacketSize - currentLength);
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

    // Returns a custom slice of the packet. If no length is specified, will give all the data from the start point to the end
    public Span<byte> Span(int start = 0, int length = 0)
    {
            //Debug.Log(nimi+" ASKED FOR " + length + " BYTES STARTING AT " + start+" WITH REMAINDING SPACE "+(MnetSettings.maxPacketDataSize-currentLength));
        if (length == 0) length = Mnet.maxPacketDataSize - start;
        return _data.AsSpan(start, length);
    }

    public Span<byte> Write(int writeSegmentLength)
    {
        currentLength += writeSegmentLength;
        return _data.AsSpan(currentLength-writeSegmentLength,writeSegmentLength);
    }

    public Span<byte> Read(int readSegmentLength)
    {
        readPosition += readSegmentLength;
        return _data.AsSpan(readPosition-readSegmentLength,readSegmentLength);
    }

    public void WriteSingleByte(byte newByte)
    {
        _data[currentLength++] = newByte;
    }

    public byte ReadSingleByte()
    {
        return _data[readPosition++];
    }

    // Returns the packet in its current form. This is the one we want to send through sockets
    public Span<byte> CurrentMessage()
    {
        return _data.AsSpan(0,currentLength);
    }

    // Returns the entire array as a span
    public Span<byte> AllBytes()
    {
        return _data.AsSpan();
    }

    // Returns only the header part of the packet
    public Span<byte> HeaderOnly()
    {
        return _data.AsSpan(0, headerLength);
    }

    // Returns only the data portion of the packet, leaving out the packet's header
    public Span<byte> DataOnly()
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

    // Swap the data between two packets. Since _data is private, we have to use two methods to finish the process
    public void SwapData(MnetPacket otherPacket)
    {
        byte[] tempPointer = otherPacket.Data;
        otherPacket.OverrideData(this);
        _data = tempPointer;

    }

    // Since _data is private, we have to use this method to gain access to change the value
    public void OverrideData(MnetPacket overridingPacket)
    {
        _data = overridingPacket.Data;
    }

}
