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
    public MnetPacket[] packetBuffer;
    //private byte[][] packetBuffer;
    public MnetPacket packetForProcessing;  //
    public MnetPacket packetForWriting;   // 
    public int writeReadDistanceInTicks;
    // Seuraava odotettu paketti numero kai? Olis loogista jos bufferi itse yll‰pit‰‰ tilannetta? joo ku useampi user 
    // Pit‰‰ kyll‰ yhdist‰ess‰ siirt‰‰ alotus numeroon ja siit‰ sitten seurata



    public MnetPacketBuffer(int bufferSize)
    {
        //buffer = new MnetPacket[ServerSettings.packetBufferSize];
        //packetBuffer = new byte[ServerSettings.packetBufferSize][ServerSettings.maxPacketSize];
        //packetBuffer = new byte[ServerSettings.packetBufferSize][];
        packetBuffer = new MnetPacket[bufferSize];
        for (int i = 0; i < packetBuffer.Length; i++)
        {
            packetBuffer[i] = new MnetPacket(true);
        }
        // Link packets in buffer together
        for (int i = 0; i < packetBuffer.Length - 1; i++)
        {
            packetBuffer[i].nextPacket = packetBuffer[i + 1];
        }
        // Connect last packet to the first
        packetBuffer[packetBuffer.Length - 1].nextPacket = packetBuffer[0];
        /*
        for (int i = 0; i < packetBuffer.Length; i++)
        {
            packetBuffer[i] = new byte[ServerSettings.maxPacketSize];
        }
        */
        packetForWriting = packetBuffer[0];
        packetForProcessing = packetBuffer[0];
        writeReadDistanceInTicks = 0;
    }

    // DELETE THIS SHIT!
    public void CheckBuffer()
    {
        for (int i = 0; i < packetBuffer.Length; i++)
        {
            if (packetBuffer[i] == null || packetBuffer[i].nextPacket == null)
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
        return packetBuffer[packetID % packetBuffer.Length];
    }
}
