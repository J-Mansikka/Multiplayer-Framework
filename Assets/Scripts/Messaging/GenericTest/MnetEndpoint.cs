using System;
using System.Net;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public abstract class MnetEndpoint : MonoBehaviour
{




    // !!! ELI LOGIIKKA ON ETTÄ MESSAGERIN FIELDIT ON OMIA. ESIM. SERVERIL ON WORLD BUFFER.

    [HideInInspector]
    public MnetPacketBuffer worldBuffer;
    // This packet is used to send messages that are not part of the buffer, e.g. disconnect and snapshot requests
    protected MnetPacket messenger;
    //                      protected MnetInstanceManager manager;
    //private MnetPacketBuffer worldSnapshotBuffer; // TARVIIKO?
    //protected HashSet<int> missingPackets;
    //protected HashSet<int> receivedSnapshotPackets;   // Nyt ku on seq numero ni snapshoti vastaanottaminen on yksinkertasempi
    //private MnetPacketBuffer worldStateBuffer;
    //public MnetObject[] incomingObjectData;     // TARVITAAN EHKÄ SE JOKA YHDISTÄÄ ARRAY JA LINKITYKSE?!

    //private int currentPacketNumber;
    //private int lastTickSize;
    //private int currentTickNumber;
    //private float currentTickDuration;
    //private int worldStateTickVersion;          
    //private List<MnetPacket> writeBuffer;
    // Objects that the local user has control over. In case of server this often means all the objects in the game
    public WANHAMnetObject[] worldObjects;
    // Instance id values that also act as index values for the object array
    public HashSet<int> worldObjectIDs;
    public List<MnetConnection> activeConnections;
    public MnetConnection clientConnection;
    //protected Socket socket;
    //protected MnetPacket EiOleOlemassaThisPaketti;
    // When a snapshot update is needed, we use this bool to prepare for it also act as a send rate limiter (once per snapshot)
    //protected bool waitingForSnapshotPacket;
    protected int currentPacketNumber;
    protected int currentTickNumber;
    protected float latestDeltaTime = 1f;
    protected bool isServer;    // LoL Ehkä vois ol joku parempi. Ehkä vaan tekee toimivuuden perivään Serverii ni client ei voi tehä tuhmuuksii
    protected bool online;
    protected bool gameIsRunning;
    //protected bool createSnapshot;  // Local snapshot eli world tai client
    protected float receiveTimer;
    protected float sendTimer;
    protected float inputTimer;
    protected float tickTimer;
    protected bool createSnapshot;
    protected int activeSceneIndex;

    HashSet<short> objectsMarkedImportant;  // WHAT THE HELL IS THIS?!


    // Pitäiskö olla vaan yhteinen ku muuten täää menee setup base.setup pelleilyks.
    /*
    public void Setup()
    {
        outgoingBuffer = new MnetPacketBuffer();
        specialMessage = new MnetPacket(false);
        activeConnections = new List<MnetConnection> ();
        latestPacketNumber = 0;
        latestTickNumber = 0;
        latestDeltaTime = 0f;
        timeoutCounter = 0;
    }
    */

    /*
    protected void Setup(MnetObject[] worldObjects, bool getActiveObjects = false)
    {
        activeConnections = new List<MnetConnection>();
        localObjectIDs = new HashSet<int>();
        manager = GetComponent<MnetInstanceManager>();
        if(localObjects == null) localObjects = new MnetObject[MnetSettings.maxSyncedObjects];
    }
    */

    public void DebugPrintPacket()
    {
        //      int count = outgoingBuffer.Get(0).extraPacketsInUpdate;
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

    /*
    protected void CreateNewConnectionMessage(string message, MnetPacket packet, int[] values, byte[] incomingBytes = null)
    {
        packet.ResetForWriting();
        int writePos = 0;
        // Message
        System.Text.Encoding.ASCII.GetBytes(message, messenger.Span(writePos, 2));
        writePos += 2;
        if(incomingBytes != null)
        {
            for(int i = 0; i < incomingBytes.Length; i++)
            {
                packet[i+writePos++] = incomingBytes[i];
            }
        }

        for (int i = 0; i < values.Length; i++)
        {
            MnetTools.IntegerToBytes(packet.Span(writePos, 4), values[i], 4);
            writePos += 4;
        }
        messenger.currentLength = writePos;

        // Address
        //int addressLength;
        //localEP.Address.TryWriteBytes(messenger.Span(messenger.currentLength), out addressLength);
        //print(addressLength);
        //messenger.currentLength += addressLength;
        // Port
        //MnetTools.IntToBytes(messenger.Span(messenger.currentLength, 4), port, 4);
    }
    */

    protected void NetworkUpdate()
    {


        receiveTimer += Time.deltaTime;
        //sendTimer += Time.deltaTime;
        inputTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;

        // Tick käydää useamman kerran läpi jos pakko mutta receive toimii samal periaatteel oli viive tai ei
        if (receiveTimer >= Mnet.receiveRate)
        {
            receiveTimer -= Mnet.receiveRate;
            for (int i = 0; i < activeConnections.Count; i++)
            {
                //ListenForIncoming(activeConnections[i]);
            }
        }

        // !!   !!  !! Nähtävästi joo oikeesti on vaan Receive, LocalTick ja Sitten kaikki muut. JÖSSES!

        /*
        Receive: Otetaan vastaan jos on jotain. Paukkuu usein.
        LocalTick: Eli pelaajan objektit päivitetään useammin (eli ei lähetetä mutta otetaan input talteen.
            ??? Miten automatisoitais toi player input? Variaabeleil tulee olee yks back up waan (previousValue)
            Voiko vaan kirjottaa paketin välissä? Tai erillisesti client käyttää messager pohjaa että kirjottaa pakettiin muutokset ja sitte samaan uudestaa?
        TickTimer: Eli tää on se iso missä kyl kai mun näkökannal voidaan paukuttaa kaikki putkeen
        - Tehdään myös tarvittaessa useamman kerran ja sillo pitää miettii mitä deltatime tarkottaa paikallisesti JA lähetettynä.
        - 1. Luetaan seuraava tick.
        -- Serverillä nopee, clientilla hidas
        - 2. Tick
        -- Eli kaikki pelimaailman objektit päivitetään. Serverillä se on local objects ja clientilla remote objects. Pelkkä isServer check ny.
        - 3. Kirjotetaan mitä tapahtu omilla objecteilla. Eli pelaajan liike vs maailman oliot
        -- Serverillä hidas, clientilla nopee
        - 4. Send next tick, melko varmasti eri kuin se mikä just luotiin (innerbuffer paketti väli)
        -- Taitaa olla että kaikilla on vähäsen bufferia. Vois asettaa niin että olis aikka vähä päälle 100ms et ehtis viel korjaa jos puuttuu
             
            */


        // Pitääkö kutsua täällä automatisoinnin nimissä?
        // Kyl kannataa eli tarvitaan... interface? Vähä peritään jo liikaa
        // !isServer && inputTimer
        // Mut tän vois lisätä client messagerii.. mutta miten? LateUpdate.Base() ?

        // This shit is nasty, keksi jotain parempaa!
        // Tarviiko käydä useamman kerran samanlai ku tick? Jos kaks tickiä menny ja saadaa yks ni mene vituiks eikö?
        // Lisään while loopi

        // !!!! Siirrä tää vaan client puolelle. Eli sama juttu, lateupdate ja ajastin

        latestDeltaTime = Time.deltaTime;

        while (!isServer && inputTimer >= Mnet.inputRate)
        {
            inputTimer -= Mnet.inputRate;
            LocalTick();
            // Vois vaa lähetää kerra isol deltal ja sitte tyhjä perään? Vittu ei väsyneenä tiedä 
        }

        // !!! Eiks pitäis olla melkee kaikki while checkejä? Serveris pitäis olla sama sääntö et jos nykäsee ni tehää useampi tick... VAI oliks tää heartbeat juttu taas


        // Eiks tää oo suoraan while loop? Jos timer isompi ku aika ni siin on yks tick
        //if(tickTimer >= MnetSettings.tickRate)
        if (tickTimer >= Mnet.tickRate)
        {
            // !!! PITÄÄ TICKATA NII PALJO KU ON AIKAA ELI VOI OLLA USEAMPI JOS PELI JÄMÄHTÄNY

            // Eli ei saisi missata saapuneita. Onko servulla sama juttu että voisi joutua ajamaan useamman pawn ticki kerralla?
            // Kokeillaa while looppia ja tehdä kaikki kerralla jösses.. BLJÖSSES! This is gonna suck
            // Eiks tää sisäne while oo turha?

            tickTimer -= Mnet.tickRate;

            // LUE REMOTE PACKETS
            // TICK ALL : Miltä tää näyttää clientilla? Client Tick remote eikö? joo eli isServer iskee taas lol?
            // -- Ehkä erillinen worldObjects lista? Siihe sitte clien tallettaa remote ja server localin?
            // KIRJOTA LOCAL PACKET
            // SEND LOCAL PACKET

            foreach (MnetConnection connection in activeConnections)
            {
                if (connection.isActive)
                {
                    if (isServer)
                    {
                        ReadRegularPacket(connection.playerBuffer, connection);
                    }
                    else
                    {
                        ReadRegularPacket(worldBuffer, connection);
                    }
                }
            }

            // worldObjects pointteri pitää sitte osottaa oikeeseen eli Setupit on joo sitte erillisiä.


            // MIKS EI VOI VAAN TEHDÄ IFFFILLÄ TICK KOHDAS`? MIKS MONIMUTKJASTAS
            /*
            if (isServer)
            {
                */
            foreach (int instanceID in worldObjectIDs)
            {
                if (isServer)
                {
                    worldObjects[instanceID].Tick(createSnapshot);
                }
                else
                {
                    worldObjects[instanceID].PawnTick();
                }
            }
            /*
            }
            else
            {
                // Ottaaks tää ny varmasti huomioon pawnit? Kyl pitäis koska pawn on molemmis listois ja samoin oikee player objectikin
                // Vähä sick nasty tää [0] mut tarvitaa toi ja clientilla ei oo kyl yks vitun active connection niin... what?
                // Hyi saatana. Kyl ehkä wolrd objects pointteri olis selkeempi vaiks onki servul tuhalust mut eiks se oo 8 tavuun ifuckit
                MnetConnection server = activeConnections[0];

                foreach (int instanceID in server.playerObjectIDs)
                {
                    server.remoteObjects[instanceID].PawnTick();
                }
            
            }
            */


            // Kirjotus useamman kertaa jos tarvii eikö joo?
            if (isServer)
            {
                WriteRegularOutgoingMessage(worldBuffer, worldObjectIDs);
                SendPacketsInTick(worldBuffer);
            }
            else
            {
                WriteRegularOutgoingMessage(clientConnection.playerBuffer, clientConnection.playerObjectIDs);
                SendPacketsInTick(clientConnection.playerBuffer);
            }
            // Eiks sendiki pauku kahteen kertaan jos pakko?


            // Jos serveri nykäsee ni pitääkö siitä ilmottaa koska nyt taitaa tulla identinen paketti tai tyhjä paketti?

            // Ei taida tietää ennen ku testaa ja simuloi ongelmii?

            currentTickNumber++;

            // Oikea tick tehty, jos on pätkässy että pitäis tehdä useampi, nollataan ne tässä
                
                // Heartbeat lähtee kaikille JA on periaatteessa perus tick eli packetnumber kasvaa ja tallentuu bufferii. size 0 vaa huomioidaa
            while(tickTimer > Mnet.tickRate)
            {
                tickTimer -= Mnet.tickRate;
                SendHeartbeat();
                currentTickNumber++;
            }
        }

        createSnapshot = false;


        // Selvitä tää tilanne. Varmaa helppo kirkkaal pääl ja ei tää oo monimutkane kuiteskaan
        // 1. Lue paketti ja tick. 2. Tee omien objektien tick. 3. Tee paketti. 4. Lähetä varmaanki heti perään?
        // Jos on useampi update ni vois paketin lähettämisen vähän jakaa mut ei voi tietää monta updatee on ni vähä vaikee jakaa pyh
        // Receive Rate
        // Local Tick Rate
        // Send Rate (world tick rate)

    }

    public void CreatePlayerObjects(MnetConnection connection)
    {
        /*
        WANHAMnetObject[] playerObjects = manager.spawner.GetPlayerObjects();
        for (int i = 0; i < playerObjects.Length; i++)
        {
            if (isServer)
            {
                playerObjects[i].ownership = Ownership.SharedLocalAuth;
            }
            else
            {
                playerObjects[i].ownership = Ownership.SharedRemoteAuth;
            }
            // MANAGER + OFFSET + PLAYER SPACE POSITION
            int instanceID = 1 + i + (connection.playerNumber * Mnet.objectsPerPlayer);
            playerObjects[i].objectInstanceID = instanceID;
            worldObjects[instanceID] = playerObjects[i];
            // !!! Lisätään luettaviin
            connection.playerObjectIDs.Add(instanceID);
            // !!! Lisätään aktiivisiin?
            worldObjectIDs.Add(instanceID);
        }
        */
    }

    public abstract void LocalTick();

    /*
    public void Listen()
    {
        MnetConnection connection;
        for (int i = 0; i < activeConnections.Count; i++)
        {
            connection = activeConnections[0];

            if(connection.state > ConnectionState.Connected)
            {

            }
            else if(connection.state > ConnectionState.SnapshotSynchronization)
            {

            }
            else if(connection.state > ConnectionState.TryingToConnect)
            {

            }
            else if(connection.state > ConnectionState.Disconnecting)
            {

            }
            else
            {
                // Not connected
            }
        }
    }

    */

    public void WriteRegularOutgoingMessage(MnetPacketBuffer buffer, HashSet<int> controlledInstances)
    {
        // Q: Onko edes muita paketti tyyppejä? Eiks se oo tää aina, ainut ero et eka voidaa pakottaa 

        // OTA BUFFERISTA EKA JA SITTE KU UUS PAKETTI TARVITAAN NI SIIRRÄ MYÖS FREE PACKETTI SEURAAVAA ET ON AINA VIIMINEN
        MnetPacket firstPacketInUpdate = buffer.packetForWriting;
        MnetPacket curPacket = firstPacketInUpdate;
        int extraPacketsNeeded = 0;
        int activePacketNumber = 0;
        firstPacketInUpdate.Reset();
        //firstPacketInUpdate.isActive = true;    // ??? Nollaa sitte ku lähetty mut voi olla kirjottajal turha?
        //  !!! Ei ollu instanceIDt käytös ni nyt joutuu muokkaa tänki
        //      for (int i = 0; i < objectsBeingSynced.Length; i++)
        foreach(int instanceID in controlledInstances)
        {   // LOOP ALL OBJECTS
            //      while (objectsBeingSynced[i].WriteChanges(curPacket))
            // !!! Turha size lasketaan tässä
            //worldObjects[instanceID].CountSize();

            while (worldObjects[instanceID].WriteChanges(curPacket))
            {   // WHILE LOOP WRITE OBJET
                if (activePacketNumber == extraPacketsNeeded)
                {   // NEW PACKET, INCREASE MAX IF NECESSARY
                    extraPacketsNeeded++;
                    curPacket.nextPacket.Reset();
                }
                activePacketNumber++;
                curPacket = curPacket.nextPacket;
            }
            // RESET TO FIRST PACKET WHEN DONE WITH OBJECT
            curPacket = firstPacketInUpdate;
            activePacketNumber = 0;
        }
        // *CLIENT PACKET[TYPE 1b] miks ei packet numba? [LAST TICK RECEIVED 4b][SIZE 2b][INPUT DATA ?][PREVIOUS INPUTS...
        // *SERVER PACKET[TYPE 1b][PACKET NUMBER 4b][TICK NUMBER 4b][PACKET SIZE 2b][TICK TIME 4b][SPLIT / TOTAL 2b][DATA X * Yb]
        // Add headers
        
        for (int i = 0; i <= extraPacketsNeeded; i++)
        {
            // Yhteinen headeri luonti MnetToolsis vaikka tai voi if switch tehdä tähä et onks client vai servu
            // Mitä pitää lähettää? Packet ja... ?
            // Set type on first byte
            curPacket.Message = MessageType.Regular;
            // !! OLI METODI JO? curPacket[0] = (byte)MessageType.Regular;

            //!!!!! SIIRRETTIIN TYÖKALUT PAKETI PUOLEL KU PALJO SIISTIMPI KÄYTTÄÄ VOI KÄYTTÄÄ MUISSAKI METODEIS JA TILANTEIS (SNAPSHOT ESIM)
            // SIIVOO KU OOT VARMIASTNU ET TOIMII

            // Packet number
            curPacket.SetPacketNumber(currentPacketNumber);
            /*
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerPacketNumberPosition,
                MnetSettings.headerPacketNumberLength),
                buffer.nextExpectedPacketNumber,
                MnetSettings.headerPacketNumberLength);
            */
            // Latest tick number
            curPacket.SetTickNumber(currentTickNumber);
            /*
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerTickNumberPosition,
                MnetSettings.headerTickNumberLength),
                latestTickNumber,
                MnetSettings.headerTickNumberLength);
            */
            // Packet size
            curPacket.SetPacketSize(curPacket.currentLength - curPacket.headerLength);
            /*
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerSizePosition,
                MnetSettings.headerSizeLength),
                curPacket.currentLength - curPacket.headerLength,
                MnetSettings.headerSizeLength);
            */
            // Tick delta time
            curPacket.SetDeltaTime(latestDeltaTime);
            /*
            MnetTools.FloatToBytes(curPacket.Span
                (MnetSettings.headerDeltaTimePosition,
                MnetSettings.headerDeltaTimeLength),
                latestDeltaTime);
            */
            /*  !!!! Jos kerra paketit järjestykses ja tiedetää mis alotetaa ni index on turha ja total count on vaa tarpee
            // Multi packet index
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerPacketCountInfoPosition,
                MnetSettings.bytesReservedForMultiPacketSize),
                i,
                MnetSettings.bytesReservedForMultiPacketSize);
            */
            // Multi packet total count
            curPacket.SetSequenceNumber(i);

            curPacket.SetTotalPacketsInUpdate(1+extraPacketsNeeded);
            /*
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerPacketCountInfoPosition,
                MnetSettings.headerPacketCountInfoLength),
                extraPacketsNeeded,
                MnetSettings.headerPacketCountInfoLength);

            */
            // !!!!! ONKO OK ETTÄ REGULAR PAKETISSA ON SAMA HEADERI SERVERIS JA CLIENTIS?
            curPacket = curPacket.nextPacket;
            currentPacketNumber++;
            // latestPacketNumber++; Ottaaks numba messagerist vai bufferist? ehkä bufferist?
        }
        // At the end of the loop curPacket holds the next free packet that is not part of update, so we can store it for the buffer
        buffer.packetForWriting = curPacket;
    }

    public void ReadRegularPacket(MnetPacketBuffer buffer, MnetConnection connection)
    {
        // 1: Ota paketti. 2: Luo loop sen mukaan mikä on pakettien lukumäärä. 3. Lue segmenttejä kunnes tila loppuu.
        // 4. Merkkaa paketti vapaaks. 5. Toista kunnes paketit loppu. 6: Varmista että lopuksi packetToProcess on nexPacket viimisestä
        // Pitäis toimii heartbeat paketillaki eli luetaan ja packet count = 0 ni ei tehdä mitään
        int packetCount = buffer.packetForProcessing.GetTotalPacketsInUpdate();
        //int packetDataAmount = 
        buffer.packetForProcessing.UpdatePacketSize();
        //int readPos = buffer.packetForProcessing.headerLength;

        //int objectID = 0;
        //int objectSize = 0;
        // !!!! oli <= niin ku luodessa mutta ku kirjotetaan ni se on 1+extraPacketsNeeded eli tässä nyt vaan < packetCount


        //todo
            // RESET() EI AUTA TÄÄLLÄ VAAN PITÄÄ KATTOA PAKETIN NUMERO ETTÄ TIETÄÄ ETTEI OO VANHAA PASKAA
        for(int i = 0; i < packetCount; i++)
        {
            if (connection.nextPacketToProcessNumber == buffer.packetForProcessing.packetNumber)
            {
                //int dataReadSoFar = buffer.packetForProcessing.readPosition;
                // Talleta koko mutta muuten vaan bytetoint ja readpos + offset ja lopuks readpos + id + size +data.length
                while (buffer.packetForProcessing.readPosition != buffer.packetForProcessing.currentLength)//packetDataAmount > 0)
                {
                    //buffer.packetForProcessing.GetObjectInfo(out objectID, out objectSize);
                    //readPos = readPos + Mnet.bytesReservedForObjectID + Mnet.bytesReservedForSegmentSize;
                    worldObjects[buffer.packetForProcessing.GetObjectID()].ReadChanges(buffer.packetForProcessing);//.Read(objectSize));//buffer.packetForProcessing.Span(readPos, objectSize));
                    //readPos += objectSize;
                    //packetDataAmount -= (Mnet.bytesReservedForObjectID + Mnet.bytesReservedForSegmentSize + objectSize);
                    //packetDataAmount -= buffer.packetForProcessing.readPosition - dataReadSoFar;
                }
                buffer.packetForProcessing = buffer.packetForProcessing.nextPacket;
                //packetDataAmount =
                buffer.packetForProcessing.UpdatePacketSize();
                //readPos = buffer.packetForProcessing.headerLength;
                connection.nextPacketToProcessNumber++;
            }
            else
            {
                // WE ARE FUCKED ELI STATE VAIHTO TÄHÄN
                // Tää voi olla iha jees et menee samaa kautta ja sit tossa metodissa booli sijaan vaihdetaan connection state
                SendSnapshotRequest(connection);
                break;
            }
            // We processed one tick, so the buffer between writing and processing shrinks by one
            buffer.writeReadDistanceInTicks--;
        }
    }

    public void SendPacketsInTick(MnetPacketBuffer buffer)
    {
        /*
         Paketit bufferin process kohdasta ja yksi vastaan ottaja. Vai tehdäkö array eli clienti array olis length 1 hmmm
        IPEndpoint vai joku muu? Iha socketti? Pitää kattoo mikä metodi paras ni sen mukaan parametrit
        Mitä jos socket on parametri? Koska siin on jo ipendpoint plus muut asetukset
        
         1. Looppi remoteConnectioneille
        2. Ota paketti määrä
        3. Loop jossa paketti määrän mukaan vaan lähetät ja otat seuraavan packet = packet.nextPacket

        !!!! VOIsKO LOOPATA NIIN ETTÄ LOPUS PACKETORPROCESS JÄIS OIKEESEE KOHTAA SEuraAVAA TICKIÖ VARTE?
         */
        int packetCount = buffer.packetForProcessing.GetTotalPacketsInUpdate();
        // Store the position of the first packet in update so we can return to it while looping
        MnetPacket firstPacketInUpdate = buffer.packetForProcessing;

        foreach(MnetConnection connection in activeConnections)
        {
            // We reset the position here instead of a the end of the loop.
            // This way packetToProcess ends up as the first packet in the next update when the loops are done.
            buffer.packetForProcessing = firstPacketInUpdate;
            for (int p = 0; p <= packetCount; p++)
            {
                Send(buffer.packetForProcessing, connection);
                //activeConnections[i].socket.Send(localBuffer.packetForProcessing.CurrentMessage());
                buffer.packetForProcessing = buffer.packetForProcessing.nextPacket;
            }
        }
    }

    // Send on vähä sama ku lukeminen eli ei packetToProcess ni tää olis iha vaa otetaa kaikki taltee llman miettimist
    // SIIS muista että on prosessontoin kohta bufferissa ja kirjotus kohta ni tää on kirjotus kohta
    // !!!! Heartbeat check on ny tässä heti yhteisenä, mut nää et palauttaa saatiiko mitään ni olis aika tärkee joo
    // !!!! Pitäiskö tällä antaa perijän ja muiden seurata tilannetta (missed packets = false, jos ok ni true) ??
    // POISTA MYÖHEMMI JOS ET LÖYDÄ SYYTÄ SÄILYTTÄÄ!!!!
    public void ListenForIncoming(MnetConnection incoming, MnetPacketBuffer localBuffer) //Socket incomingSocket, MnetPacketBuffer incomingBuffer)
    {
        /*
         Tää ihan karusti ottaa paketit vastaan ja tietää että mikä pitäis olla seuraava numba.
        Tähä saattaa löytyy vanha koodia missä on paketti vaihtelut sun muut valmiina. Pitäiskö olla ihan erikseen joku temp packet
        joho tallettaa mutta sitte data pitäis siirtää ni ehkä se vanha koodi ratkasi tän ongelma
        !!! MITE TUNNISTAA MIsSED PaCKET?
        Idea 1: Ota alotus kohta (eka paketti) ylös. Jos tulee isompi ni ota erillisee inttii ylös.
        Ku receive on valmis, looppaa luvuilla paketit läpi ja merkkkaa viel puuttuvat ylös. Ehkä vois heti tehä missed sendi
        
        Eli tilanteet:
        OIKEA SAAPUI: Älä tee mitään, vaihda vaan seuraavaan pakettiin ja nextPacketNumber++
        VANHA SAAPUI: Älä tee mitään.
        UUDEMPI SAAPUI: SWAP JA MERKKAA ET PITÄÄ TARKASTAA TILANNE JA LOOPPAA JÄLKEE

        1. While loop (socket.available > 0), palauta false jos ei tullu mitään ja lisää timeouttimer kutsuja metodis
        2. Talleta oikeeseen kohtaan.
        3. Jos isompi luku ku odotettu, ota taltee ja tee packet.swap. Merkkaa isActive et ku haetaan seuraava ni skipata valmiit
        -- Jos lukija muuttaa isActive falseks ni voiko mennä päällekkäin eli merkkaa jo luetun vai pysäytetäänkö lukeminen?
        4. Jos pienempi (vanhempi) ni älä tee mitään ja talleta seuraava saapuva vaan nykyse paketin päälle.
        Puuttuvat ei voi olla vanhempia koska tää jää jumiin odottaa nykystä mikä ei taida olla paras ratkasu?
        5. 
         */

        if (incoming.socket.Available > 0)
        {
            incoming.missedHeartbeatCount = 0;
            MnetPacket firstPacketToWriteOn = incoming.playerBuffer.packetForWriting;
            int receivedPacketNumber;
            while(incoming.socket.Available > 0)
            {
                // OLisKO PAREMPi OLLA VAAN byte[] receiveBuffer mihi talletattaa ja swapata data sen mukaan mihin menee?
                // Ehkä sit selkeempi tsekata et onko vanhaa dataa, puuttuvaa dataa, oikee data vai liian uutta dataa.
                // Eli sillo tsekata isActive ja jos ei ollu ni missingpackets--? Luku paketti ei hirvee kaukana et jos saapuu
                // tosi vanha paketti ni voi olla että oli jo isactive = false koska oli luettu.
                incoming.socket.Receive(incoming.playerBuffer.packetForWriting.AllBytes());
                receivedPacketNumber = incoming.playerBuffer.packetForWriting.GetPacketNumber();
                incoming.playerBuffer.packetForWriting.packetNumber = receivedPacketNumber;
                MessageType messageType = incoming.playerBuffer.packetForWriting.Message;
                // CHECK TYPE Ja REAGOI HETI JA SIT TALLETA PÄÄLLE NIIN VANHOIS PAKETEIS.
                // ELI JOS MISSING PYYNTÖ NI HETI RESENDIÄ JA SITTE DISCONNECT ILMOTUS JA SNAPSHOT ETC
                switch (messageType)
                {
                    // Jos tullaa snapin kautta ni voi olla et vähä turhaa prosessoidaa
                    // ja sitte viimisel askeleel tulee aina missing packet mut oletettavasti seuraava pitäis olla se ni ei oo iso ongelma.
                    case MessageType.Regular:
                        {
                            if (receivedPacketNumber == incoming.expectedPacketNumber)
                            {
                                // VOI OLLA ETTÄ SAATU JO ETUKÄTEE NI EI VOIDA VAA MENNÄ YHDELLÄ ETEENPÄIN buffer.nextExpectedPacketNumber++;
                                // If we have missing packets we can check if this was one of missing ones
                                if (incoming.missingPackets.Count > 0)
                                {
                                    incoming.missingPackets.Remove(receivedPacketNumber);
                                }
                                //      incoming.buffer.packetForWriting.isActive = true;
                                // Find the next old packet that will be overriden with receive
                                do
                                {
                                        // Eli tässä kohtaa processBuffer++ aina ku ohitetaan sequence numero 0 ! sitte vaan lukemiseen pb--
                                    // Move on to the next packet and then compare if this is a newer packet or a free old one
                                    incoming.playerBuffer.packetForWriting = incoming.playerBuffer.packetForWriting.nextPacket;
                                    if (incoming.playerBuffer.packetForWriting.GetSequenceNumber() == 0)
                                    {
                                        incoming.playerBuffer.writeReadDistanceInTicks++;
                                    }
                                    incoming.expectedPacketNumber++;
                                }
                                while (receivedPacketNumber < incoming.playerBuffer.packetForWriting.packetNumber);
                            }
                            else
                            {
                                // We did not receive the expected packet so we will add it as a missed one
                                incoming.missingPackets.Add(receivedPacketNumber);

                                // If we received a packet that is newer than the one expected, we will use the packet number to swap the data
                                if (receivedPacketNumber > incoming.expectedPacketNumber)
                                {
                                    MnetPacket newerPacket = incoming.playerBuffer.Get(receivedPacketNumber);
                                    // Simple check to make sure that the received packet was not a duplicate
                                    if (newerPacket.packetNumber != receivedPacketNumber)
                                    {
                                        newerPacket.SwapData(incoming.playerBuffer.packetForWriting);
                                        newerPacket.packetNumber = incoming.playerBuffer.packetForWriting.packetNumber;
                                    }
                                }
                            }
                        }
                        break;
                    case MessageType.MissingPacketsRequest:
                        {
                            // Lähettäjä pyytää puuttuvia paketteja, ota paketti numerot ylös ja lähetä buffer.Get metodilla loopissa
                            // Tarvitaanko toinen hashSet? Vai loopataanko message avulla vaan läpi suoraan. PArempi nii ku ei tarvii muistaa?
                            // The system cannot handle many missed packets so the total amount has to fit between 1-255 (1 byte)
                            int packetCount = incoming.playerBuffer.packetForWriting[Mnet.headerTypeLength];
                            int readPos = Mnet.headerTypeLength + 1; // Type + number of missed packets (1-255 = 1 byte)
                            int packetNumber = 0;
                            for (int i = 0; i < packetCount; i++)
                            {
                                packetNumber = MnetTools.BytesToInteger(incoming.playerBuffer.packetForWriting.Span
                                    (readPos, Mnet.headerPacketNumberLength));
                                    //, Mnet.headerPacketNumberLength)
                                    
                                Send(localBuffer.Get(packetNumber), incoming, PacketPriority.Safe);
                                readPos += Mnet.headerPacketNumberLength;
                            }
                            
                        }
                        break;
                    /*
                    case MessageType.MissingPacketsIncoming:
                        {
                            // Saadaan puuttuva mikä pyydettiin aikasemmin...
                            // EI TARVITA, MENEE REGIN KAUTTA
                        }
                        
                        break;
                    */
                    case MessageType.SnapshotRequest:
                        {
                            // Lähettäjä on huomannu että paketteja puuttuu liikaa tai on jääny liikaa jälkeen että tarttis snapshotin
                            // ??? Looppaa objektit läpi ja pyydä valmistaa snapshot update

                            // Snapshot request is simply the type byte and the value of the tick number when the call was made
                            int tickNumber = incoming.playerBuffer.packetForWriting.GetTickNumber();
                            // There might be multiple snapshot messages that arrive on different times so we check a cooldown before committing
                            // !!! cooldown määrä tarkottaa et jos joinais tikc 0-10 vaikka ni ei sais snapshottia
                            if (tickNumber > incoming.snapshotRequestTick + Mnet.snapshotCooldownInTicks)
                            {
                                createSnapshot = true;
                                /*
                                foreach (MnetObject obj in localObjects)
                                {
                                    obj.createSnapshotUpdateNext = true;
                                }
                                */
                                incoming.snapshotRequestTick = tickNumber;
                            }
                        }
                        break;
                    case MessageType.SnapshotIncoming:
                        {
                            // Snapshot päivitys. Vaikuttaa vasta käsittelyssä mun mieleestä. Koska kirjotus on edellä ni ei voida nopeempaa
                            // versiota ehkä antaaa ni tää tulee samal vauhdilla ku normi paketit
                            // Snapshotissa vois myös tehdä sen että ku käyttää interpolointi ajan (wrong->snapshot) siihe että luo uude pufferi
                            // välin write paketin ja process paketin välille.

                            // !!!!!     PACKETOPROCESS PITÄÄ HYPÄTÄ KANSSA        !!!!!!

                            // EHKÄ TÄN KAUTTA MENNÄÄ REGGIIN KUNNES ON FULL UPDATE KASASSA NI SITTE waiting = false ja packetToPRocess = eka?

                            // Okei koska seuranta on jo päin vittua ni voidaan skipata ja keskittyä snappiin
                            if (incoming.firstPacketInSnapshot == -1)
                            {
                                // Move on the correct packet for the snapshot, swap the received data and also adjust the next free packet 
                                //int packetNumber = incoming.buffer.packetForWriting.GetPacketNumber();
                                // Paketti määrä
                                int totalUpdateSize = incoming.playerBuffer.packetForWriting.GetTotalPacketsInUpdate();
                                // Otetaan oikee paketti
                                MnetPacket snapshotPacket = incoming.playerBuffer.Get(receivedPacketNumber);
                                // Vaihdetaan data että seuravaaks luetaan vapaaseen
                                snapshotPacket.SwapData(incoming.playerBuffer.packetForWriting);
                                //      incoming.buffer.packetForWriting.isActive = false;
                                // Päivitetään seuraava paketti jota odotetaan
                                incoming.playerBuffer.packetForWriting = snapshotPacket.nextPacket;
                                //      snapshotPacket.isActive = true;


                                // !!! Nyt ku on seq numero ni tiedetään mikä on eka eli säädetään nextexpected vaan siihe



                                // We need to know what packet number comes next so we check if the one received is the first one or not
                                int sequenceNumber = snapshotPacket.GetSequenceNumber();
                                incoming.firstPacketInSnapshot = receivedPacketNumber - sequenceNumber;
                                if (sequenceNumber != 0)
                                {
                                    // We did not receive the first one, so we aim to get that one as soon as possible
                                    incoming.expectedPacketNumber = incoming.firstPacketInSnapshot;
                                }
                                else
                                {
                                    // We got the first one in the update, so the wanted packet number is the next one
                                    incoming.expectedPacketNumber = receivedPacketNumber + 1;
                                }

                                if (totalUpdateSize > 1)
                                {
                                    for (int i = 0; i < totalUpdateSize; i++)
                                    {
                                        if (i != sequenceNumber) incoming.snapshotPacketsLeft.Add(incoming.firstPacketInSnapshot + i);
                                    }
                                }
                                else
                                {
                                    // If the snapshot is a single packet, we can close up early
                                    incoming.playerBuffer.packetForProcessing = incoming.playerBuffer.Get(incoming.firstPacketInSnapshot);
                                    // If we received a snapshot update, we can ignore previously missed packets
                                    incoming.missingPackets.Clear();
                                }


                                /*
                                receivedSnapshotPackets.Add(incoming.buffer.packetForWriting.GetPacketNumber());
                                
                                if (!snapshotPacket.isActive)
                                {
                                    snapshotPacket.SwapData(incoming.buffer.packetForWriting);
                                    snapshotPacket.isActive = true;
                                }
                                if (receivedSnapshotPackets.Count == incoming.buffer.packetForWriting.GetTotalPacketsInUpdate())
                                {
                                    // IF FINISHED

                                    // Otetaanko ny pois vai prosessoidessa? Prosessointi kai aina sama ni voi jo ottaa pois?
                                    waitingForSnapshot = false;
                                    int firstPacket = int.MaxValue;
                                    int lastPacket = 0;
                                    foreach (int packetNumber in receivedSnapshotPackets)
                                    {
                                        if (packetNumber < firstPacket) firstPacket = packetNumber;
                                        if (packetNumber > lastPacket) lastPacket = packetNumber;
                                    }

                                    // !! Eiks tää oo +1 eli lastpacket on osa viel snappia ni kirjotetaan seuraavaa
                                    incoming.buffer.packetForWriting = incoming.buffer.Get(lastPacket + 1);
                                    incoming.buffer.nextExpectedPacketNumber = lastPacket;
                                    receivedSnapshotPackets.Clear();

                                    // !!! KU SNAPPI PROSESSOIDAAA NI LUODAAN UUS BUFFERI JOKO INTERPOLAIMAL VÄHÄ AIKAA TAI IHAN VITTU PAUSELLA
                                }
                                */
                            }
                            else
                            {
                                if(incoming.snapshotPacketsLeft.Count > 0)
                                {
                                    incoming.snapshotPacketsLeft.Remove(receivedPacketNumber);
                                    if(incoming.snapshotPacketsLeft.Count == 0)
                                    {
                                        incoming.playerBuffer.packetForProcessing = incoming.playerBuffer.Get(incoming.firstPacketInSnapshot);
                                        // If we received a snapshot update, we can ignore previously missed packets
                                        incoming.missingPackets.Clear();
                                        incoming.firstPacketInSnapshot = -1;
                                    }
                                }
                                // Snapshot updates arrive for everyone, so if there are no issues they will be stored like regular packets
                                goto case MessageType.Regular;
                            }
                        }
                        break;
                    case MessageType.DisconnectNotification:
                        {
                            // Lähettäjä on sulkemassa yhteyden, toista tässä päässä
                            Disconnect(incoming, DisconnectCause.RequestedByRemote);
                        }
                        break;
                    case MessageType.Heartbeat:
                        {
                            goto case MessageType.Regular;
                        }
                        break;
                    default:
                        break;
                }

            }

            // Check to see if we still have missing packets
            if (incoming.missingPackets.Count > 0) // && firstPacket.isActive tai siis siihe nyt on talletettu)
            {
                // !!! TÄÄ CHECK EPÄILYTTÄÄ EIKÄ OO KAUHEEN HYVÄ MUUTENKAAN
                // Mutta jos yhteys EI oo poikki, mutta pätkii ku jumalauta ni ei koskaan tuu timeouttia. Dropout tarvitaan! Mutta luultavasti voi olla parempi ku tää
                if (incoming.missingPackets.Count > Mnet.maxMissedPacketsBeforeDropout)
                {
                    Disconnect(incoming, DisconnectCause.UnstableConnection);
                }
                else
                {
                    // We have missing packets and need to send a resend request for them
                    SendMissingPacketsResendRequest(incoming);
                }



                /*
                if (incoming.missingPackets.Count <= MnetSettings.maxMissedPacketsBeforeSnapshot)
                {
                    SendMissingPacketsResendRequest(incoming);
                }
                */

                /*
                else if (incoming.missingPackets.Count > MnetSettings.maxMissedPacketsBeforeDropout)
                {
                    Disconnect(incoming, DisconnectCause.UnstableConnection);
                }
                */
            }
        }
        else
        {
            // If we did not receive anything on this attempt, return false !!! JOS FALSE Ni KUtsuJa LISÄÄ TIMEOUT TIMERII
            incoming.missedHeartbeatCount++;
            if(incoming.missedHeartbeatCount > Mnet.WANHAheartbeatLimit)
            {
                if(incoming.missedHeartbeatCount > Mnet.WANHAtimeoutLimit)
                {
                    Disconnect(incoming,DisconnectCause.Timeout);
                }
                else
                {
                    SendSnapshotRequest(incoming);
                }
            }
        }

    }

    public void Send(MnetPacket packet, MnetConnection connection, PacketPriority priority = PacketPriority.Regular)
    {
        int sendCount = (int)priority * connection.sendRate;
        for (int i = 0; i < sendCount; i++)
        {
            connection.socket.Send(packet.CurrentMessage());
        }
    }

    public void SendHeartbeat()
    {
        messenger.Reset();
        messenger.Message = MessageType.Heartbeat;
        // !! PacketNumber on vaan normi paketeil jotka tallentuu bufferiin
        //outgoingMessage.SetPacketNumber(currentPacketNumber);
        messenger.SetTickNumber(currentTickNumber);

        foreach (MnetConnection connection in activeConnections)
        {
            Send(messenger, connection);
        }
    }

    public void ChangeScene(int newIndex)
    {

    }

    public void SendMissingPacketsResendRequest(MnetConnection outgoing)
    {
        messenger.Reset();
        messenger.Message = MessageType.MissingPacketsRequest;
        //messenger[Mnet.headerMessageTypeLength] = (byte)outgoing.missingPackets.Count;
        messenger.WriteSingleByte((byte)outgoing.missingPackets.Count);
        //int writePos = Mnet.headerMessageTypeLength + 1;
        foreach(int i in outgoing.missingPackets)
        {
            messenger.Write(i);
            //MnetTools.IntegerToBytes(messenger.Span(writePos), i, Mnet.headerPacketNumberLength);
            //writePos += Mnet.headerPacketNumberLength;
        }
        //messenger.currentLength = writePos;
        Send(messenger, outgoing, PacketPriority.Safe);
        //outgoing.socket.Send(specialMessage.CurrentMessage());
    }

    public void SendSnapshotRequest(MnetConnection outgoing)
    {
        // Tarvitaanko ees mitään dataa? Type kertoo mikä on jutun ydin...
        // ELI TYPE JA TICK NUMBER JA TULIKO MUUTA? Miten lasketaa RTT tickeinä? rtt / updateRate ?
        // !!! Miks tick? Ei sitä ainakaan käytetä ku otetaan p
        messenger.Reset();
        messenger.Message = MessageType.SnapshotRequest;
        messenger.SetTickNumber(currentTickNumber);
        // 
        outgoing.playerBuffer.writeReadDistanceInTicks = 0;
        Send(messenger, outgoing, PacketPriority.Important);
        //outgoing.socket.Send(outgoingMessage.CurrentMessage());
        outgoing.state = WANHAAConnectionState.Desynced;
    }

    public abstract void Disconnect(MnetConnection connection, DisconnectCause cause);
}
