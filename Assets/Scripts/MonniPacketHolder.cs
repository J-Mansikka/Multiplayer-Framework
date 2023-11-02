using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MonniPacketHolder
{
    public IPEndPoint sender { get; private set; }
    public byte[] packet { get; private set; }


    public MonniPacketHolder(IPEndPoint ep, byte[] pac)
    {
        sender = ep;
        packet = pac;
    }


}
