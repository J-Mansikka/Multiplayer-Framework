using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MpacketContainer
{
    private byte[] packetData;
    private int size;
    public bool send;

    public MpacketContainer()
    {
        packetData = new byte[Mnet.maxPacketDataSize];
    }

}
