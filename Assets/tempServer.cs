using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class tempServer
{

    bool listenForConnections;

    /*
    
    tickRate
    // Arrayt vois olla niin ett� index on sama ku clientID, mut pit�� varautua siihen jos pelaaja l�htee/putoo v�list�. Tai ehk� vaan pit�� lista josta k�yd� muut taulukot l�pi niin
    // ett� pystyy helposti skippaa tyhj�t.
    timeSinceLastPacketFromClient?[clientID]?[int]
    clientAcknowledgements?[clientID]?[ACKs]

    ListenForConnections()          // Listen ja Receive vois olla sama metodi, mutta ne on sen verran erillaiset et antaa olla erilliset.
    CreateNewConnectionToClient()
    ReceivePackets()
    SendPackets()
    Tick/FixedUpdate()              // FixedUpdate vois olla k�tev� niin pelin p�ivitys olis samassa mist� heti l�htee send ja ratea voi s��t�� editorista.
                                    //// Fixed update kutsutaan vaikka kaikki fysiikat olis otettu pois p��lt�.
     
     
     
     */
    private int port;
    Socket listenerSocket;

    public tempServer(int portToUse)
    {
        this.port = portToUse;
        InitListener();
    }

    private void InitListener()
    {
        listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp);
        IPEndPoint listenerEP = new IPEndPoint(IPAddress.Any, port);
        listenerSocket.Bind(listenerEP);
        listenerSocket.Listen(10);
        listenForConnections = true;
        ListenForNewConnections();

    }

    private async void ListenForNewConnections()
    {
        while (listenForConnections)
        {
            Socket incomingConnection = await listenerSocket.AcceptAsync();
            //VerifyConnection(incomingConnection);
        }
    }

    private async void VerifyConnection(Socket newPossibleConnection)
    {
        int waitForConnectionID = -1;

        while (waitForConnectionID > 0)
        {
            ArraySegment<byte> testBuffer;
            //newPossibleConnection.ReceiveAsync();
            waitForConnectionID--;
        }

    }



    private void ListenForNewClients()
    {

    }

}
