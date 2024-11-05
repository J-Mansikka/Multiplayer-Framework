using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;

public class MnetClient : MonoBehaviour
{
    public string serverIP;
    public int serverPort;
    public string clientName;
    private Socket client;
    private IPEndPoint clientEP;
    private IPEndPoint serverEP;
    private Thread udpThread;
    private byte[] outgoingBytes;
    private bool serverRunning;

    private bool running;
    private bool tryingToConnect;
    private byte[] incomingBytes;

    private byte[][] testBytes;
    int retries = 1;

    public MnetObject objects;

    private void Start()
    {
        incomingBytes = new byte[1400];
        clientEP = new IPEndPoint(IPAddress.Any, 44444);
        client = new Socket(SocketType.Dgram, ProtocolType.Udp);
        client.SendBufferSize = 65535;
        client.Bind(clientEP);
        client.Blocking = false;

        testBytes = new byte[10][];

        for (int i = 0; i < 10; i++)
        {
            testBytes[i] = new byte[1200];
            for (int b = 0; b < 1200; b++)
            {
                testBytes[i][b] = (byte)(((b + 1) * (i + i) / 2));
            }
        }

        ConnectToServer();
    }

    public void ConnectToServer()
    {
        serverEP = new IPEndPoint(IPAddress.Parse(serverIP), 54321);
        client.Connect(serverEP);
        //byte[] sendBytes = System.Text.Encoding.ASCII.GetBytes(clientName);
        //byte[] fuckThisShit = new byte[160];
        //client.Send(fuckThisShit.AsSpan());
        //client.Send(sendBytes.AsSpan());
        running = true;
        tryingToConnect = true;
        //ListenForMessages();
    }

    private void FixedUpdate()
    {
        if (running)
        {
            //Debug.Log(testBytes[0].Length);
            //client.Send(testBytes[0].AsSpan());

            for (int i = 0; i < 10; i++)
            {
                client.Send(testBytes[i].AsSpan());
            }
            //running = false;




            /*
            if (!tryingToConnect)
            {

                try
                {
                    if (client.Receive(incomingBytes.AsSpan()) > 0)
                    {
                        Debug.Log(System.Text.Encoding.ASCII.GetString(incomingBytes));
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    if (client.Receive(incomingBytes.AsSpan()) > 0)
                    {
                        string[] newConnection = System.Text.Encoding.ASCII.GetString(incomingBytes).Split('#');
                        IPEndPoint newConnectionEP = new IPEndPoint(IPAddress.Parse(newConnection[0]), int.Parse(newConnection[1]));
                        //Debug.Log(newConnectionEP);
                        client.Connect(newConnectionEP);
                        client.Send(System.Text.Encoding.ASCII.GetBytes("YEAH"));
                        tryingToConnect = false;
                    }
                }
                catch { }
            }
            */
        }
    }
}
