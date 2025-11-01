using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NewConnection
{
    public int playerNumber;
    public bool isActive;
    public ConnectionState connectionState;
    public int sendRate;
    public int expectedPacketNumber;
    public int latestPacketNumberReceived;

    public List<int> missingPackets;
    public Socket socket;
    public IPEndPoint local;
    public IPEndPoint remote;
    public Packet messager;
    public PacketManager playerPackets;


    public NewConnection(MnetNetwork localMnetInstance)
    {
        playerPackets = new PacketManager(Mnet.clientPacketBufferSize, localMnetInstance);
        messager = new Packet();
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
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
