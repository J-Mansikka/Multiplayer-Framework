using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Client : MonoBehaviour
{

    public int port;
    public string serverIP;
    public int serverPort;

    private UdpClient client;



    private void Awake()
    {
        client = new UdpClient(port);
    }


}
