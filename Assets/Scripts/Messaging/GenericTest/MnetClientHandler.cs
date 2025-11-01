using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine;
using System.Buffers.Binary;
using System.Text;
using Unity.VisualScripting;
using JetBrains.Annotations;

/*
    Pelaajan toiminnot serverin puolella, eli bufferissa paketit ja inputit.
    Hoitaa syncin ja rollbackin
 */
public class MnetClientHandler
{
    public WANHAConnectionState connectionState = WANHAConnectionState.NotActive;
    //private MnetServer server;
    public int clientDictionaryKey;
    public byte[] IPasBytes;
    //public ulong clientID;
    public Socket connectionToClient;
    public IPEndPoint handlerEP;
    public IPEndPoint remoteClientEP;
    private float timeoutTimer;

    private MnetPacket connectionEstablisherPacket;
    private MnetPacket incomingPacket;
    private MnetPacket outgoingPacket;
    private int lastTickReceived;
    private MnetPacketBuffer clientActionBuffer;
    private MnetPacketBuffer serverBuffer;
    private MnetPacketBuffer worldSnapshotBuffer;
    //private Queue<short> freeObjectIndex;
    public WANHAMnetObject[] objectsBeingSynced;


    private int currentPacketNumber;
    private int lastTickSize;
    private int currentFrameNumber;
    private float currentFrameTime;

    //private byte[] incomingPacket;
    private bool serverRunning;


    public MnetClientHandler(MnetPacketBuffer serverBuffer, MnetPacketBuffer worldSnapshotBuffer, int clientDictionaryKey, IPAddress localIP, int localPort, IPAddress clientIP, int clientPort)
    {
        this.serverBuffer = serverBuffer;
        this.worldSnapshotBuffer = worldSnapshotBuffer;
        this.clientDictionaryKey = clientDictionaryKey;

        /*
        // Create random id for client that will be used to verify the user
        byte[] randomUlong = new byte[16];
        for (int i = 0; i < randomUlong.Length; i++)
        {
            randomUlong[i] = (byte)UnityEngine.Random.Range(0,255);
        }

        clientID = BinaryPrimitives.ReadUInt64LittleEndian(randomUlong);
        */
        connectionEstablisherPacket = new MnetPacket(false);
        //incomingPacket = new byte[ServerSettings.maxPacketSize];
        clientActionBuffer = new MnetPacketBuffer(Mnet.clientPacketBufferSize);
        lastTickReceived = 0;
        //incomingPacket = clientActionBuffer.Get(las);
        handlerEP = new IPEndPoint(localIP, localPort);
        remoteClientEP = new IPEndPoint(clientIP, clientPort);
        connectionToClient.Bind(handlerEP);
        connectionToClient.Connect(remoteClientEP);
        timeoutTimer = 0f;
    }

    // 
    public void HandShake(float frameTime)
    {
        if (connectionToClient.Available > 0)
        {
            connectionToClient.Receive(connectionEstablisherPacket.AllBytes());

            //string messageReceived = Encoding.ASCII.GetString(connectionEstablisherPacket.Read(Mnet.messageLength));
            string messageReceived = connectionEstablisherPacket.ReadMessage();
            if (messageReceived == Mnet.messageNewConnectionTest)
            {
                // Send back the response
                connectionEstablisherPacket.Reset();
                //Encoding.ASCII.GetBytes(Mnet.messageNewConnectionVerified.AsSpan(), connectionEstablisherPacket.Write(Mnet.messageLength));
                connectionEstablisherPacket.WriteMessage(Mnet.messageNewConnectionVerified);

                for (int i = 0; i < (int)PacketPriority.Important; i++)
                {
                    connectionToClient.Send(connectionEstablisherPacket.CurrentMessage());
                }

                /// Ei voi viel yhist‰‰
                //connectionState = ConnectionState.Connected;
            }
            if (messageReceived == Mnet.messageReadyToStart)
            {
                connectionState = WANHAConnectionState.SyncWorldState;
            }
        }
    }

    public void Receive(float frameTime)
    {
        // Ota vastaan paketit jos available
        if (connectionToClient.Available != 0)
        {
            //connectionToClient.Receive(incomingPacket);
            // Clientit ei jaa paketteja ni tsekkaa frame ja talleta bufferiin
        }
    }

    public void UpdatePlayer(float time)
    {
        /// RECEIVE PACKETS AND UPDATE PLAYER OBJECT WITH INPUT AND SHIT
    }

    public void SendTick()
    {

    }
    
    public void SendFullWorldSnapshot()
    {
        /*
        outgoingPacket = worldSnapshotBuffer.Get(0);
        int packetsToSend = outgoingPacket.extraPacketsInUpdate;
        while (packetsToSend >= 0)
        {
            connectionToClient.Send(outgoingPacket.CurrentMessage());
            outgoingPacket = outgoingPacket.nextPacket;
            packetsToSend--;
        }
        // Player should now have the most up to date snapshot, so they should be in sync again
        connectionState = ConnectionState.Connected;
        */
    }

    public void ResendMissedPackets()
    {

    }

    public void CheckForMessages(float time)
    {

        if (connectionToClient.Available != 0)
        {
            timeoutTimer = 0f;
            while(connectionToClient.Available != 0)
            {
                incomingPacket = clientActionBuffer.Get(lastTickReceived);
                //MessageResult result = (MessageResult)incomingPacket.data[0];

                switch(incomingPacket.Message)
                {
                    case MessageType.Regular:
                        // Liikuta ajalla ja ota paketti taltee
                        break;
                    case MessageType.MissingPacketsRequest:
                        // Kopioi listaa numerot
                        break;
                    //case MessageType.:
                    //    // L‰het‰ koko sync jos viel‰ on yhteys?
                    //    break;
                    case MessageType.DisconnectNotification:
                        if(incomingPacket.ReadMessage() == Mnet.messageDisconnectByClient) { Disconnect(); }
                        break;
                    default:
                        break;
                }
            }
        }
        else
        {
            timeoutTimer += time;
            if (timeoutTimer > Mnet.heartbeatLimit)
            {
                Disconnect();
            }
        }
    }

    public void TickRemoteClient(float time)
    {
        switch (connectionState)
        {
            case WANHAConnectionState.Connected:
                // L‰het‰ uusin paketti vaan
                SendTick();
                break;
            case WANHAConnectionState.MissingPackets:
                // receiving kautta saadaa tieto ni lis‰t‰‰ ne List<short> ja sielt‰ paukautetaan l‰htee bufferista uudestaan ja per‰‰n viimeisin
                ResendMissedPackets();
                SendTick();
                break;
            case WANHAConnectionState.SyncWorldState:
                // L‰het‰ koko state joka sitten on myˆs viimeisin ni per‰‰ ei tarvii l‰hett‰‰
                SendFullWorldSnapshot();
                break;
            case WANHAConnectionState.Handshake:
                HandShake(time);
                break;
            default:
                break;
        }

    }

    public void SendWorldPacket()
    {

    }

    public void Disconnect()
    {
        //connectionToClient.Shutdown(SocketShutdown.Both); /// Uskoisin ettei udp tarvii ku ei oo yhteytt‰ eik‰ streamia
        if (connectionState == WANHAConnectionState.Connected)
        {
            connectionEstablisherPacket.Reset();
            connectionEstablisherPacket[0] = (byte)MessageType.DisconnectNotification;
            connectionEstablisherPacket.WriteMessage(Mnet.messageDisconnectByServer);
            //Encoding.ASCII.GetBytes(Mnet.messageDisconnectByServer.AsSpan(), connectionEstablisherPacket.Span(1, 16));
            for (int i = 0; i < 3; i++)
            {
                connectionToClient.Send(connectionEstablisherPacket.CurrentMessage());
            }
        }
        connectionToClient.Close();
        connectionState = WANHAConnectionState.Disconnected;
    }
}
