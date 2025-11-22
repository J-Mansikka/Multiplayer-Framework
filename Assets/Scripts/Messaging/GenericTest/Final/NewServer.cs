using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewServer : MnetNetwork
{
    public bool testScene;
    public bool TestSpawn;
    public bool TestTick;

    public string serverIP;
    public int serverPort;

    private float newConnectionListenTimer = 0f;
    private bool twoFrameTick;
    private Socket incomingConnections;
    private Packet newMessage;
    //private PacketManager snapshotManager;
    private HashSet<string> alreadyJoined;
    private List<NewConnection> activeConnections;
    private Queue<int> availablePlayerNumbers;

    private void Awake()
    {
        Time.timeScale = 0;
        DontDestroyOnLoad(this);
        sceneNumber = SceneManager.GetActiveScene().buildIndex;
        worldObjectInstances = new NewObject[Mnet.maxSyncedObjects];
        activeConnections = new List<NewConnection>();
        alreadyJoined = new HashSet<string>();
        availablePlayerNumbers = new Queue<int>();
        newMessage = new Packet(PacketType.SystemMessage);
        //snapshotManager = new PacketManager(Mnet.serverSnapshotPacketBufferSize, this, PacketType.SnapshotUpdate);
        for (int i = 0; i < Mnet.maxPlayerCount; i++)
        {
            availablePlayerNumbers.Enqueue(i + 1);
        }

        incomingConnections = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        incomingConnections.Blocking = false;
        incomingConnections.Bind(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
    }

    private void Start()
    {
        Setup(true);
    }

    public void ListenForConnectionRequests()
    {

        while (incomingConnections.Available > 0)
        {
            incomingConnections.Receive(newMessage.AsEmpty());
            newMessage.InitializeReceived();
            // Kokoa ei tarkasteta, Eikä edes tyyppiä
            string receivedMessage = newMessage.ReadMessage();
            print("SERVER: " + receivedMessage+" of type "+newMessage.ReadPacketType());
            if (receivedMessage == Mnet.messageHandshakeRequest)
            {
                // !!! If address is not actually an address, shit will crash here
                try
                {
                    IPEndPoint receivedEP = new IPEndPoint
                        (new IPAddress(newMessage.ReadBytes(4))
                        , MnetTools.BytesToInt32(newMessage.ReadBytes(4)));

                    if (!alreadyJoined.Contains(receivedEP.Address.ToString()))
                    {
                        AddNewConnection(receivedEP);
                    }

                    foreach (NewConnection connection in activeConnections)
                    {

                        if (connection.ipAddress == receivedEP.Address.ToString())
                        {
                            SendNewConnectionInfo(connection);
                        }
                    }

                }
                catch
                {
                    Debug.LogError("SERVER GOT BAD ENDPOINT DATA");
                    // Not valid IPaddress or Port
                }
            }
        }

    }

    public void CheckIfReadyToStart(NewConnection newComer)
    {


        // Tsekataa et jos kaikki pausella ni auto start (demo)
        // Jos on jo päällä ni aktivoidaa vaa yhteys

        if (gameRunning)
        {
            newComer.state = ConnectionState.Active;
        }
        else
        {
            bool everyoneIsReady = true;
            foreach (NewConnection connection in activeConnections)
            {
                if (everyoneIsReady && connection.state != ConnectionState.Paused) everyoneIsReady = false;
            }
            if (everyoneIsReady)
            {
                foreach (NewConnection connection in activeConnections)
                {
                    connection.messager.Clear();
                    connection.messager.WriteMessage(Mnet.messageUnpausePlayer);
                    Send(connection, connection.messager, PacketPriority.Important);
                    connection.state = ConnectionState.Active;
                }
                //Debug.Log("SCENE IS "+SceneManager.GetActiveScene().name);
                //instanceManager.GetExistingObjectsInScene(spawner);
                gameRunning = true;
                Time.timeScale = 1;
            }
        }
    }

    private void LateUpdate()
    {
        if (TestSpawn)
        {
            TestSpawn = false;
            foreach(NewConnection connection in activeConnections)
            {
                Debug.Log("TRIED TO SPAWN "+activeManager);
                instanceManager.SpawnPlayerRequest(connection.playerNumber);
            }
            //NewObject chatti = instanceManager.SpawnRequest(10);
            //chatti.transform.SetParent(transform);
        }

        if (testScene)
        {
            testScene = false;
            SceneManager.LoadScene(1, LoadSceneMode.Single);
        }


        // INTERPOLATE
        // - Unity omat animaatiot etc, ei katsota logiikkaa tai mitään raskasta
        // UPDATE
        // - Saattaa olla turha jos Unityn oma Update ei tukehduta paketteja paskalla. MITEN ESTÄÄ? COOLDOWN PER OBJECT EVENT? (OLIS PALJO TIMEREITA JAIKS)
        // - - Tää lukitsee frameraten ja event raten toisiinsa ja pelaajat varmaan tykkäis jos grafiikat päivittyy 100 kertaa sekunnis nykyään :L
        // - Tapahtuu useimmiten, triggerit etc, tsekataan KIRJOTETAAN MUUTOKSET + METODI
        // - ! Unityn omat metodit paukkuu millo sattuu eli tätä ei ehkä edes ole? Antaa mennä kuten yksinpelissä mutta pitää rajottaa?
        // - OnCollisionEnter -> store collision -> update check -> Write
        // - - Voitais missata collisioineita updatien välissä? tai jos pitää array paskaa tehdä ni menee monimutkaseks. Olis bueno hoitaa vaa vanilja tyylii
        // RECEIVE
        // - Tapahtuu myös usein, serveri myös kuuntelee uusia yhteyksiä (ehkä jos full ni skippaa)
        // TICK
        // - Luetaan paketit (S: Local + Received) (C: Received (inventaariot sun muut lähtee mutta lopputulos päättää serveri))
        // - Ajetaan muutokset ja eventit
        //
        // Muuten yhteinen mutta client ei päivitä vaan ottaa omat inputit ja ajaa ne
        //

        /*
        updateTimer += Time.deltaTime;
        if(updateTimer > Mnet.updateRate)
        {
            updateTimer -= Mnet.updateRate;
            ServerUpdate();
        }

        */


        if (gameRunning)
        {
            receiveTimer += Time.unscaledDeltaTime;
            if (receiveTimer > Mnet.receiveRate)
            {
                receiveTimer -= Mnet.receiveRate;

                foreach (NewConnection connection in activeConnections)
                {
                    if (connection.socket.Available > 0) CheckIncoming(connection);
                }
            }

            tickTimer += Time.unscaledDeltaTime;
            // !!! TEST
            if (tickTimer > Mnet.tickRate)
            {
                tickTimer -= Mnet.tickRate;
                // Close final packet
                int sizeOfPacket = worldPackets.packetToWriteOn.currentLength;
                if (sizeOfPacket <= 0)
                {
                    Debug.Log("SERVER TICK CLOSE PACKET AT SIZE " + worldPackets.packetToWriteOn.currentLength);
                    Debug.Log("INDEX: " + worldPackets.packetToWriteOn.debugIndex);
                }
                worldPackets.NextWritePacket();

                foreach (NewConnection connection in activeConnections)
                {
                    ClientTick(connection);
                }
                foreach (NewConnection connection in activeConnections)
                {
                    SendPackets(connection, worldPackets);
                }
                Tick();
            }
        }
        else
        {
            foreach (NewConnection connection in activeConnections)
            {
                if (connection.socket.Available > 0) CheckIncoming(connection);
            }
        }

        newConnectionListenTimer += Time.unscaledDeltaTime;
        if (newConnectionListenTimer > Mnet.receiveRate)
        {
            newConnectionListenTimer -= Mnet.receiveRate;
            ListenForConnectionRequests();
        }
    }

    public void ServerUpdate()
    {
        // Check everything... objekteil oma metodi? mites tää menee? Ehkä vois vaan käyttää Update() ja
        // varmistaa ettei kirjoteta sata kertaa yksinkertaisii trigger tapahtumii etc (mieti miten yksinpeli toimis)
    }

    public void CreateSnapshot(NewConnection connection)
    {
        SendSnapshotInfoPacket(connection);

        activeManager = snapshotPackets;

        // Eka info? Eli skippaa ja kirjota vaikka ykkösestä. EI ESTÄ ETTÄ KÄYTTÄÄ TICKIN!
        // Tarvittava info: Regular managerin oikea paketti (eli seuraava joka lähtee ja serverin aktiivinen tick)
        // INFO, DATA, READY MESSAGE (eli ei tarvita tickiä lainkaan?
       

        // NYKYNEN LOGIIKKA: Kirjota snapshot ja lähetä message jossa on viiminen paketti numero snapissa (eli lukee kunnes tulee vastaan) ja regular info
        // Mut message tulee LUKEMISEN JÄLKEEN NI EI VOI TOIMIA

        // InstantManager hoitaa spawnit JA data asetukset. Ja muuta ei tarviikkaa lähettää
        instanceManager.CreateSnapshot(snapshotPackets);
        SendPackets(connection, snapshotPackets);
        connection.messager.Clear();
        connection.messager.WriteMessage(Mnet.messageSnapshotSent);
        Send(connection, connection.messager, PacketPriority.Important);
        // Since tick numbers are used to detect update length, we need to make sure that every received snapshot has a different tick number
        snapshotPackets.currentTickNumber++;
        activeManager = worldPackets;
    }

    // Connection luotu mutta ei välttämättä testattu eli joko checkIncoming tai sitte vielä yrittää yhdistää tai ehkä keskeyttää etc
  /*
    public void ListenToConnection(NewConnection connection)
    {
        //if (connection.socket.Available > 0)  CheckIncoming(connection);
            /*
            if (connection.isConnected)
            {
                // REG PACKETS
                CheckIncoming(connection);
            }

            else
            {
                connection.messager.Clear();
                connection.socket.Receive(connection.messager.AsEmpty());
                connection.messager.InitializeReceived();

                // Prosessi kesken? Näyttää et on vaa tää metodi et isConnected on melko varmasti true?
                // Ei ku voi olla joo connectionTest -> SceneInfo
                if (connection.messager.PacketType == PacketType.SystemMessage)
                {
                    ReadSystemMessage(connection, connection.messager);
                }

                // !!! SIIRRÄ TÄÄ METODIIN
                else if(connection.messager.PacketType == PacketType.Connecting)
                {
                    string message = connection.messager.ReadMessage();
                    print("CONNECTION: " + message);
                    if (message == Mnet.messageNewConnectionTest)
                    {


                    }
                }
            }
    }
  */
    public void AddNewConnection(IPEndPoint playerEP)
    {
        if (availablePlayerNumbers.Count > 0)
        {
            int playerNumber = availablePlayerNumbers.Dequeue();
            Debug.Log("PLAYER NUMBER ANNETTUNA ON " + playerNumber);
            NewConnection newPlayerConnection = new NewConnection(this, true, playerNumber);
            newPlayerConnection.SetLocalEndpoint(new IPEndPoint(IPAddress.Parse(serverIP), serverPort + playerNumber));
            newPlayerConnection.ConnectTo(playerEP);
            newPlayerConnection.state = ConnectionState.Connecting;
            newPlayerConnection.ipAddress = playerEP.Address.ToString();
            newPlayerConnection.playerNumber = playerNumber;
            alreadyJoined.Add(newPlayerConnection.ipAddress);
            activeConnections.Add(newPlayerConnection);
        }

    }

    public void SendNewConnectionInfo(NewConnection playerConnection)
    {
        // Luo viesti missä message + uuden connectionin infot
        // Message + ip + port + player number.. scene?
        newMessage.Clear();
        newMessage.WriteMessage(Mnet.messageHandshakeResponse);
        playerConnection.local.Address.TryWriteBytes(newMessage.WriteBytes(4), out _);
        MnetTools.Int32ToBytes(newMessage.WriteBytes(4), playerConnection.local.Port);
        MnetTools.Int32ToBytes(newMessage.WriteBytes(4), playerConnection.playerNumber);
        playerConnection.socket.SendTo(newMessage.bytes, newMessage.currentLength, SocketFlags.None, playerConnection.remote);
    }

    public void SendSceneInfo(NewConnection connection, SceneLoadMethod method)
    {
        connection.isConnected = true;
        connection.state = ConnectionState.Loading;

        connection.messager.Clear();
        connection.messager.WriteMessage(Mnet.messageNewConnectionVerified);
        //string activeSceneName = SceneManager.GetActiveScene().name;
        string activeSceneName = "KakkosScene";
        connection.messager.WriteString(activeSceneName);
        connection.messager.WriteSingleByte((byte)method);
        connection.socket.Send(connection.messager.Data);
        // SCENE + INFO (Scene number + bool firstLoad (voi ladata suoraa scene objektit sellasenaa)
        // Muut pelaajat etc
    }

    public void SendSnapshotInfoPacket(NewConnection connection)
    {
        connection.messager.Clear();
        connection.messager.WriteMessage(Mnet.messageSnapshotInfo);
        MnetTools.IntegerToBytes(connection.messager.WriteBytes(Mnet.headerTickNumberLength), snapshotPackets.currentTickNumber);
        MnetTools.IntegerToBytes(connection.messager.WriteBytes(Mnet.headerPacketNumberLength), worldPackets.currentPacketNumber);
        MnetTools.IntegerToBytes(connection.messager.WriteBytes(Mnet.headerTickNumberLength), worldPackets.currentTickNumber);
        Debug.Log("SNAPINFO Sent = Snaptick: " + snapshotPackets.currentTickNumber + 
            ", RegPacket: " + worldPackets.currentPacketNumber + ", RegTick: " + worldPackets.currentTickNumber);
        Send(connection, connection.messager, PacketPriority.Important);
    }

    private void OnDisable()
    {
        incomingConnections.Close();
        incomingConnections.Dispose();
    }

    public override void Disconnect(NewConnection connection, string message)
    {
        // Keep rejoin window open for a while before freeing slot or free slot immediately
        throw new System.NotImplementedException();
    }

    public override void ConnectionTimingOut(NewConnection connection)
    {
        // Pause and protect player character and ignore regular packets until snapshot request comes in and player can resume
        throw new System.NotImplementedException();
    }

    protected override void ReceivedRegularPacket(NewConnection connection)
    {
        Debug.Log("REGREC server");
        connection.playerPackets.InsertPacket(connection.receivedPacket);
        CheckForMissingPackets(connection, connection.playerPackets);
    }

    protected override void ReceivedSnapshotPacket(NewConnection connection)
    {
        if (connection.state != ConnectionState.SnapshotUpdateRequired)
        {
            print("SERVER: Client was marked as needing snapshot");
            connection.state = ConnectionState.SnapshotUpdateRequired;

            CreateSnapshot(connection);
        }
    }

    protected override void ReceivedResendRequest(NewConnection connection)
    {
        ResendPacket(connection.receivedPacket, connection, worldPackets);
    }

    protected override void ReceivedResendSnapshotRequest(NewConnection connection)
    {
        ResendPacket(connection.receivedPacket, connection, snapshotPackets);
    }

    protected override void ReadSystemMessage(NewConnection connection, Packet messagePacket)
    {
        string receivedMessage = messagePacket.ReadMessage();

        Debug.Log(name + ": " + receivedMessage);
        if (receivedMessage == Mnet.messageNewConnectionTest && connection.state == ConnectionState.Connecting)
        {
            SceneLoadMethod loadMethod = SceneLoadMethod.CallForSnapshot;
            SendSceneInfo(connection, loadMethod);
        }
        else if (receivedMessage == Mnet.messageSceneIsReady 
            &&  (connection.state == ConnectionState.Loading  || connection.state == ConnectionState.SnapshotUpdateRequired))
        {
            connection.state = ConnectionState.Paused;
            connection.isConnected = true;
            CheckIfReadyToStart(connection);
        }
        else if (receivedMessage == Mnet.messageNeedSnapshot && connection.state != ConnectionState.SnapshotUpdateRequired)
        {
            connection.state = ConnectionState.SnapshotUpdateRequired;
            CreateSnapshot(connection);
        }
        else
        {
            Debug.Log(name+" ignored message " + receivedMessage + ".");
        }
    }

    public override void AddPlayerObject(NewObject playerObject, int playerID)
    {
        /*
        foreach(NewConnection connection in activeConnections)
        {
            if(connection.playerNumber == playerID)
            {
                connection.playerObjects[playerObject.objectID] = playerObject;
            }
        }
        */
        worldObjectInstances[playerObject.instanceID] = playerObject;
        playerObject.gameObject.SetActive(true);
    }
}
