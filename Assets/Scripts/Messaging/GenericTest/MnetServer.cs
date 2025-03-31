using JetBrains.Annotations;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


/*
 * 
 * RTT kannattais pistää headeriin?
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
    public MnetObjectStateHandler objectHandler;
    private MnetPacketBuffer buffer;
    private MnetPacketBuffer worldSnapshotBuffer;
    //private MnetPacketBuffer worldStateBuffer;

    //public MnetObject[] objectsBeingSynced;         // All the objects that are being syncronized
    public List<MnetObject> objectsBeingSynced;
    Dictionary<int, MnetClientHandler> clients;    // Client handlers act as player surrogates on the server. IP addresses act as the unique keys
    private List<int> disconnectedClients;

    private int currentPacketNumber;
    private int lastTickSize;
    private int currentTickNumber;
    private float currentTickDuration;
    private int worldStateTickVersion;          // Current tick number of stored world snapshot in buffer, so we only do it once if multiple need it
    //private List<MnetPacket> writeBuffer;
    private MnetPacket currentPacket;
    private Socket newConnectionSocket;
    private IPEndPoint serverEP;
    //private Thread udpThread;
    private MnetPacket newConnectionPacket;
    //private long newConnectionCode;
    private bool serverRunning;

    HashSet<short> objectsMarkedImportant;

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
        currentTickNumber = 0;
        serverIP = IPAddress.Parse("127.0.0.1");
        //locker = new object();
        objectsBeingSynced = new List<MnetObject>(MnetSettings.maxSyncedObjects);
        disconnectedClients = new List<int>();
            //objectsBeingSynced = new MnetObject[ServerSettings.maxSyncedObjects];
        buffer = new MnetPacketBuffer();
        worldSnapshotBuffer = new MnetPacketBuffer(MnetSettings.worldStatePacketBufferSize);
        //// Mihin tallettaa world state bufferin koko? Vois olla sama ku max update size ja lisät settinkeihi
        newConnectionPacket = new MnetPacket(false); //new byte[28];
        objectHandler.HandlerSetup(objectsBeingSynced);
        objectsMarkedImportant = new HashSet<short>();
    }

    private void Start()
    {
        //StartServer();

        currentTickDuration = Time.realtimeSinceStartup;

        checkClientsTimer = 0f;
        tickTimer = 0f;
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

    // !! ! ! MOST MAGIC NUMEROT VOI PERKELE SAAAAAAAAAAAAAAATANA!
    // AIKA LUKKOO LYÖTY TÄÄ OSA ETTEI TARVII USERIN EHKÄ MUUTTAA MUTTA NIMEE AINAKI PARAMETRIT VITTU JEESUS KRISTSUS
    private void CheckForNewConnections()
    {
        // Check if socket has any packets in its buffer
        while (newConnectionSocket.Available != 0)
        {
            // Take the packet and extract the possible message type identifier
            newConnectionSocket.Receive(newConnectionPacket.Span());
            string newMessageType = Encoding.ASCII.GetString(newConnectionPacket.Span(0, 16));            

            // If the incoming message was identified as a new connection request, extract ip and port
            if (newMessageType == MnetSettings.messageClientNewConnectionRequest)
            {
                // Add return message identifier
                Encoding.ASCII.GetBytes(MnetSettings.messageServerNewConnectionResponse.AsSpan(), newConnectionPacket.Span());
                // IP Addresses are commonly sent in big endian form
                IPAddress newIP = new IPAddress(newConnectionPacket.Span(16, 4));
                int newPort = BinaryPrimitives.ReadInt32LittleEndian(newConnectionPacket.Span(20, 4));
                int connectionIdentifier = BinaryPrimitives.ReadInt32LittleEndian(newConnectionPacket.Span(16, 4));
                // Check if the incoming message was from a new client
                if (!clients.ContainsKey(connectionIdentifier))
                {
                    int newLocalClientPort = serverPort + 1 + clients.Count;
                    MnetClientHandler newClient = new MnetClientHandler(buffer, worldSnapshotBuffer
                        ,connectionIdentifier, serverEP.Address, newLocalClientPort, newIP, newPort);
                    clients.Add(connectionIdentifier, newClient);

                }
                // Replace the ip address and port values on the handshake message with the valid clientHandler ones
                clients[connectionIdentifier].handlerEP.Address.TryWriteBytes(newConnectionPacket.Span(16, 4), out _);
                //BinaryPrimitives.WriteInt64BigEndian(newConnectionPacketBytes.AsSpan().Slice(16,8), clients[connectionIdentifier].remoteEP.Address.GetAddressBytes().AsSpan());
                BinaryPrimitives.WriteInt32LittleEndian(newConnectionPacket.Span(20,4), clients[connectionIdentifier].handlerEP.Port);

                // Send the changed handshake message back to the client with optional redundancy copies
                // SendTo in Standard 2.1 does not support span, so it is necessary to get the actual bytes from packet
                for (int i = 0; i < MnetSettings.redundantCopiesHandshake; i++)
                {
                    newConnectionSocket.SendTo(newConnectionPacket.Data, clients[connectionIdentifier].remoteClientEP);
                }
            }

        }
        
    }


    /// <summary>
    /// In this method the server will call update on the players who are still connected and checks if there are some
    /// who have disconnected. They will then be removed.
    /// </summary>
    /// <param name="frameTime">Frame time</param>
    private void UpdatePlayerHandlers()
    {
        foreach (MnetClientHandler client in clients.Values)
        {
            if(client.connectionState == ConnectionState.Disconnected)
            {
                disconnectedClients.Add(client.clientDictionaryKey);
            }
            else
            {
                client.CheckForMessages(Time.deltaTime);
            }
        }

        if(disconnectedClients.Count > 0) 
        {
            foreach (int disconnectedClient in disconnectedClients)
            {
                clients.Remove(disconnectedClient);
            }
            disconnectedClients.Clear();
        }
    }

    private void Tick()
    {
        foreach (MnetObject obj in objectsBeingSynced)
        {
            obj.Tick();
        }
    }

    private void CreatePacketFromTick(float time)
    {

        // Create packet with only the changes between ticks
        CreatePacket(time);
        foreach (MnetClientHandler client in clients.Values)
        {
            // In case one of the players needs a full update, create one
            if (client.connectionState == ConnectionState.SyncWorldState && currentTickNumber != worldStateTickVersion)
            {
                CreatePacket(time, true);
            }

            // Call update on all players so they can send the packets
            client.TickRemoteClient(time);
        }

    }

    private void LateUpdate()
    {
        //ListenTest();

        /// joku parempi tähä ku yks booli? VAi tarviiko serveri edes? Onko sillä state vai aina vaan hommissa tai nukkuu
        if (serverRunning)
        {
            tickTimer += Time.deltaTime;
            checkClientsTimer += Time.deltaTime;

            if (checkClientsTimer >= MnetSettings.clientSendRate)
            {
                CheckForNewConnections();
                UpdatePlayerHandlers();
                checkClientsTimer -= MnetSettings.clientSendRate;
            }
            if (tickTimer >= MnetSettings.serverSendRate)
            {
                Tick();
                CreatePacketFromTick(tickTimer - MnetSettings.serverSendRate);
                tickTimer -= MnetSettings.serverSendRate;
            }
        }
        // Check and get player packets = Luultavasti aina enintään yksi paketti vaikka olisi vanhat perässä
        // if tickTimer = ota talteen ja lähetä mukaan? Mitä jos kaikki liikkeet lasketaan serverin puolella? Tarvitaanko kelloa?
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

    private void CreatePacket(float frameTime, bool createFullSnapshot = false)
    {
        //int remainingBytes = 0;
        // Turha alotus check?
        //bool needToSplit = false;



        // TARVIIKO NÄITÄ MIHINKÄÄN?
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

        //// Säädetään mihin bufferiin/toimintoon nää kuuluu, joko world state päivitys tai normi
        if (createFullSnapshot)
        {
            firstPacketInUpdate = worldSnapshotBuffer.Get(0);
            firstPacketInUpdate.PacketType = MessageType.Snapshot;
        }
        else
        {
            firstPacketInUpdate = currentPacket;
            firstPacketInUpdate.PacketType = MessageType.Regular;
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

        /*
        foreach (MnetPacket packet in writeBuffer)
        {
            packet.Reset();
        }
        */

        //// Napataan seuraava objecti jos on olemassa ja seuraavassa loopissa käsitellään
        //while (currentObjectIndex < objectsBeingSynced.Length)

        /* AALKUPERÄNE

        foreach(MnetObject activeObject in objectsBeingSynced)
        {

            bool processingObject;
            short sizeOfObject;
            if (createFullSnapshot)
            {
                processingObject = true;    // Snapshotissa tarvitaa kaikki tieto oli sitte muuttunu tai ei viime frames
                sizeOfObject = activeObject.UpdateCurrentSize();  // Tarvitaa koko koko (hoho hoho) ku otetaan objekti kokonaisena
            }
            else
            {
                processingObject = activeObject.hasUpdated;
                sizeOfObject = activeObject.currentSize;
            }

            //// Niin kauan ku objeti tarvii kodin ni loopataan ja etitään pakettia johon mahtuu
            while (processingObject)
            {
                //// Katotaa mahtuuko objekti edes pakettiin
                if (sizeOfObject <= (ServerSettings.maxPacketSize - activePacket.currentLength))
                {
                    //// Mahtuu eli otetaan paketista loput tavut ja kirjotetaan objekti niihin
                    activeObject.WriteChanges(activePacket.AvailableSpace(), createFullSnapshot);
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
        // FRAME OUT OF: PAKETTIEN MÄÄRÄ

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
            if (createFullSnapshot)
            {
                activePacket.PacketType = MessageType.FullWorldUpdate;
            }
            else
            {
                activePacket.PacketType = MessageType.Normal;
            }

            Span<byte> packetNeedingHeader = activePacket.PacketHeader();
            BinaryPrimitives.WriteInt32LittleEndian(packetNeedingHeader.Slice(ServerSettings.headerServerPacketNumberPosition, 4)
                , currentPacketNumber++);//currentPacketNumber + i);
            BinaryPrimitives.WriteInt16LittleEndian(packetNeedingHeader.Slice(ServerSettings.headerServerSizePosition, 2)
                , activePacket.currentLength);


            // Unity does not support all BinaryPrimitives' methods, so BitConverter is used as a substitute
            BitConverter.TryWriteBytes(packetNeedingHeader.Slice(ServerSettings.headerServerTickTimePosition, 4), currentTickDuration);
            BinaryPrimitives.WriteInt32LittleEndian(packetNeedingHeader.Slice(ServerSettings.headerServerTickNumberPosition, 4),
                currentTickNumber);

            // If the frame needs multiple packets, add sequence number and total number of packets.
            // If update can fit singular packet and there is no splitting, set values to zero.
            packetNeedingHeader[ServerSettings.headerServerTickSplitInfoPosition] = 
                (numberOfExtraPacketsNeeded > 0) ? (byte)i : (byte)0;
            packetNeedingHeader[ServerSettings.headerServerTickSplitInfoPosition+1] = 
                (numberOfExtraPacketsNeeded > 0) ? (byte)numberOfExtraPacketsNeeded : (byte)0;
            //buffer.Add(currentPacketNumber + i, packetNeedingHeader);
            activePacket = activePacket.nextPacket;
        }
        currentPacket.extraPacketsInUpdate = numberOfExtraPacketsNeeded;
        //// Jos oli normi päivitys ni tiedetään mistä jatkaaa seuraavassa rundissa
        if(!createFullSnapshot) currentPacket = activePacket;      
        */
    }
}


