using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class MnetPacketBuffer
{
    public bool isCircularBuffer;
    //public int currentID = 0;
    public MnetPacket[] buffer;
    //private byte[][] packetBuffer;



    public MnetPacketBuffer(int size = ServerSettings.serverPacketBufferSize, bool bufferIsServerType = true)
    {
        //buffer = new MnetPacket[ServerSettings.packetBufferSize];
        //packetBuffer = new byte[ServerSettings.packetBufferSize][ServerSettings.maxPacketSize];
        //packetBuffer = new byte[ServerSettings.packetBufferSize][];
        buffer = new MnetPacket[size];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = new MnetPacket(bufferIsServerType);
        }
        // Link packets in buffer together
        for (int i = 0; i < buffer.Length - 1; i++)
        {
            buffer[i].nextPacket = buffer[i + 1];
        }
        // Connect last packet to the first
        buffer[buffer.Length - 1].nextPacket = buffer[0];
        /*
        for (int i = 0; i < packetBuffer.Length; i++)
        {
            packetBuffer[i] = new byte[ServerSettings.maxPacketSize];
        }
        */
    }

    // DELETE THIS SHIT!
    public void CheckBuffer()
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == null || buffer[i].nextPacket == null)
            {
                Debug.Log("!!!!!!!!!!!!!!!          HOLD ON         !!!!!!!!!!!!");
            }
        }
    }
    /*
    public void Add(int currentID, Span<byte> packets)
    {
        //buffer[currentID % buffer.Length].Set(packets);
        //int bufferSlot = currentID % packetBuffer.Length;
        Span<byte> data = buffer[currentID % ServerSettings.maxPacketSize].Get();
        for (int i = 0; i < packets.Length; i++)
        {
            data[i] = packets[i];
            //packetBuffer[bufferSlot][i] = packets[i];
        }
        //Debug.Log("SIZE "+BinaryPrimitives.ReadInt16LittleEndian(packetBuffer[bufferSlot].AsSpan(ServerSettings.headerSizePosition, 2)));
    }
    */

    /*
    public Span<byte> Get(int packetID)
    {
        return buffer[packetID % buffer.Length].Get();
        //int bufferSlot = packetID % packetBuffer.Length;
        //int packetLength = BinaryPrimitives.ReadUInt16LittleEndian(packetBuffer[bufferSlot].AsSpan(ServerSettings.headerSizePosition,2));
        //Debug.Log("SIZE "+packetLength);
        //return packetBuffer[bufferSlot].AsSpan(..packetLength);
    }
    */

    public MnetPacket Get(int packetID)
    {
        return buffer[packetID % buffer.Length];
    }
}
