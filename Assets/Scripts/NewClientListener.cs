using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class NewClientListener
{
    /*
    
    Server owner;
    int port;
    int maxAttempts;
    float timeoutLimit;
    udpClient listener;
    incomingConnections[];

    Init()

    StartListening()

    VerifyConnectionAttempt()

    StoreNewConnection()
    {owner.SetupNewConnection}

    Shutdown()


    */

    private UdpServer server;
    private UdpClient listener;
    public Queue<string> messageBuffer;
    //private int listenPort;
    IPEndPoint listenEP;
    bool listening;
    //Dictionary<IPEndPoint, int> connectionAttemps;
    //HashSet<IPAddress> acceptedConnections;

    public NewClientListener(UdpServer ownerServer, int port)
    {
        server = ownerServer;
        listenEP = new IPEndPoint(IPAddress.Any, port);
        listener = new UdpClient(listenEP);
        //connectionAttemps = new Dictionary<IPEndPoint, int>(); 
    }

    public void StartListening()
    {
        listening = true;
        ListenForNewConnections();
    }

    private void ListenForNewConnections()
    {

        Debug.Log("Server is now listening for new connections...");
        listener.BeginReceive(new AsyncCallback(ReceiveNewConnection), messageBuffer);

    }



    public void ReceiveNewConnection(IAsyncResult ar)
    {
        Queue<string> buffer = ar.AsyncState as Queue<string>;
        Debug.Log("GOT MESSAGE");
        IPEndPoint newEndpoint = new IPEndPoint(IPAddress.Any,0);
        byte[] receiveBytes = listener.EndReceive(ar, ref newEndpoint);
        string receivedMessage = Encoding.UTF8.GetString(receiveBytes);

        //buffer.Enqueue(receivedMessage);

        
        ListenForNewConnections();
    }

    /*
    public void RemoveFromStoredConnections(IPAddress disconnected)
    {
        acceptedConnections.Remove(disconnected);
    }
    */

    public void ShutDown()
    {
        listening = false;
        listener.Close();
    }
















    

}
