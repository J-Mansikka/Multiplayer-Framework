using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class ThreadServer : MonoBehaviour
{
    public bool acceptNewConnections;
    public string serverIP;
    public int port;
    private UdpClient newConnectionsListener;
    private UdpClient testServersideClientListener;
    private IPEndPoint allNewConnectionsEP;
    private IPEndPoint testClientEP;
    private Thread threadForNewConnections;
    private List<Thread> testServersideClients;
    private byte[] receivedBuffer;

    public TMP_Text printTest;
    private string newTestMessage = "";
    private float checkMessageDelay = 0f;

    private static readonly object receivedBufferLock = new object(); 


    private void Awake()
    {
        //receivedBufferLock = new object();
    }

    private void Start()
    {
        if (acceptNewConnections)
        {
            testServersideClients = new List<Thread>();
            threadForNewConnections = new Thread(new ThreadStart(ListenForNewConnections));
            threadForNewConnections.Start();
        } 
    }

    private void Update()
    {
        checkMessageDelay += Time.deltaTime;

        if (checkMessageDelay > 1f)
        {
            checkMessageDelay = 0f;
            lock (receivedBufferLock)
            {
                if (newTestMessage.Length > 0)
                {
                    printTest.text += " " + newTestMessage;
                    newTestMessage = "";
                }
            }
        }
    }

    private void CreateNewTestClient()
    {
        Thread testServersideClient = new Thread(new ThreadStart(TestClientListen));
        testServersideClients.Add(testServersideClient);
        testServersideClient.Start();
    }

    private void TestClientListen()
    {
        
        testServersideClientListener = new UdpClient();
        IPEndPoint clientEP = testClientEP;
        //testServersideClientListener.Client.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"),54321));
        //testServersideClientListener.Client.Listen(54321);
        if (testClientEP != null)
        {
            Debug.Log("Connected to new client " + testClientEP.Address);
            testServersideClientListener.Connect(testClientEP);
            testClientEP = null;
        }

        byte[] newIncomingPacket = new byte[0];

        while (true)
        {
            // Try Catch should go here ehkä
            newIncomingPacket = newConnectionsListener.Receive(ref clientEP);
            Debug.Log("ClientListener to "+clientEP+" got " + Encoding.ASCII.GetString(newIncomingPacket));
        }
    }


    private void ListenForNewConnections()
    {
        allNewConnectionsEP = new IPEndPoint(IPAddress.Any, port);
        newConnectionsListener = new UdpClient(allNewConnectionsEP);
        IPEndPoint newPossibleClientEP = new IPEndPoint(IPAddress.Any,0);
        byte[] newIncomingPacket = new byte[0];

        while (acceptNewConnections)
        {
            // Try Catch should go here ehkä
            newIncomingPacket = newConnectionsListener.Receive(ref newPossibleClientEP);
            string message = Encoding.ASCII.GetString(newIncomingPacket);
            Debug.Log("RECEIVED "+message+" FROM "+newPossibleClientEP.ToString());

            if(message == "connect test")
            {
                testClientEP = newPossibleClientEP;
                CreateNewTestClient();
            }
            else
            {
                lock (receivedBufferLock)
                {
                    newTestMessage = message;
                }
            }
        }
    }

    private void OnDisable()
    {
        foreach (Thread thrd in testServersideClients)
        {
            thrd.Abort();
        }
    }
}
