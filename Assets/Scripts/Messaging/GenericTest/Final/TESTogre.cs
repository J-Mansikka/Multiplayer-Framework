using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class TESTogre : MnetObject
{
    public string ipAddressTest;
    public MnetInt health;
    public bool button;

    private void Start()
    {
        Initialize(69);
    }

    private void Update()
    {
        if (button)
        {
            button = false;
            Test();
        }
    }


    public void Test()
    {
        System.Net.IPEndPoint ep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipAddressTest), 12345);
        print(ep.Address);
        byte[] addAsBytes = ep.Address.GetAddressBytes();
        for (int i = 0; i < addAsBytes.Length; i++)
        {
            print(addAsBytes[i]);
        }
        ep = new System.Net.IPEndPoint(new IPAddress(addAsBytes), 12345);
        print(ep.Address);
    }
}
