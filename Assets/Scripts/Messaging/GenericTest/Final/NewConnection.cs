using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NewConnection
{
    public string ipAddress;
    public int playerNumber;
    public bool isActive;       // Client is active and trying to connect into game
    public bool isConnected;    // Client has connected and is going to receive gamestate packets
    public ConnectionState state;
    public int sendRate;
    //public int expectedPacketNumber;
    //public int latestPacketNumberReceived;
    //public int playerTickNumber;
    public int timeoutCounter;

    //public List<int> missingPackets;
    public Socket socket;
    public IPEndPoint local;
    public IPEndPoint remote;
    public Packet messager;
    //public Packet received;
    //public PacketManager activeManager;
    public Packet receivedPacket;
    public PacketManager playerPackets;
    //public NewObject[] playerObjects;


    public NewConnection(MnetNetwork localMnetInstance, bool serverSidePlayerConnection, int playerNumber )
    {
        timeoutCounter = 0;
        //playerTickNumber = 1;
        this.playerNumber = playerNumber;
        playerPackets = new PacketManager(Mnet.clientPacketBufferSize, localMnetInstance, PacketType.RegularUpdate, serverSidePlayerConnection, playerNumber); // Clients do not create snapshot updates
        messager = new Packet(PacketType.SystemMessage);
        state = ConnectionState.Handshake;
        receivedPacket = new Packet(PacketType.RegularUpdate);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        sendRate = 1;
        //playerObjects = new NewObject[Mnet.objectsPerPlayer];
    }

    public void SetLocalEndpoint(IPEndPoint localEP)
    {
        local = localEP;
        socket.Bind(localEP);
    }

    public void ConnectTo(IPEndPoint remoteEP)
    {
        remote = remoteEP;
        socket.Connect(remoteEP);
    }

}
