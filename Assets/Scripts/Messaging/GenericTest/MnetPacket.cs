using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class MnetPacket
{
    private byte[] data;
    public short currentLength;
    public bool isActive;
    public MnetPacket nextPacket;
    public int extraPacketsInUpdate;

    public MnetPacket()
    {
        data = new byte[ServerSettings.maxPacketSize];
        Reset();
    }

    public void Reset()
    {
        currentLength = ServerSettings.headerCombinedLength;
        isActive = false;
        extraPacketsInUpdate = 0;
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

    // Typerä nimi.. GetRemainingSpace ?
    public Span<byte> GetRemainingSpace()
    {
        // Get remaining space on the buffer
        return data.AsSpan(currentLength..);//, ServerSettings.maxPacketSize - currentLength);
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

    public Span<byte> Get()
    {
        return data.AsSpan(0,currentLength);
    }

    public Span<byte> GetHeader()
    {
        return data.AsSpan(0,ServerSettings.headerCombinedLength);
    }

    public Span<byte> GetData()
    {
        return data.AsSpan(ServerSettings.headerCombinedLength,currentLength-ServerSettings.headerCombinedLength);
    }
}
