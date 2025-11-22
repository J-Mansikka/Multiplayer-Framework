using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using UnityEngine.SceneManagement;

public class MnetClient : MnetEndpoint
{
    public string localAddress = "192.168.1.2";
    public int port = 28501;
    public string serverAddress = "192.168.1.2";
    public int serverPort = 28500;
    //private MnetConnection clientConnection;
    public bool sendTest;

    private void Awake()
    {
        //          manager = GetComponent<MnetInstanceManager>();
        worldBuffer = new MnetPacketBuffer(Mnet.clientPacketBufferSize);
        IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(localAddress), port);
        //clientConnection = new MnetConnection(-1, localEP, Mnet.serverPacketBufferSize);
        messenger = new MnetPacket(false);
        worldObjectIDs = new HashSet<int>();
        activeConnections = new List<MnetConnection>();
        messenger = new MnetPacket(false);
        worldObjects = new WANHAMnetObject[Mnet.maxSyncedObjects];
        //          manager.SetupInstanceManager(worldObjects, Ownership.Remote);

        // !!! DEBUG
        //connection.remoteObjects = new MnetObject[10];
        //connection.remoteObjects[1] = worldObjects[1];
        //worldObjects[1] = null;
    }

    public void StartClient()
    {
        print("CLIENT: STARTING");
        //connection.ChangeConnection(new IPEndPoint(IPAddress.Parse("192.168.1.2"), 28500));
        online = true;
        HandshakeRequest();
    }

    private void Update()
    {
        /*
        if(sendTest)
        {
            sendTest = false;
            System.Text.Encoding.Unicode.GetBytes("Hello world!", messenger.AllBytes());
            connection.socket.SendTo(messenger.Data, connection.remoteEP);
        }
        */
    }


    private void LateUpdate()
    {

        /*
         Aika paljoo samaa rakennetta -> kuuntele -> lue viesti -> tee jotain -> siirry askel eteenpäin          
         */
        // CONNECTED
        if (online)
        {
            if (gameIsRunning && clientConnection.state >= WANHAAConnectionState.Desynced)
            {
                NetworkUpdate();
            }
            // CONNECTING
            else
            {
                // Siirrä nää omaan metodiin? Vois vähä selkeytttää ku on metodi mil on nimi eikä sotkis updatee ku toi perus on noin pieni

                if (clientConnection.socket.Available > 0)
                {

                    //int readPos = 0;
                    messenger.Reset();
                    clientConnection.socket.Receive(messenger.AllBytes());
                    //string receivedMessage = System.Text.Encoding.ASCII.GetString(messenger.Span(0, 16));
                    //readPos += 16;
                    //string receivedMessage = System.Text.Encoding.ASCII.GetString(messenger.Read(Mnet.messageLength));
                    string receivedMessage = messenger.ReadMessage();
                    print("CLIENT: Message Received = " + receivedMessage);
                    print("CLIENT STATE: " + clientConnection.state);
                    if (clientConnection.state == WANHAAConnectionState.HandshakeRequest && receivedMessage == Mnet.messageHandshakeResponse)
                    {
                        //CreateConnection(readPos);
                        CreateConnection();
                    }
                    else if (clientConnection.state == WANHAAConnectionState.Connecting && receivedMessage == Mnet.messageNewConnectionVerified)
                    {
                        //BeginInitialization(readPos);
                        BeginInitialization();
                    }
                    else if (clientConnection.state == WANHAAConnectionState.Initializing)
                    {
                        InitializingClientGame();
                        // Tsekkaa puuttuva snapshot paketti JA tick buffer? (processestickseparation)
                        // START GAME AFTER SETTING EVERYTHING UP AND USING SNAPSHOT
                        // SEND READY AND SET CONNECTED
                    }
                }
            }
        }
    }

    public void HandshakeRequest()
    {
        messenger.Reset();
        // Message
        //System.Text.Encoding.ASCII.GetBytes(Mnet.messageHandshakeRequest, messenger.Write(Mnet.messageLength));
        messenger.WriteMessage(Mnet.messageHandshakeRequest);
        //messenger.currentLength += 16;
        // Address
        int addressLength;
        clientConnection.localEP.Address.TryWriteBytes(messenger.Write(Mnet.ipAddressLength), out addressLength);
        //messenger.currentLength += addressLength;
        // Port
        MnetTools.Int32ToBytes(messenger.Write(Mnet.portLength), port);
        //messenger.currentLength += 4;

        /*
        CreateNewConnectionMessage(MnetSettings.messageHandshakeRequest, messenger, new int[] {
            localEP.Port
        }, localEP.Address.GetAddressBytes());
       */

        IPEndPoint handshaker = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);

        //Send(messenger, connection, PacketPriority.Important);
        for (int i = 0; i < (int)PacketPriority.Important; i++)
        {
            clientConnection.socket.SendTo(messenger.Data, handshaker);
        }
        clientConnection.state = WANHAAConnectionState.HandshakeRequest;
        
        print("CLIENT: SEND HANDSHAKE REQUEST");
    }

    public void CreateConnection()//int readPos)
    {
        // Get the information for the new connection, send by the server
        //IPAddress newAddress = new IPAddress(messenger.Span(readPos, 4));
        IPAddress newAddress = new IPAddress(messenger.Read(Mnet.int32));
        //readPos += 4;
        //int newPort = MnetTools.BytesToInteger(messenger.Span(readPos, 4), 4);
        int newPort = MnetTools.BytesToInteger(messenger.Read(Mnet.int32));//, Mnet.int32);
        //readPos += 4;
        //clientConnection.playerNumber = MnetTools.BytesToInteger(messenger.Span(readPos, 4), 4);
        clientConnection.playerNumber = MnetTools.BytesToInteger(messenger.Read(Mnet.int32));//, 4);

        // Bind the socket to the endpoint of the new connection
        clientConnection.ConnectTo(new IPEndPoint(newAddress, newPort));
        clientConnection.state = WANHAAConnectionState.Connecting;

        // Send a message to test the new connection
        messenger.Reset();
        //System.Text.Encoding.ASCII.GetBytes(Mnet.messageNewConnectionTest, messenger.Write(Mnet.messageLength));
        messenger.WriteMessage(Mnet.messageNewConnectionTest);
        //messenger.currentLength = 16;
        Send(messenger, clientConnection, PacketPriority.Important);

        print("CLIENT: CREATING CONNECTION TO "+clientConnection.remoteEP.Address+":"+clientConnection.remoteEP.Port);
    }

    public void BeginInitialization()//int readPos)
    {

        // !!! Connection verified viestin perään voidaan lyödä pakettinumero ja tick et voidaan alottaa oikee. Nää pitää sitte olla TULEVAT eikä nykyset

        int latestServerSendTick =
            MnetTools.BytesToInteger(messenger.Read(Mnet.headerPacketNumberLength));//, Mnet.headerPacketNumberLength);
        //readPos += Mnet.headerTickNumberLength;
        int activeSceneOnServer = MnetTools.BytesToInt32(messenger.Read(Mnet.int32));

        if(activeSceneOnServer != activeSceneIndex)
        {
            ChangeScene(activeSceneOnServer);
        }
        // Täs vaiheessa vois jo varmaa vaihtaa normi listenii kattoo vaa et kaikki tarvittava on kunnos (tick ainaki tarvitaa, paketti numero on kai 0)
        // GET SNAP TÄSSÄ VAIHEESSA?
        // Voiko kopioida Listenistä snap osion? Tarvitaan muuten tick numero et tiedetää mikä snap haetaan

        // Client now needs to send snapshot request to get the ball rolling

        // !!! Snapin pitäis yliajaa muutenki kaikki ni se pitäis hypätä tän nykyhetkeen sitte ku se saapuu

        // Connection is verified, so we will request for the snapshot
        messenger.Reset();
        messenger.Message = MessageType.SnapshotRequest;
        messenger.SetTickNumber(latestServerSendTick);
        clientConnection.firstPacketInSnapshot = -1;

        CreatePlayerObjects(clientConnection);

        Send(messenger, clientConnection, PacketPriority.Important);
        clientConnection.state = WANHAAConnectionState.Initializing;
        print("CLIENT: INITIALIZING CONNECTION");
    }

    public void InitializingClientGame()
    {
        print("CLIENT INIT LISTENING, SNAPSHOT COUNT = "+ clientConnection.snapshotPacketsLeft.Count+", BUFFER = "+ clientConnection.playerBuffer.writeReadDistanceInTicks);
        ListenForIncoming(clientConnection, clientConnection.playerBuffer);

        // When the client receives that snapshot and enough ticks to fill write/read cushion, we can start the game on the client side
        if (clientConnection.snapshotPacketsLeft.Count == 0 && clientConnection.playerBuffer.writeReadDistanceInTicks >= Mnet.bufferBetweenWrittenAndProcessedTicks)
        {
            // Send the ready message to the server as a last step
            messenger.Reset();
            //System.Text.Encoding.ASCII.GetBytes(Mnet.messageReadyToStart, messenger.Write(Mnet.messageLength));
            messenger.WriteMessage(Mnet.messageSceneIsReady);
            Send(messenger, clientConnection, PacketPriority.Important);
            
            // Client has connected and we are ready to simulate ticks
            activeConnections.Add(clientConnection);
            clientConnection.state = WANHAAConnectionState.Connected;
            clientConnection.isActive = true;
        }
    }



    private void OnDisable()
    {
    }

    public override void Disconnect(MnetConnection disconnecting, DisconnectCause cause)
    {

    }
    public override void LocalTick()
    {
        print("CLIENT TICK");
    }
}
