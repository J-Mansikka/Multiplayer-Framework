using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class MTESTserver : MonoBehaviour
{
    public Mobject sender;
    public Mobject receiver;
    private MpacketContainer[] buffer;
    private int frame;
    private float frameTime;
    private int numberOfPackets;
    private int curPacket;

    public MnetStringUnicode useri;

    // TESTI OSAT
    public Mint inttiTesti;

    /*
     * Eli pistetään objecti pakettii ja jos ei mahdu ni hypätää seuraavaan numberOfPackets++.
     * MpacketContainerilla seurataan kokoa ja ku ollaa lähetetty ni curPacket += numberOfPackets
     * 
     * 
     */

    private void Awake()
    {
        buffer = new MpacketContainer[Mnet.serverPacketBufferSize];

    }

    private void Start()
    {
        //int testmi = 1025;
        //byte first = (byte)(testmi >> 8);
        //byte second = (byte)testmi;
        //print("EKA "+first+". TOKA "+second);
        //int back = ((int)first << 8) + (int)second;
        //print("KOKO "+back);
        serializeTest();
    }
    private void FixedUpdate()
    {
        //foreach (Mobject obj in objects)
        //{
        //    int changeCount = obj.CheckChanges();
        //    if(changeCount > 0)
        //    {
        //        for (int i = 0; i < changeCount; i++)
        //        {
                    
        //        }
        //    }
        //}        
    }

    public void serializeTest()
    {
        if (sender.CheckChanges() > 0)
        {
            receiver.SetChanges(sender.bytesToSend);
        }
    }
}
