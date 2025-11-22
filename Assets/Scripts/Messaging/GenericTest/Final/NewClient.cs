using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using UnityEngine.SceneManagement;

public class NewClient : MnetNetwork
{
    public bool testConnection;
    public bool testSpawn;
    public NewObject TestPlayer;

    public string clientIP;
    public int clientPort;
    private bool needSnapshotInfo;
    private NewConnection connection;
    private SceneLoadMethod receivedLoadMethod;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        connection = new NewConnection(this, false, -1);
        worldObjectInstances = new NewObject[Mnet.maxSyncedObjects];
        clientControlledInstances = new HashSet<int>();
    }

    private void Start()
    {
        Setup(false);
    }


    /// <summary>
    /// 
    /// 
    ///  CONNECTION
    /// - Try contact server with info
    /// - Get response of new connection
    /// - Switch to new connection
    /// - Get scene number
    /// - Load scene
    /// - Send ready message
    /// - Get object + scene info
    /// - Keep receiving packets until buffer full
    /// - Send ready message and activate player object
    /// 
    /// 
    /// 
    /// </summary>


    private void LateUpdate()
    {
        // ifActive ja muut boolit etc


        // LOOP JOSSA OTETAAN VIESTIT VASTAAN
        // Erotellaan heti aluks REGULAR ja HANDSHAKE prosessit

        // STATES
        // Connected
        // -Active
        // -Initializing
        // Connecting
        // -GetConnectionInfo
        // -GetSceneInfo
        // -GetObjectInfo ? T‰s vaihees ruvetaa jo ker‰‰ pakettei

        // INPUT, EVENTS, TICKS, READING, SENDING


        // TESTI BOOLERIT
        if (testConnection)
        {
            SendHandshake("127.0.0.1", 42069);
            testConnection = false;
        }

        // Fait for new frame when changing scene
        if (connection.state == ConnectionState.Loading && testSpawn)
        {
            testSpawn = false;
            print("SETUP SCENE");
            SetupScene();
        }



        if (gameRunning)
        {

            receiveTimer += Time.unscaledDeltaTime;
            if (receiveTimer > Mnet.receiveRate)
            {
                receiveTimer -= Mnet.receiveRate;

                // SYSTEM MESSAGET


                CheckIncoming(connection);
                /*
                if (connection.isConnected)
                {
                    CheckIncoming(connection);
                }
                else if (connection.isActive)
                {
                    Listen();
                }
                */
            }

            // Onkoha t‰‰ ok vai pit‰iskˆ siistii. Servu ei voi t‰t‰ k‰ytt‰‰ ku on useampi yhteys
            //if (connection.state == ConnectionState.Active)
            // {




                tickTimer += Time.unscaledDeltaTime;
                if (tickTimer > Mnet.tickRate)
                {


                    tickTimer -= Mnet.tickRate;
                    Tick();
                connection.playerPackets.NextWritePacket();
                SendPackets(connection, connection.playerPackets);
                ClientTick(connection);
            }
            //}
        }
        else
        {
            CheckIncoming(connection);
        }



    }


    public void RegularUpdate()
    {

    }

    public void Listen()
    {
        // TImeout ja timereita voidaan tarvita lopulliseen

        if (connection.socket.Available > 0)
        {
            connection.socket.Receive(connection.receivedPacket.AsEmpty());
            connection.receivedPacket.InitializeReceived();
            // Taas packet type riitt‰‰
            if (connection.receivedPacket.PacketType == PacketType.SystemMessage)
            {
                ReadSystemMessage(connection, connection.receivedPacket);
            }
            /*
            else
            {

                switch (connection.state)
                {
                    case ConnectionState.Handshake:
                        {
                            Connect(connection.messager);
                            break;
                        }
                    case ConnectionState.Connecting:
                        {
                            GetSceneInfo(connection.messager);
                            break;
                        }
                    default:
                        {
                            // IDLE
                            break;
                        }
                }
            }
            */
        }
    }

    public void TestSpawn()
    {

    }
    public void SendHandshake(string serverIP, int serverPort)
    {
        connection.state = ConnectionState.Handshake;
        connection.isActive = true;
        connection.SetLocalEndpoint(new IPEndPoint(IPAddress.Parse(clientIP), clientPort));
        //connection.ConnectTo(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
        connection.messager.Clear();
        connection.messager.WriteMessage(Mnet.messageHandshakeRequest);
        connection.local.Address.TryWriteBytes(connection.messager.WriteBytes(4), out _);
        MnetTools.Int32ToBytes(connection.messager.WriteBytes(4), connection.local.Port);
        connection.socket.SendTo(connection.messager.bytes, connection.messager.currentLength, SocketFlags.None, new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
    }

    public void Connect(Packet connectionInfoPacket)
    {
        try
        {
            IPEndPoint newEndpoint = new IPEndPoint(new IPAddress(connectionInfoPacket.ReadBytes(4))
                , MnetTools.BytesToInt32(connectionInfoPacket.ReadBytes(4)));
            connection.ConnectTo(newEndpoint);
            connection.playerNumber = MnetTools.BytesToInt32(connectionInfoPacket.ReadBytes(4));
            int playerIndex = 1 + (connection.playerNumber - 1) * Mnet.objectsPerPlayer;
            for (int i = 0; i < Mnet.objectsPerPlayer; i++)
            {
                clientControlledInstances.Add(playerIndex + i);
            }
            connection.state = ConnectionState.Connecting;
            ConnectionTest();
        }
        catch
        {
            Debug.LogError("CLIENT GOT BAD ENDPOINT DATA");
            // Not valid IP or port
        }

    }

    public void ConnectionTest()
    {
        connection.messager.Clear();
        connection.messager.WriteMessage(Mnet.messageNewConnectionTest);
        connection.socket.Send(connection.messager.Data);
    }

    public void GetSceneInfo(Packet sceneInfoPacket)
    {        
        string sceneName = sceneInfoPacket.ReadString();
        print("SCENE IS " + sceneName);
        receivedLoadMethod = (SceneLoadMethod)sceneInfoPacket.bytes[sceneInfoPacket.readPosition];
        if (sceneName != SceneManager.GetActiveScene().name)
        {
            SceneManager.LoadScene(sceneName);
        }
        connection.state = ConnectionState.Loading;
    }

    public void SetupScene()
    {
        connection.messager.Clear();
        if (receivedLoadMethod == SceneLoadMethod.CallForSnapshot)
        {
            needSnapshotInfo = true;
            connection.messager.SetPacketType(PacketType.SnapshotUpdate);
            // Extra info?!
            Send(connection, connection.messager, PacketPriority.Important);
            connection.state = ConnectionState.SnapshotUpdateRequired;
            Debug.Log("Client sent SNAP request");
            // After snapshot, send ready message and set to paused
        }
        else if (receivedLoadMethod == SceneLoadMethod.GetSceneObjects)
        {
            instanceManager.GetExistingObjectsInScene(spawner);
        }
        else
        {
            Debug.LogError("SCENE LOAD METHOD UNKNOWN");
        }

        if (receivedLoadMethod != SceneLoadMethod.CallForSnapshot)
        {
            connection.messager.SetPacketType(PacketType.SystemMessage);
            connection.messager.WriteMessage(Mnet.messageSceneIsReady);
            Send(connection, connection.messager, PacketPriority.Important);
            connection.state = ConnectionState.Paused;
        }
        connection.isConnected = true;
    }

    public void ReadSnapshot()
    {
        /*
        connection.receivedPacket.InitializeReceived();
        int testSnapEndPacket = MnetTools.BytesToInteger(connection.receivedPacket.ReadBytes(Mnet.headerPacketNumberLength));
        int testWorldCurPack = MnetTools.BytesToInteger(connection.receivedPacket.ReadBytes(Mnet.headerPacketNumberLength));
        int testWorldCurTick = MnetTools.BytesToInteger(connection.receivedPacket.ReadBytes(Mnet.headerTickNumberLength));
        */
        activeManager = snapshotPackets;
        // PREPARAATIO ? Onko t‰‰ nyt varma diili vai pit‰‰kˆ tehd‰ viel jotain?
        // Instance data on tullu ja objekti data eli pit‰‰ ny k‰yd‰ objektit l‰pi ja kutsuu metodi per objekti kai
        // LUE SNAPSHOT
        // Reset process to start
        activeManager.packetToRead = activeManager.GetPacket(1);
        // We get the sent tick number from the packets, so we can detect when we are done reading like with the regular packets
        activeManager.lastTickReceived = activeManager.currentTickNumber - 1;
        activeManager.lastTickProcessed = activeManager.currentTickNumber - 1;

        Debug.Log(name+" reading snapshot with tick "+activeManager.currentTickNumber);
        activeManager.ReadTick();
        // Aja snapshot l‰pi. InstanceManager kai tehty ku se oli DATA -> CHECK * count ni pit‰is olla oikein?
        for (int i = 1; i < worldObjectInstances.Length;i++)
        {
            if (worldObjectInstances[i] != null)  worldObjectInstances[i].UpdateState();
        }
        // OTA REGULAR NUMEROT (TICK ja PACKETNUMBER?)
        //string message = snapshotPackets.packetToRead.ReadMessage();
        //Debug.Log("SNAPSHOT ENDED WITH MESSAGE: " + message);
        // VAIHDA REGULAR MOODIIN
        connection.state = ConnectionState.Paused;
        activeManager = connection.playerPackets;
        // LƒHETƒ READY
        connection.messager.Clear();
        connection.messager.SetPacketType(PacketType.SystemMessage);
        connection.messager.WriteMessage(Mnet.messageSceneIsReady);
        Send(connection, connection.messager, (int)PacketPriority.Important);
    }

    public void ReadSnapshotStartingInfo(Packet infoPacket)
    {
        needSnapshotInfo = false;
        int testSnapTick = MnetTools.BytesToInteger(infoPacket.ReadBytes(Mnet.headerTickNumberLength));
        int testRegPacket = MnetTools.BytesToInteger(infoPacket.ReadBytes(Mnet.headerPacketNumberLength));
        int testRegTick = MnetTools.BytesToInteger(infoPacket.ReadBytes(Mnet.headerTickNumberLength));

        Debug.Log("SNAPINFO Received = Snaptick: " + testSnapTick + ", RegPacket: " + testRegPacket + ", RegTick: " + testRegTick);

        snapshotPackets.currentTickNumber = testSnapTick;
        worldPackets.currentTickNumber = testRegTick;
        worldPackets.incomingPacket = worldPackets.GetPacket(testRegPacket);
        worldPackets.incomingPacket.Clear();
    }

    public void ActivateConnection()
    {

    }

    private void OnDisable()
    {
        connection.socket.Close();
        connection.socket.Dispose();
    }

    public override void Disconnect(NewConnection connection, string message)
    {
        // Try to send disconnect message and enalbe reconnecting later (keep necessary data in memory when loading main menu? Or is server side enough)
        throw new System.NotImplementedException();
    }

    public override void ConnectionTimingOut(NewConnection connection)
    {
        // Stop accepting inputs and check incoming until packets arrive and ask for snap?
        throw new System.NotImplementedException();
    }

    protected override void ReceivedRegularPacket(NewConnection connection)
    {
        worldPackets.InsertPacket(connection.receivedPacket);
        CheckForMissingPackets(connection, worldPackets);
    }

    protected override void ReceivedSnapshotPacket(NewConnection connection)
    {
        if (connection.state == ConnectionState.SnapshotUpdateRequired)
        {
            Debug.Log(name + " received snapshot "+connection.receivedPacket.GetTickNumber());
            snapshotPackets.InsertPacket(connection.receivedPacket);
        }
    }

    // Tarviiko rajottaa? Voiko aiheuttaa ongelmia jos ollaa esim. snapshotti tilassa
    protected override void ReceivedResendRequest(NewConnection connection)
    {
        ResendPacket(connection.receivedPacket, connection, connection.playerPackets);
    }

    protected override void ReceivedResendSnapshotRequest(NewConnection connection)
    {
        // Snapshots can only be done by server, but this method can be used as a base for possible expansion by the user
    }

    protected override void ReadSystemMessage(NewConnection connection, Packet messagePacket)
    {
        string receivedMessage = messagePacket.ReadMessage();
        Debug.Log(name + ": " + receivedMessage);
        if (receivedMessage == Mnet.messageHandshakeResponse && connection.state == ConnectionState.Handshake)
        {
            Connect(messagePacket);
        }
        else if (receivedMessage == Mnet.messageNewConnectionVerified && connection.state == ConnectionState.Connecting)
        {
            GetSceneInfo(messagePacket);
        }
        else if (receivedMessage == Mnet.messageSnapshotInfo && needSnapshotInfo)
        {
            ReadSnapshotStartingInfo(messagePacket);
        }
        else if(receivedMessage == Mnet.messageSnapshotSent && connection.state == ConnectionState.SnapshotUpdateRequired)
        {
            ReadSnapshot();
        }
        else if(receivedMessage == Mnet.messageUnpausePlayer && connection.state == ConnectionState.Paused)
        {
            connection.state = ConnectionState.Active;
            activeManager = connection.playerPackets;
            gameRunning = true;
        }
        else
        {
            Debug.Log("Client ignored message " + receivedMessage+".");
        }
    }

    public override void AddPlayerObject(NewObject playerObject, int playerID)
    {
        Debug.Log("ADD PLAYER KUTSUTTU");
        if(playerID == connection.playerNumber)
        {
            playerObject.transform.SetParent(transform);
            Debug.Log("Player object is " + playerObject.gameObject.activeSelf);
            playerObject.gameObject.SetActive(true);
        }
        worldObjectInstances[playerObject.instanceID] = playerObject;
    }
}
