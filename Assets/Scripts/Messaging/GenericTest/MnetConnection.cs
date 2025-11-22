using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class MnetConnection
{
    // Used to toggle if the the connection packets are read and their data used
    public bool isActive;
    // !!! Numba joo selkee. On myös indexi eli UI pitää olla number + 1 tai sitte kasvattaa tän jo yhel ja indexi haussa sitte - 1
    public int playerNumber;
    // Connection ID is just the address bytes converted to an integer
    //  !!! Ei taida olla käytös?
    public int connectionID;
    //
    public IPEndPoint localEP;
    // Remote endpoint we connect to
    public IPEndPoint remoteEP;
    // We use this enum to keep track of the connection state
    public WANHAAConnectionState state;
    // How many duplicates of the packets we send. On bad or slow connections we can increase this amount to make sure all packets will come through
    public int sendRate;
    // Objects that the remote has control over
    //public MnetObject[] remoteObjects;
    // Index numbers of active objects controlled by remote
    public HashSet<int> playerObjectIDs;
    // Last received tick of the remote user
    public int tick;
    // Next expected packet number from the remote
    public int expectedPacketNumber;
    // Next packet number to process
    public int nextPacketToProcessNumber;
    // Everytime the messager does not receive a packet from the connection, we will increment this counter to monitor the delay
    public int missedHeartbeatCount;
    // Last tick the remote user requested for a snapshot. Snapshot is a heavier process, so use this to filter out unnecessary requests
    public int snapshotRequestTick;
    // Packet numbers of those that we were expecting, but did not receive
    public HashSet<int> missingPackets;
    // We need to know when the snapshot update has arrived in full so we can process it
    public HashSet<int> snapshotPacketsLeft;
    // When we have received all the snapshot packets, we will move the processed packet position on this number
    public int firstPacketInSnapshot = -1;
    // Socket that is used to communicate with the remote user
    public Socket socket;
    // Buffer that contains the received packets from the remote user
    public MnetPacketBuffer playerBuffer;
    /// Tänne vois sitte lisätä bool connected etc?
    /// Jos saa messageri toimimaan molemmil servul ja clientil ilma isompaa ongelmaa ni tää vois olla se erotus paikka?

    // UURET
    public PacketManager playerPackets;
    public Packet messager;
    public int latestPacketNumberReceived;
    public ConnectionState connectionState;

    public MnetConnection(int playerNumber, IPEndPoint endpoint, MnetNetwork owner, int bufferSize = Mnet.serverPacketBufferSize, int setSendRate = 1)
    {
        missingPackets = new HashSet<int>();
        snapshotPacketsLeft = new HashSet<int>();
        connectionID = BitConverter.ToInt32(endpoint.Address.GetAddressBytes());
        localEP = endpoint;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        socket.Bind(endpoint);

        playerBuffer = new MnetPacketBuffer(bufferSize);
        snapshotRequestTick = 0;
        sendRate = setSendRate;
        playerPackets = new PacketManager(Mnet.clientPacketBufferSize, owner, PacketType.RegularUpdate, false, 69);
        messager = new Packet(PacketType.SystemMessage);
    }

    public void ConnectTo(IPEndPoint newEndpoint)
    {
        remoteEP = newEndpoint;
        socket.Connect(newEndpoint);
    }
}
