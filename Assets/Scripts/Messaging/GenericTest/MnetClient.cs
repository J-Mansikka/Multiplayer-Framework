using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using System.Buffers.Binary;
using System.Text;
using Unity.VisualScripting;

public class MnetClient : MonoBehaviour
{
    [Tooltip("Object Handler which handles spawn/despawn messaging")]
    public MnetObjectStateHandler objectHandler;
    [Tooltip("Client ip address as a string (E.g. 127.0.0.1)")]
    public string clientIPaddress;
    [Tooltip("Port used to receive messages")]
    public int clientPort;
    [Tooltip("Server ip address as a string (E.g. 127.0.0.1)")]
    public string serverAddress;
    [Tooltip("Port used to send messages")]
    public int serverPort;
    private Socket socket;


    private MnetPacketBuffer playerBuffer;
    private MnetPacketBuffer worldStateBuffer;

    public List<MnetObject> objectsBeingSynced;

    private int nextExpectedPacketNumber;   // Saapuva (tyhjä slotti)
    private int nextPacketNumberToProcess;  // Seuraava luettava paketti numero
    private int lastTickSize;
    private int currentFrameNumber;
    private float currentFrameTime;
    private MnetPacket currentPacket;

    private MnetPacket connectionEstablisherPacket;
    private ConnectionState connectionState;
    private IPEndPoint localEP;
    private IPEndPoint handlerEP;
    //private float sendPlayerPacket;

    private HashSet<int> MissingPacketNumbers;

    private float clientTimer;
    private float tickTimer;
    private float timeoutTimer;
    private int missedPackets;

    private int writeHead;


    private void Awake()
    {
        objectsBeingSynced = new List<MnetObject>(ServerSettings.maxSyncedObjects);
        //objectsBeingSynced = new MnetObject[ServerSettings.maxSyncedObjects];
        playerBuffer = new MnetPacketBuffer(ServerSettings.clientPacketBufferSize, false);
        //// Mihin tallettaa world state bufferin koko? Vois olla sama ku max update size ja lisät settinkeihi
        worldStateBuffer = new MnetPacketBuffer(ServerSettings.worldStatePacketBufferSize);
        connectionEstablisherPacket = new MnetPacket(false);
        objectHandler.HandlerSetup(objectsBeingSynced);
        MissingPacketNumbers = new HashSet<int>();

        clientTimer = 0f;
        tickTimer = 0f;
        timeoutTimer = 0f;
        nextExpectedPacketNumber = 0;
        writeHead = 0;
    }

    private void Start()
    {

        ActivateSocket();
        //StartServer();

        currentFrameTime = Time.realtimeSinceStartup;
        //ServerTest();

    }

    public void ActivateSocket()
    {
        socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        localEP = new IPEndPoint(IPAddress.Parse(clientIPaddress), clientPort);
        socket.Bind(localEP);
        socket.Blocking = false;
        socket.Connect(IPAddress.Parse(serverAddress), serverPort);
    }

    public void HandshakeWithHandler()
    {
        if (socket.Available > 0)
        {
            timeoutTimer = 0f;
            socket.Receive(connectionEstablisherPacket.Span());
            string messageReceived = Encoding.ASCII.GetString(connectionEstablisherPacket.Span(0, 16));
            if (messageReceived == ServerSettings.messageHandlerHandshakeResponse)
            {
                Encoding.ASCII.GetBytes(ServerSettings.messageClientReadyToStart.AsSpan(), connectionEstablisherPacket.Span(0, 16));
                for (int i = 0; i < ServerSettings.redundantCopiesHandshake; i++)
                {
                    socket.Send(connectionEstablisherPacket.Span());
                }
                connectionState = ConnectionState.SyncWorldState;
            }
        }
        else
        {
            if (!ConnectionTimeoutCheck())
            {
                Encoding.ASCII.GetBytes(ServerSettings.messageClientHandshake.AsSpan(), connectionEstablisherPacket.Span(0, 16));
                localEP.Address.TryWriteBytes(connectionEstablisherPacket.Span(16, 4), out _);
                BinaryPrimitives.WriteInt32LittleEndian(connectionEstablisherPacket.Span(20, 4), localEP.Port);
                socket.Send(connectionEstablisherPacket.Span());
            }
        }
    }

    private void RequestConnection()
    {
        /// Eka tsekkaa onko jo serveri vastannu ja jos nii ni päivitetää connection ja break out
        if (socket.Available > 0)
        {
            timeoutTimer = 0f;
            socket.Receive(connectionEstablisherPacket.Span());
            /// Jos ei oo tullu vastausta ni lähetetään serveriin viesti
            string messageReceived = Encoding.ASCII.GetString(connectionEstablisherPacket.Span(0, 16));
            if (messageReceived == ServerSettings.messageServerNewConnectionResponse)
            {
                IPAddress handlerIP = new IPAddress(connectionEstablisherPacket.Span(16, 4));
                int newPort = BinaryPrimitives.ReadInt32LittleEndian(connectionEstablisherPacket.Span(20, 4));

                IPEndPoint handlerEP = new IPEndPoint(handlerIP, newPort);
                socket.Connect(handlerEP);
                connectionState = ConnectionState.Handshake;
            }
        }
        else
        {
            if (!ConnectionTimeoutCheck())
            {
                Encoding.ASCII.GetBytes(ServerSettings.messageClientNewConnectionRequest.AsSpan(), connectionEstablisherPacket.Span(0, 16));
                localEP.Address.TryWriteBytes(connectionEstablisherPacket.Span(16, 4), out _);
                BinaryPrimitives.WriteInt32LittleEndian(connectionEstablisherPacket.Span(20, 4), localEP.Port);
                socket.Send(connectionEstablisherPacket.Span());
            }
        }
    }

    public void AddMissingPacketRequest()
    {

    }

    public bool ConnectionTimeoutCheck()
    {
        timeoutTimer += Time.deltaTime;
        if (timeoutTimer > 0f)
        {
            connectionState = ConnectionState.Disconnected;
            return true;
        }
        return false;
    }

    // REGULAR TICK
    // EXTRAPOLATED TICK
    // RECONCILIATION TICK
    private void Tick()
    {
        /*
         *  EXTRAPOLAATIOSSA EI VOI TIETÄÄ MITKÄ OBJECTIT EDES TEKEE JOTAIN JA MITÄ NE TEKEE
         *  PITÄÄ MYÖS VARMISTAA ETTEI ENNUSTA OBJECTEJA JOIDEN DATA ON TULLU PERILLE
         *  ELI ONKO OBJECTISSA JOKU BOOL TAI JOKU MIKÄ FLIPPAIS JOS ON TEHNY JUTTUNSA
         *  PITÄISKÖ VAAN HYLÄTÄ PAKETIT ELI EXTRAPOLOIDA KOKO TICK? MUTTA ENEMMÄN VÄÄRÄSSÄ SE ON KU YHEN PAKETIN EXTRAPOLAATIO.
         * 
         *  OBJECT.HASCHANGED!!! ELI JOS PUUTTUU NI EROTA JA KÄSITTELE SITTE KU KAIKKI LÖYTYVÄT ON TEHTY
         *  SITTE VAAN LOPUILLE INTERPOLOINTI
         *  
         *  MITES LUETTU (CLIENT) PUOLI HASCHANGED TOIMINTO EDES ON?
         *  PITÄÄKÖ LISÄTÄ OBJECTEIHIN INT LASTUPDATEDINTICKNUMBER ?
         *  !!!! HASCHANGED RIITTÄÄ KOSKA TICK SÄÄTÄÄ FALSEKS ELI PAKETTEJA LUETTAESSA PÄIVITETYT ON TRUE JA LOPUT EXTRAPOLOIDAA
         */
        
        if (connectionState == ConnectionState.Reconcile)
        {
            // WILD SHIT HERE. MANY UPDATES AT THE SAME TIME
            // ELI LUE PAKETTI + TICK ALL REPEAT KUNNES KÄYTY KAIKKI LÄPI
        }
        else
        {            // AVAA PAKETIT JA SERIALISOI
            // JOS PUUTTUU NI KOHTA TALTEEN JA EXTRAPOLAATIO KÄYNTIIN


            // REG

            foreach (MnetObject obj in objectsBeingSynced)
            {
                obj.Tick();
            }
        }


        
    }

    private void Update()
    {
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

        clientTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;

        ///  FAster rate often
        /// Client tick. Move player with inputs, store inputs, send inputs
        if (clientTimer > ServerSettings.clientSendRate)
        {
            ClientUpdate();
            clientTimer -= ServerSettings.clientSendRate;
        }

        if (tickTimer > ServerSettings.serverSendRate)
        {
            // Slower rate
            // World tick. Read packets and update all objects
            ReceivePackets();
            Tick();
        }
    }

    private void ReceivePackets()
    {
        timeoutTimer++;
        if(timeoutTimer > ServerSettings.clientTimeoutLimit)
        {
            connectionState = ConnectionState.Disconnected;
            return;
        }

        while(socket.Available > 0)
        {
            timeoutTimer = 0f;
            socket.Receive(currentPacket.WholePacket());
            int packetNumber = currentPacket.ServerPacketNumber;
            /// jos väärä ni swappia ja huomioi puuttuva?
            if (packetNumber != nextExpectedPacketNumber)
            {
                MnetPacket correctPacket = worldStateBuffer.Get(packetNumber);
                currentPacket.SwapData(correctPacket);
                correctPacket.InitServerPacket();
                MissingPacketNumbers.Add(nextExpectedPacketNumber);
                if (connectionState == ConnectionState.Connected)
                {
                    connectionState = ConnectionState.MissingPackets;
                }
                missedPackets++;
                /*
                short packetLength = currentPacket.ServerPacketLength;
                MnetPacket correctPacket = worldStateBuffer.Get(packetNumber);
                byte[] dataSwap = correctPacket.Data;
                correctPacket.SwapBytes(currentPacket.Data, packetLength);
                */
            }
            else
            {
                // Check if client had missed this packet earlier
                if(connectionState == ConnectionState.MissingPackets)
                {
                    bool intermediatePacketsReceived = true;
                    MnetPacket checkPacket = currentPacket;
                    for (int i = 1; i < missedPackets; i++)
                    {
                        checkPacket = checkPacket.nextPacket;
                        if (!checkPacket.isActive)
                        {
                            intermediatePacketsReceived = false;
                            break;
                        }
                    }
                    if (intermediatePacketsReceived)
                    {
                        connectionState = ConnectionState.Reconcile;
                    }
                    // Jos saavutettiin reconcile tila ni missedPackets = 0
                }
                // jos kaikki meni nappii ni samal voidaan ottaa pois missing listasta jos tuli
                // 
                // If the packet was missing, remove it from the list
                MissingPacketNumbers.Remove(packetNumber);
                currentPacket.InitServerPacket();
                // napataa seuraava odotettu paketti (voi olla että on täytettyjä jos tuli väärässä järjstykses)
                while (currentPacket.isActive)
                {
                    nextExpectedPacketNumber++;
                    currentPacket = currentPacket.nextPacket;
                }
            }
            
            // tsekkaa jos on jo olemassa
            /// Jos oikee ni prosessoi
        }

        // Mikä vitu tää on?
        if(MissingPacketNumbers.Count > 0) { }
    }

    private void UpdatePlayer()
    {

    }

    private void CreatePlayerPacket()
    {

    }

    /// tarvitaanko client puolella world state sync? Vois ol eeeeehkä kätevä?
    private void ClientUpdate()
    {
        switch (connectionState)
        {
            case ConnectionState.Connected:
                UpdatePlayer();
                CreatePlayerPacket();
                break;
            case ConnectionState.Handshake:
                HandshakeWithHandler();
                break;
            case ConnectionState.ContactServer:
                RequestConnection();
                break;
            default:
                break;
        }
    }

    private void ReadPacket()
    {

        // Tsekkaa montako? Eli loopataanko täällä vai luetaanko ulkopuolelta pala kerrallaan? Tää olis paras paikka ku voi lukee splitin

        // Pituus tarvitaan, voidaan tallettaa suoraan pakettiin.. tai tehäänkö receivissä? Ei kai turhaan

        // SERVER PACKET [TYPE 1b][PACKET NUMBER 4b][TICK NUMBER 4b][PACKET SIZE 2b][TICK TIME 4b][SPLIT / TOTAL 2b][DATA X * Yb]

        // Eka tyyppi, eli jos sielt tulis vaikka disconnect ni lakkautetaan prosessit?
        // Packet numero turhake?
        // Koko tärkee
        // Tick turhake? Last successful vastaus?
        // Time otetaan ylös floattiin kai
        // splitit tärkee
        // Data tietysti tärkee

        // DATA: Eli while remaining -> object numero, koko -> object read
        // Sit ku loppuu ni loppuu


        // DEBUG SHIT REMVOE
        if (currentPacket == null)
        {
            currentPacket = playerBuffer.Get(0);
        }

        short dataRead = currentPacket.headerLength;
        int numberOfPacketsRemaining = currentPacket.extraPacketsInUpdate + 1;

        // EKA LOOP PAKETTTI MÄÄRÄ
        // SISÄLLÄ LADOTAA OBJEKTEIHIN KUNNES KOKO ON TÄYNNÄ
        // pitäiskö currentlenght vähentyä? Tai vara sinne uus?
        // EXTRAPOLAATIO ELI CHECK ET KAIKKI ON ACTIVE JA JOS EI OO NI MERKATAA EXTRA
        while(numberOfPacketsRemaining > 0 ) 
        {
        
        }

        
        // EXTRAPOLAATIO UPDATE EIKS TÄÄ PITÄNY OLLA TICKISSÄ VITTU

        // KU LUETTU NI NOLLAA
        currentPacket.Reset();
    }

    private void CreatePacket(float frameTime, bool getWorldState = false)
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
            currentPacket = playerBuffer.Get(0);
            currentPacket.isActive = true;
        }

        MnetPacket activePacket;
        MnetPacket firstPacketInUpdate;

        //// Säädetään mihin bufferiin/toimintoon nää kuuluu, joko world state päivitys tai normi
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

        //// Napataan seuraava objecti jos on olemassa ja seuraavassa loopissa käsitellään
        //while (currentObjectIndex < objectsBeingSynced.Length)
        foreach (MnetObject activeObject in objectsBeingSynced)
        {
            //activeObject = objectsBeingSynced[currentObjectIndex];
            /*
            //// TYhjä slot, NEXT!
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

            //// Niin kauan ku objeti tarvii kodin ni loopataan ja etitään pakettia johon mahtuu
            while (processingObject)
            {
                //// Katotaa mahtuuko objekti edes pakettiin
                if (sizeOfObject <= (ServerSettings.maxPacketSize - activePacket.currentLength))
                {
                    //// Mahtuu eli otetaan paketista loput tavut ja kirjotetaan objekti niihin
                    activeObject.WriteChanges(activePacket.RemainingPacketSpace(), getWorldState);
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
            Span<byte> packetNeedingHeader = activePacket.PacketHeader();
            BinaryPrimitives.WriteInt32LittleEndian(packetNeedingHeader.Slice(ServerSettings.headerServerPacketNumberPosition, 4)
                , nextExpectedPacketNumber++);//currentPacketNumber + i);
            BinaryPrimitives.WriteInt16LittleEndian(packetNeedingHeader.Slice(ServerSettings.headerServerSizePosition, 2)
                , activePacket.currentLength);
            // Unity does not support all BinaryPrimitives' methods, so BitConverter is used as a substitute
            BitConverter.TryWriteBytes(packetNeedingHeader.Slice(ServerSettings.headerServerTickTimePosition, 4), currentFrameTime);
            BinaryPrimitives.WriteInt32LittleEndian(packetNeedingHeader.Slice(ServerSettings.headerServerTickNumberPosition, 4),
                currentFrameNumber);

            // If the frame needs multiple packets, add sequence number and total number of packets.
            // If singular packet, set values to zero.
            packetNeedingHeader[ServerSettings.headerServerTickSplitInfoPosition] =
                (numberOfExtraPacketsNeeded > 0) ? (byte)i : (byte)0;
            packetNeedingHeader[ServerSettings.headerServerTickSplitInfoPosition + 1] =
                (numberOfExtraPacketsNeeded > 0) ? (byte)numberOfExtraPacketsNeeded : (byte)0;
            //buffer.Add(currentPacketNumber + i, packetNeedingHeader);
            activePacket = activePacket.nextPacket;
        }
        currentPacket.extraPacketsInUpdate = numberOfExtraPacketsNeeded;
        //// Jos oli normi päivitys ni tiedetään mistä jatkaaa seuraavassa rundissa
        if (!getWorldState) currentPacket = activePacket;
    }

    public void Disconnect()
    {
        if(connectionState == ConnectionState.Connected)
        {
            connectionEstablisherPacket.Data[0] = (byte)MessageType.Disconnect;
            Encoding.ASCII.GetBytes(ServerSettings.messageDisconnectByClient.AsSpan(), connectionEstablisherPacket.Span(1, 16));
            for (int i = 0; i < 3; i++)
            {
                socket.Send(connectionEstablisherPacket.Span(0, 17));
            }
        }
        connectionState = ConnectionState.Disconnected;
        socket.Close();
    }
}


