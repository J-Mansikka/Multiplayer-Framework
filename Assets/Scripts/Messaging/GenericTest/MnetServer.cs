using JetBrains.Annotations;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


/*
 * 
 * RTT kannattais pist‰‰ headeriin?
 * 
 */
public class MnetServer : MonoBehaviour
{
    //public bool activateMultiTest;
    [Tooltip("Server ip address as a string (E.g. 127.0.0.1)")]
    public string serverAddress;
    private IPAddress serverIP;
    [Tooltip("Port used to listen to new connections")]
    public int serverPort;
    [Tooltip("Object Handler which handles spawn/despawn messaging")]
    public MnetObjectInstanceMessenger objectHandler;
    private MnetPacketBuffer buffer;
    private MnetPacketBuffer worldStateBuffer;

    //public MnetObject[] objectsBeingSynced;         // All the objects that are being syncronized
    public List<MnetObject> objectsBeingSynced;
    Dictionary<uint, MnetRemoteClientConnection> clients;    // Client handlers act as player surrogates on the server. IP addresses act as the unique keys
    //

    private int currentPacketNumber;
    private int lastTickSize;
    private int currentFrameNumber;
    private float currentFrameTime;
    //private List<MnetPacket> writeBuffer;
    private MnetPacket currentPacket;
    private Socket newConnectionSocket;
    private IPEndPoint serverEP;
    //private Thread udpThread;
    private byte[] newConnectionPacketBytes;
    //private long newConnectionCode;
    private bool serverRunning;

    private List<Socket> socks;
    private Socket newSock;
    byte testiInti = 0;

    private float checkClientsTimer;
    private float tickTimer;


    //private bool newConnectionTest = false;
    //private int testPos;

    //private Thread testThread;

    //private static object locker;
    //private int testInt=0;

    private void Awake()
    {
        serverIP = IPAddress.Parse("127.0.0.1");
        //locker = new object();
        if(objectsBeingSynced == null)
        {
            objectsBeingSynced = new List<MnetObject>(ServerSettings.maxSyncedObjects);
            //objectsBeingSynced = new MnetObject[ServerSettings.maxSyncedObjects];
        }

        buffer = new MnetPacketBuffer();
        //// Mihin tallettaa world state bufferin koko? Vois olla sama ku max update size ja lis‰t settinkeihi
        worldStateBuffer = new MnetPacketBuffer(64);
        newConnectionPacketBytes = new byte[28];
        if(objectHandler == null) { objectHandler = FindObjectOfType<MnetObjectInstanceMessenger>(); }
        objectHandler.HandlerSetup(objectsBeingSynced);
    }

    private void Start()
    {
        //StartServer();

        currentFrameTime = Time.realtimeSinceStartup;
        currentFrameNumber = 69;
        //CreatePacket();
        currentFrameNumber++;

        checkClientsTimer = ServerSettings.clientSendRate;
        tickTimer = ServerSettings.serverSendRate;
        //ServerTest();


        /*
        while(printTest.isActive)
        { 
            Debug.Log("-------------------------------------"+numba+" DID IT---------------------------------------");
            Span<byte> butts = printTest.Get();
            for (int u = ServerSettings.headerCombinedLength; u < butts.Length; u++)
            {
                Debug.Log("# " + u + " " + butts[u]);
            }
            printTest = printTest.nextPacket;
            numba++;
        }
        */

        //DebugPrintPacket();

    }

    public void StartServer()
    {
        serverEP = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
        newConnectionSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        newConnectionSocket.Bind(serverEP);
        Debug.Log(newConnectionSocket.LocalEndPoint);
        newConnectionSocket.Blocking = false;

        /*
        testEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 44444);
        testIfMultipleReceive = new Socket(SocketType.Dgram, ProtocolType.Udp);
        testIfMultipleReceive.Bind(testEP);
        testIfMultipleReceive.Blocking = false;
        */

        serverRunning = true;

        /*
        newSock = new Socket(SocketType.Dgram, ProtocolType.Udp);
        socks = new List<Socket>
        {
            newConnectionSocket
        };
        newConnectionSocket.Bind(serverEP);
        newConnectionSocket.Blocking = false;
        serverRunning = true;
        */
        //testPos = currentPacketNumber;
        //udpThread = new Thread(new ThreadStart(ListenTest));
        //testThread = new Thread(new ThreadStart(ListenTest));
        //udpThread.Start();
        //testThread.Start();
    }

    public void FixedUpdate()
    {
        /*
        bool lockTaken = false;
        int timeout = 10;
        try
        {
            Monitor.TryEnter(locker, timeout, ref lockTaken);
            if (lockTaken)
            {
                // The critical section.
                Debug.Log("CAN USE "+testInt);
            }
            else
            {
                // The lock was not acquired.
                Debug.LogError("DOOR STUCK");
            }
        }
        finally
        {
            // Ensure that the lock is released.
            if (lockTaken)
            {
                Monitor.Exit(locker);
            }
        }
        //byte[] testBuff = new byte[1400];

        if (!serverRunning)
        {
            
            try
            {
                Debug.Log(newConnectionSocket.Receive(buffer.Get(currentPacketNumber).Get()));
                if (newConnectionSocket.Receive(buffer.Get(currentPacketNumber).Get()) > 0)
                {
                    Debug.Log("GOT SOMETHIN");
                    currentPacketNumber++;
                    if(testPos - currentPacketNumber == -10)
                    {
                        serverRunning = false;
                        Debug.Log("DONESKI!");
                    }
                }
            }
            catch
            {

            }

        
            
            if (newConnectionTest)
            {
                try
                {
                    if (newSock.Receive(incomingBytes.AsSpan()) > 0)
                    {
                        Debug.Log("NEW SOCK SAYS: ");
                        Debug.Log(System.Text.Encoding.ASCII.GetString(incomingBytes));
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    if (newConnectionSocket.Receive(incomingBytes.AsSpan()) > 0)
                    {
                        string[] newConnection = System.Text.Encoding.ASCII.GetString(incomingBytes).Split('#');
                        IPEndPoint newConnectionEP = new IPEndPoint(IPAddress.Parse(newConnection[0]), int.Parse(newConnection[1]));
                        newConnectionSocket.Connect(newConnectionEP);
                        newSock.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"),54322));
                        newSock.Blocking = false;
                        newConnectionTest = true;
                        newConnectionSocket.Send(System.Text.Encoding.ASCII.GetBytes("127.0.0.1#54322"));
                        newConnectionSocket.Disconnect(true);
                        //newConnectionSocket.Bind(serverEP);
                    }
                }
                catch { }
            }
            
        }

        */
    }

    private void CheckForNewConnections()
    {
        // Check if socket has any packets in its buffer
        while (newConnectionSocket.Available != 0)
        {
            // Take the packet and extract the possible message type identifier
            newConnectionSocket.Receive(newConnectionPacketBytes.AsSpan());
            string newMessageType = Encoding.ASCII.GetString(newConnectionPacketBytes.AsSpan().Slice(0, 16));            

            // If the incoming message was identified as a new connection request, extract ip and port
            if (newMessageType == ServerSettings.messageNewConnectionRequest)
            {
                // IP Addresses are commonly sent in big endian form
                long newIP = BinaryPrimitives.ReadInt64BigEndian(newConnectionPacketBytes.AsSpan().Slice(16, 8));
                int newPort = BinaryPrimitives.ReadInt32LittleEndian(newConnectionPacketBytes.AsSpan().Slice(24, 4));
                uint connectionIdentifier = (uint)newIP;
                // Check if the incoming message was from a new client
                if (!clients.ContainsKey(connectionIdentifier))
                {
                    int newLocalClientPort = serverPort + 1 + clients.Count;
                    MnetRemoteClientConnection newClient = new MnetRemoteClientConnection(this, connectionIdentifier, serverEP.Address, newLocalClientPort, new IPAddress(newIP), newPort);
                    clients.Add(connectionIdentifier, newClient);

                }
                // Replace the ip address and port values on the handshake message with the valid clientHandler ones
                BinaryPrimitives.WriteInt64BigEndian(newConnectionPacketBytes.AsSpan().Slice(10,8), clients[connectionIdentifier].localIP);
                BinaryPrimitives.WriteInt32LittleEndian(newConnectionPacketBytes.AsSpan().Slice(18,4), clients[connectionIdentifier].localPort);
                
                // Send the changed handshake message back to the client with optional redundancy copies
                for (int i = 0; i < ServerSettings.redundantCopiesHandshake; i++)
                {
                    newConnectionSocket.SendTo(newConnectionPacketBytes, clients[connectionIdentifier].remoteEP);
                }
            }

        }
        
    }

    private void UpdatePlayerHandlers(float frameTime)
    {
        foreach (MnetRemoteClientConnection client in clients.Values)
        {
            client.PlayerUpdate(frameTime);
        }
    }

    public void DisconnectPlayer(uint identifier)
    {
        clients.Remove(identifier);
    }

    private void Tick()
    {
        foreach (MnetObject obj in objectsBeingSynced)
        {
            obj.Tick();
        }
    }

    private void SendWorldStateToPlayers()
    {
        foreach(MnetRemoteClientConnection client in clients.Values)
        {
            client.SendWorldPacket();
        }
    }

    private void LateUpdate()
    {
        //ListenTest();

        if (serverRunning)
        {
            tickTimer -= Time.deltaTime;
            checkClientsTimer -= Time.deltaTime;

            if (checkClientsTimer <= 0f)
            {
                CheckForNewConnections();
                UpdatePlayerHandlers(Time.deltaTime);
                checkClientsTimer += ServerSettings.clientSendRate;
            }
            if (tickTimer <= 0f)
            {
                Tick();
                //Debug.Log("TIMES WERE " + tickTimer + " and framtime " + (ServerSettings.serverSendRate - tickTimer));
                CreatePacket(ServerSettings.serverSendRate - tickTimer);
                SendWorldStateToPlayers();
                tickTimer += ServerSettings.serverSendRate;
            }
        }
        // Check and get player packets = Luultavasti aina enint‰‰n yksi paketti vaikka olisi vanhat per‰ss‰
        // if tickTimer = ota talteen ja l‰het‰ mukaan? Mit‰ jos kaikki liikkeet lasketaan serverin puolella? Tarvitaanko kelloa?
        // Tick
        // Send world state packets

        // Client side aikalailla sama
        // Check and get world packets
        // tickTimer
        // tick
        // inputTimer
        // Get input
        // Apply input
        // Store input
        // inputSendTimer
        // Send input + old inputs
    }

    public void ListenTest()
    {
        /*
        lock (locker)
        {
            for (int i = 0; i < 2147483647; i++)
            {
                testInt++;
            }
        }

        Debug.Log("HOFDFS " + testInt);
        */
        //while (serverRunning)
        //{
            //try
            //{
            /*
            if (newConnectionSocket.Available > 0)
            {
                newConnectionSocket.Receive(incomingBytes.AsSpan());
            if(testiInti != incomingBytes[0])
            {
                Debug.Log("EXPECTED " + testiInti + " BUT GOT " + incomingBytes[0]);
            }
            testiInti++;
            if(testiInti == 10)
            {
                testiInti = 0;
            }
            }
            */
            //}
            //catch{ }
            
            //Thread.Sleep(5);
        //}
        
    }

    private void OnDisable()
    {
        //serverRunning = false;
        //if (udpThread != null)
        //{
        //    Debug.Log("Thread aborted...");
        //    udpThread.Abort();
        //}
    }

    public void DebugPrintPacket()
    {
        int count = buffer.Get(0).extraPacketsInUpdate;
        //testPrint.ReadChanges(buffer.Get(0).Get().Slice(ServerSettings.headerCombinedLength + 4));
        //testPrint.DebugPrintOut();
        
        /*
        MnetPacket packetToPrint = buffer.Get(0);
        bool runnin = true;
        while (runnin)
        {
            Span<byte> packetHeader = packetToPrint.GetHeader();
            Debug.Log("PACKET: " + BinaryPrimitives.ReadInt32LittleEndian
                (packetHeader.Slice(ServerSettings.headerPacketNumberPosition, 4)));
            Debug.Log("SIZE: " +
            BinaryPrimitives.ReadInt16LittleEndian(packetHeader.Slice(ServerSettings.headerSizePosition, 2)));
            Debug.Log("FRAME TIME: " +
            BitConverter.ToSingle(packetHeader.Slice(ServerSettings.headerTimePosition, 4)));
            Debug.Log("FRAME NUMBER: " +
            BinaryPrimitives.ReadInt16LittleEndian(packetHeader.Slice(ServerSettings.headerFrameNumberPosition, 4)));
            Span<byte> packetData = packetToPrint.GetData();
            for (int i = 0; i < packetData.Length; i+=4)
            {
                Debug.Log("VALUE "+(i/4)+" IS: "+BinaryPrimitives.ReadInt32LittleEndian(packetData.Slice(i)));
            }

            if (!packetToPrint.nextPacket.isActive)
            {
                runnin = false;
            }
            else
            {
                packetToPrint = packetToPrint.nextPacket;
            }
        }
        */
    }

    private void CreatePacket(float frameTime, bool getWorldState = false)
    {
        //int remainingBytes = 0;
        // Turha alotus check?
        //bool needToSplit = false;



        // TARVIIKO NƒITƒ MIHINKƒƒN?
        //
        /*
        for (int i = 0; i < objects.Length; i++)
        {
            remainingBytes += objects[i].currentSize;
        }
        */
        //if (remainingBytes > ServerSettings.maxPacketSize) needToSplit = true;

        //int extraPackets = 0;
        if (currentPacket == null)
        {
            currentPacket = buffer.Get(0);
            currentPacket.isActive = true;
        }

        MnetPacket activePacket;
        MnetPacket firstPacketInUpdate;

        //// S‰‰det‰‰n mihin bufferiin/toimintoon n‰‰ kuuluu, joko world state p‰ivitys tai normi
        if (getWorldState)
        {
            firstPacketInUpdate = worldStateBuffer.Get(0);
        }
        else
        {
            firstPacketInUpdate = currentPacket;
        }

        activePacket = firstPacketInUpdate;
        int currentPacketID = 0;
        int numberOfExtraPacketsNeeded = 0;
        //int currentObjectIndex = 0;
        //MnetObject activeObject;
        //int bufferRemaining = writeBuffer.Count;
        //
        //
        //
        //


        int infLoop = 1000;

        /*
        foreach (MnetPacket packet in writeBuffer)
        {
            packet.Reset();
        }
        */

        //// Napataan seuraava objecti jos on olemassa ja seuraavassa loopissa k‰sitell‰‰n
        //while (currentObjectIndex < objectsBeingSynced.Length)
        foreach(MnetObject activeObject in objectsBeingSynced)
        {
            //activeObject = objectsBeingSynced[currentObjectIndex];
            /*
            //// TYhj‰ slot, NEXT!
            if(activeObject == null)
            {
                currentObjectIndex++;
                continue;
            }
            */
            bool processingObject;
            short sizeOfObject;
            if (getWorldState)
            {
                processingObject = true;
                sizeOfObject = activeObject.GetCurrentTotalSize();
            }
            else
            {
                processingObject = activeObject.hasUpdated;
                sizeOfObject = activeObject.currentSize;
            }

            //// Niin kauan ku objeti tarvii kodin ni loopataan ja etit‰‰n pakettia johon mahtuu
            while (processingObject)
            {
                //// Katotaa mahtuuko objekti edes pakettiin
                if (sizeOfObject <= (ServerSettings.maxPacketSize - activePacket.currentLength))
                {
                    //// Mahtuu eli otetaan paketista loput tavut ja kirjotetaan objekti niihin
                    activeObject.WriteChanges(activePacket.GetRemainingSpace(), getWorldState);
                    activePacket.currentLength += sizeOfObject;
                    // Start over with next object if remaining
                    currentPacketID = 0;
                    //// Objecti mahtu niin palataan ekaan pakettiin ja alotetaan alusta
                    activePacket = firstPacketInUpdate;
                    processingObject = false;
                }
                else
                {
                    currentPacketID++;
                    activePacket = activePacket.nextPacket;
                    if (currentPacketID > numberOfExtraPacketsNeeded)
                    {
                        numberOfExtraPacketsNeeded++;
                        activePacket.Reset();
                        activePacket.isActive = true;
                    }
                }
            }
            /*
            currentObjectIndex++;
            infLoop--;

            if (infLoop == 0)
            {
                print("INF LOOP DINGUS");
                break;
            }
            */

        }
        firstPacketInUpdate.extraPacketsInUpdate = numberOfExtraPacketsNeeded;

        // LOOPS TO CREATE PACKETS
        //
        // GET PACKET FROM BUFFER
        // SET MOST OF HEADER
        // FILL UNTIL CANNOT FIT MORE
        // CREATE NEW IF NO PACKETS CAN FIT
        // FOLLOW HOW MANY PACKETS ARE TO BE SEND AND FROM WHICH INDEX
        // CALL SEND FROM MAIN METHOD

        // GET PACKET
        // SET DATA
        // SET HEADER
        // - SIZE HELPPO
        // - FRAMETIME KAIKIL SAMA
        // FRAME # SUORAA INDEX
        // FRAME OUT OF: PAKETTIEN MƒƒRƒ

        //PACKET_#
        //SIZE
        //FRAMETIME
        //FRAME_#
        //- IF SPLIT(viiminen bitti flag)
        //- #
        //- out of
        //    - IF Supersize(viiminen bitti flag sizessa)

        //    SUPERSIZE
        //    -#
        //	-OUT OF #
        //DATA
        lastTickSize = numberOfExtraPacketsNeeded + 1;
        activePacket = firstPacketInUpdate;
        for (int i = 0; i <= numberOfExtraPacketsNeeded; i++)
        {
            Span<byte> packetNeedingHeader = activePacket.GetHeader();
            BinaryPrimitives.WriteInt32LittleEndian(packetNeedingHeader.Slice(ServerSettings.headerPacketNumberPosition, 4)
                , currentPacketNumber++);//currentPacketNumber + i);
            BinaryPrimitives.WriteInt16LittleEndian(packetNeedingHeader.Slice(ServerSettings.headerSizePosition, 2)
                , activePacket.currentLength);
            // Unity does not support all BinaryPrimitives' methods, so BitConverter is used as a substitute
            BitConverter.TryWriteBytes(packetNeedingHeader.Slice(ServerSettings.headerTimePosition, 4), currentFrameTime);
            BinaryPrimitives.WriteInt32LittleEndian(packetNeedingHeader.Slice(ServerSettings.headerTickNumberPosition, 4),
                currentFrameNumber);

            // If the frame needs multiple packets, add sequence number and total number of packets.
            // If singular packet, set values to zero.
            packetNeedingHeader[ServerSettings.headerFrameSplitPosition] = 
                (numberOfExtraPacketsNeeded > 0) ? (byte)i : (byte)0;
            packetNeedingHeader[ServerSettings.headerFrameSplitPosition+1] = 
                (numberOfExtraPacketsNeeded > 0) ? (byte)numberOfExtraPacketsNeeded : (byte)0;
            //buffer.Add(currentPacketNumber + i, packetNeedingHeader);
            activePacket = activePacket.nextPacket;
        }
        currentPacket.extraPacketsInUpdate = numberOfExtraPacketsNeeded;
        //// Jos oli normi p‰ivitys ni tiedet‰‰n mist‰ jatkaaa seuraavassa rundissa
        if(!getWorldState) currentPacket = activePacket;        
    }
}


