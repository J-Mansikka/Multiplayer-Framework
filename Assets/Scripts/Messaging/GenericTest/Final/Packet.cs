using System;
using System.Buffers.Binary;
using UnityEngine;

public class Packet
{
    public byte[] bytes;
    public int currentLength;
    public int readPosition;
    public PacketType PacketType { get; private set; }
    public bool isNew;

    public Packet nextPacket;

    public int debugIndex;

    // Span of current bytes on the packet
    public Span<byte> Data
    {
        get { return bytes.AsSpan(0,currentLength); }
    }

    public byte this[int key]
    {
        get { return bytes[key]; }
        set { bytes[key] = value; }
    }

    public Packet(PacketType regularPacketType)
    {
        bytes = new byte[Mnet.maxPacketDataSize];
        readPosition = Mnet.headerCombinedLength;
        currentLength = Mnet.headerCombinedLength;
        SetPacketType(regularPacketType);
    }

    public int SpaceRemaining
    {
        get { return Mnet.maxPacketDataSize - currentLength; }
    }

    public int AmountWritten()
    {
        return currentLength - Mnet.headerCombinedLength;
    }

    public int BytesLeftToRead
    {
        get { return currentLength - readPosition; }
    }

    public void WriteHeader(int packetNumber, int tickNumber, float time)
    {
        //SetPacketType(PacketType);
        MnetTools.IntegerToBytes(Span(Mnet.headerPacketNumberPosition,Mnet.headerPacketNumberLength), packetNumber);
        SetTickNumber(tickNumber);
        MnetTools.FloatToBytes(Span(Mnet.headerDeltaTimePosition,Mnet.headerDeltaTimeLength), time);
        MnetTools.IntegerToBytes(Span(Mnet.headerSizePosition,Mnet.headerSizeLength), currentLength);
        Debug.Log("WROTE HEADER " + GetPacketType() + ", Number:" + GetPacketNumber() +", Tick:"+GetTickNumber()+", Time:"+GetDeltaTime()+", Size: "+GetSize());
    }

    public void SetPacketType(PacketType type)
    {
        bytes[0] = (byte)type;
    }

    public PacketType GetPacketType()
    {
        return (PacketType)bytes[0];
    }

    public int GetPacketNumber()
    {
        return MnetTools.BytesToInteger(Span(Mnet.headerPacketNumberPosition, Mnet.headerPacketNumberLength));
    }

    public int GetTickNumber()
    {
        return MnetTools.BytesToInteger(Span(Mnet.headerTickNumberPosition, Mnet.headerTickNumberLength));
    }

    public void SetTickNumber(int tickNumber)
    {
        MnetTools.IntegerToBytes(Span(Mnet.headerTickNumberPosition, Mnet.headerTickNumberLength), tickNumber);
    }

    public float GetDeltaTime()
    {
        return MnetTools.BytesToFloat(Span(Mnet.headerDeltaTimePosition, Mnet.headerDeltaTimeLength));
    }

    public int GetSize()
    {
        return MnetTools.BytesToInteger(Span(Mnet.headerSizePosition, Mnet.headerSizeLength));
    }

    public void InitializeReceived()
    {
        //currentLength = GetSize();
        //Debug.Log("RECEIVED ASETTI PAKETI PITUUDEKS " + currentLength);
        readPosition = Mnet.headerCombinedLength;
        PacketType = GetPacketType();
        isNew = true;
    }

    public void PrepareForRead()
    {
        //readPosition = Mnet.headerCombinedLength;
        currentLength = GetSize();
    }

    public void Clear()
    {
        currentLength = Mnet.headerCombinedLength;
        readPosition = Mnet.headerCombinedLength;
        isNew = false;
    }

    public PacketType ReadPacketType()
    {
        if (readPosition == 0) readPosition++;
        return (PacketType)bytes[0];
    }

    public string ReadMessage()
    {
        return System.Text.Encoding.ASCII.GetString(ReadBytes(Mnet.messageLength));
    }

    public void WriteMessage(string message)
    {
        byte[] newMessage = System.Text.Encoding.ASCII.GetBytes(message);
        for(int i = 0; i < Mnet.messageLength; i++)
        {
            bytes[currentLength++] = newMessage[i];
        }
    }

    public void WriteString(string message)
    {
        int byteLength = message.Length * 2;
        MnetTools.Int32ToBytes(WriteBytes(4), byteLength);
        System.Text.Encoding.Unicode.GetBytes(message, WriteBytes(byteLength));
    }

    public string ReadString()
    {
        int length = MnetTools.BytesToInt32(ReadBytes(4));
        string scene = System.Text.Encoding.Unicode.GetString(ReadBytes(length));
        return scene;
    }

    public void WriteSingleByte(byte value)
    {
        bytes[currentLength++] = value;
    }

    public byte ReadSingleByte()
    {
        return bytes[readPosition++];
    }

    public Span<byte> WriteBytes(int writeSegmentLength)
    {
        currentLength += writeSegmentLength;
        //Debug.Log("Length: " + currentLength + " Segment: " + writeSegmentLength);
        return bytes.AsSpan(currentLength - writeSegmentLength, writeSegmentLength);
    }

    public Span<byte> ReadBytes(int readSegmentLength)
    {
        readPosition += readSegmentLength;
        return bytes.AsSpan(readPosition - readSegmentLength, readSegmentLength);
    }

    public Span<byte> Span(int start, int length)
    {
        return bytes.AsSpan(start, length);
    }

    // Returns all available space left
    public Span<byte> AllAvailableBytes()
    {
        return bytes.AsSpan(currentLength, bytes.Length - currentLength);
    }

    // Returns all remaining data
    public Span<byte> AllRemainingBytes()
    {
        return bytes.AsSpan(readPosition, currentLength - readPosition);
    }

    // Returns the entirety of the packet, mostly used when receiving a whole packet from a socket
    public Span<byte> AsEmpty()
    {
        return bytes.AsSpan(0, Mnet.maxPacketDataSize);
    }

    public void SwapData(Packet otherPacket)
    {
        byte[] tempPointer = bytes;
        bytes = otherPacket.bytes;
        otherPacket.bytes = tempPointer;
    }

    public void DemoGetData(byte[] newBytes)
    {
        for (int i = 0; i < newBytes.Length; i++)
        {
            bytes[i] = newBytes[i];
        }
    }
}
