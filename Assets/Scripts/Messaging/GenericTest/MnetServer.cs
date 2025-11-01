using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class MnetServer : MnetEndpoint
{
    public string addressString = "192.168.1.2";
    public int handshakerPort = 28500;
    private Socket handshaker;
    private bool acceptNewPlayers;
    private HashSet<MnetConnection> newConnectionsToInitialize;
    private Queue<int> playerNumbersAvailable;  // 0 - max
    private Queue<int> freeObjectInstanceIDs;

    private void Awake()
    {
        isServer = true;

        worldBuffer = new MnetPacketBuffer(Mnet.serverPacketBufferSize);
        worldObjectIDs = new HashSet<int>();
        activeConnections = new List<MnetConnection>();
        messenger = new MnetPacket(false);
        worldObjects = new WANHAMnetObject[Mnet.maxSyncedObjects];
        //          manager = GetComponent<MnetInstanceManager>();
        //          manager.SetupInstanceManager(worldObjects, Ownership.Local);
    }

    private void Start()
    {

    }

    public void StartServer()
    {
        //manager.TryGetSceneObjects(worldObjects, true);
        handshaker = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        handshaker.Bind(new IPEndPoint(IPAddress.Parse(addressString), handshakerPort));
        handshaker.Blocking = false;
        newConnectionsToInitialize = new HashSet<MnetConnection>();
        playerNumbersAvailable = new Queue<int>();
        for(int i = 0; i < Mnet.maxPlayerCount; i++)
        {
            playerNumbersAvailable.Enqueue(i);
        }
        print("SERVER START");
        online = true;
        acceptNewPlayers = true;
    }

    public void StartGame()
    {
        gameIsRunning = true;
    }
    // Handshake rutiini servu puolel. jokanen connection vaatii oman EP eli luodaa address port + 1 vaikka? Pitä yllä numbaa intillä? ehkä int = portti JA index

    private void LateUpdate()
    {
        if (online)
        {
            if (acceptNewPlayers)
            {
                if (handshaker.Available > 0)
                {
                    messenger.Reset();
                    handshaker.Receive(messenger.AllBytes());
                    //string receivedMessage = System.Text.Encoding.ASCII.GetString(messenger.Span(0,16));
                    string receivedMessage = messenger.ReadMessage();
                    print("HANDSHAKER: Message Received = " + receivedMessage);
                    //int readPos = 16;

                    if (receivedMessage == Mnet.messageHandshakeRequest)
                    {
                        HandshakeResponse();

                        /*
                        if (playerNumbersAvailable.Count == 0)
                        {
                            acceptNewPlayers = false;
                            // !!! Jos socket jää auki ni bufferi varmaan tulee täyteen, eli jos aletaan taas kuuntelee ni pitää varmaan siivota eka
                        }
                        */
                    }
                }
            }

            if (newConnectionsToInitialize.Count > 0)
            {
                foreach (MnetConnection connection in newConnectionsToInitialize)
                {
                    if (connection.socket.Available > 0)
                    {
                        messenger.Reset();
                        connection.socket.Receive(messenger.AllBytes());
                        //string receivedMessage = System.Text.Encoding.ASCII.GetString(messenger.Span(0,16));
                        string receivedMessage = messenger.ReadMessage();
                        print("NEW CONNECTION: Message Received = " + receivedMessage);

                        if (receivedMessage == Mnet.messageNewConnectionTest && !activeConnections.Contains(connection))
                        {
                            VerifyConnection(connection);
                            // !!! LUO SNAPSHOT JA LÄHETÄ ELI TÄÄ CONNECTION PITÄIS OLLA LISTENISSÄ JO HETI TÄSSÄ KOHTAA
                        }
                        else if (receivedMessage == Mnet.messageReadyToStart)
                        {
                            CreatePlayerObjects(connection);
                            connection.isActive = true;
                            newConnectionsToInitialize.Remove(connection);
                        }
                    }
                }
            }

            if (gameIsRunning)
            {
                NetworkUpdate();
            }
        }

        // LUO PELAAJA JA ODOTA READY VIESTIÄ
    }




    public void HandshakeResponse()
    {
        print("SERVER: HANDSHAKE RESPONSE");
        // CREATE NEW CONNECTION AND SEND PACKET ADDRESS + PORT + INFO(myöhemmin)
        // CONNECTION LAATU TESTI MUT EHKÄ VOI LISÄTÄ MYÖHEMMIN (ELI EI KOSKAAN?)
        // Talleta ja tsekkaa IP osote

        IPAddress newAddress = new IPAddress(messenger.Read(Mnet.int32));
        //readPos += 4;
        int newPort = MnetTools.BytesToInteger(messenger.Read(Mnet.int32));//, 4);

        int incomingID = BitConverter.ToInt32(newAddress.GetAddressBytes());

        bool createNew = true;

        // Check if the connection already exists so we can ignore duplicate requests and separate reconnects from new connections
        foreach (MnetConnection existingConnection in activeConnections)
        {
            if (incomingID == existingConnection.connectionID)
            {
                if (existingConnection.state == WANHAAConnectionState.Disconnecting)
                {
                    // !!! ELI YHDISTETÄÄ UUDELLEEN. TARVITAA VARMAAN UUSI sTATE (dropout, reconnectAvailable) tai ehkä disconnect käy?
                    // Ehkä vois olla et iha sama miks disconnecti, antaa mahdollisuuden joinata takas joku muutama minuuti. 
                    // Ehkä lisätä bool banned tai jotai jos ei haluta päästää samaan sessioon takasin

                }
                else
                {
                    createNew = false;
                    break;
                }
            }

        }

        foreach (MnetConnection existingConnection in newConnectionsToInitialize)
        {
            if (incomingID == existingConnection.connectionID)
            {
                if (existingConnection.state == WANHAAConnectionState.Disconnecting)
                {
                    // !!! ELI YHDISTETÄÄ UUDELLEEN. TARVITAA VARMAAN UUSI sTATE (dropout, reconnectAvailable) tai ehkä disconnect käy?
                    // Ehkä vois olla et iha sama miks disconnecti, antaa mahdollisuuden joinata takas joku muutama minuuti. 
                    // Ehkä lisätä bool banned tai jotai jos ei haluta päästää samaan sessioon takasin

                }
                else
                {
                    createNew = false;
                    break;
                }
            }

        }

        if (createNew)
        {
            int playerNumber = playerNumbersAvailable.Dequeue();
            MnetConnection newConnection =
                new MnetConnection(playerNumber,
                new IPEndPoint(IPAddress.Parse(addressString), handshakerPort + 1 + playerNumber),
                new NewClient(),
                Mnet.clientPacketBufferSize,
                1);

            newConnection.ConnectTo(new IPEndPoint(newAddress, newPort));

            // Lähetä takas info uudesta yhteydestä
            messenger.Reset();
            //int writePos = 0;
            //System.Text.Encoding.ASCII.GetBytes(Mnet.messageHandshakeResponse, messenger.Span(writePos, 16));
            //writePos += 16;
            messenger.WriteMessage(Mnet.messageHandshakeResponse);
            //byte[] address = newConnection.localEP.Address.GetAddressBytes();
            newConnection.localEP.Address.TryWriteBytes(messenger.Write(4), out _);
            /*
            for (int i = 0; i < 4; i++)
            {
                messenger[i + writePos] = address[i];
            }
            */
            //writePos += 4;
            MnetTools.Int32ToBytes(messenger.Write(Mnet.int32), newConnection.localEP.Port);
            //MnetTools.Int32ToBytes(messenger.Span(writePos), newConnection.localEP.Port);
            //writePos += 4;
            //MnetTools.Int32ToBytes(messenger.Span(writePos), newConnection.playerNumber);
            MnetTools.Int32ToBytes(messenger.Write(Mnet.int32),newConnection.playerNumber);
            newConnection.state = WANHAAConnectionState.Connecting;

            int sendDuplicates = newConnection.sendRate * (int)PacketPriority.Important;
            print("RESPONDING TO:");
            print(newConnection.remoteEP.Address);
            print(newConnection.remoteEP.Port);
            print("WITH:");
            print(newConnection.localEP.Address);
            print(newConnection.localEP.Port);
            for (int i = 0; i < sendDuplicates; i++)
            {
                handshaker.SendTo(messenger.Data, newConnection.remoteEP);
            }

            newConnectionsToInitialize.Add(newConnection);

            // !!! mikä check tää on? Yleensä kaikki kirjotusket ainaki handshakingis alkaa resetil?
            messenger.Reset();
        }
    }

    public void VerifyConnection(MnetConnection newConnection)
    {
        // Lähetä verify viesti + packet + tick numbs
        // !!! Miks paketti numero puuttuu? Taitaa tulla perässä ku otetaa snappi
        messenger.Reset();
        //int writePos = 0;
        //System.Text.Encoding.ASCII.GetBytes(Mnet.messageNewConnectionVerified, messenger.Span(writePos, 16));
        messenger.WriteMessage(Mnet.messageNewConnectionVerified);
        //writePos += 16;
        //messenger.SetTickNumber(currentTickNumber);
        //MnetTools.Int32ToBytes(messenger.Span(writePos), currentTickNumber);
        MnetTools.Int32ToBytes(messenger.Write(Mnet.int32), currentTickNumber);
        //writePos += Mnet.headerTickNumberLength;

        // Add active scene number to message
        //MnetTools.IntegerToBytes(messenger.Span(writePos), activeSceneIndex,Mnet.headerTickNumberLength);
        MnetTools.IntegerToBytes(messenger.Write(Mnet.headerTickNumberLength),activeSceneIndex);//,Mnet.headerTickNumberLength);
        //writePos += Mnet.headerTickNumberLength;
        //messenger.currentLength = writePos;
        /*
        writePos += 2;
        MnetTools.Int32ToBytes(messenger.Span(writePos), currentPacketNumber);
        writePos += 4;
        MnetTools.Int32ToBytes(messenger.Span(writePos), currentTickNumber);
        */

        Send(messenger, newConnection, PacketPriority.Important);
        newConnection.state = WANHAAConnectionState.Initializing;

        // !!! Ehkä pidetää newconnection listassa kunnes ready ni voidaa huomioida isReady viesti ja käynnistää
        //newConnectionsToInitialize.Remove(newConnection);

        // !!! SNAP TARVITAAN

        activeConnections.Add(newConnection);
        print("SERVER: SWITCHING TO CONNECTION");
    }

    private void OnDisable()
    {
        if(handshaker != null)  handshaker.Close();
    }

    public override void Disconnect(MnetConnection disconnecting, DisconnectCause cause)
    {

    }

    public override void LocalTick()
    {
        throw new System.NotImplementedException();
    }
}
