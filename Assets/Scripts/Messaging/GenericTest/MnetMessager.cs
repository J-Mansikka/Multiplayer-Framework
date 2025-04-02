using System;
using System.Net;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public abstract class MnetMessager : MonoBehaviour
{





    [HideInInspector]
    public MnetPacketBuffer genericBuffer;
    private MnetPacketBuffer worldSnapshotBuffer; // TARVIIKO?
    //private MnetPacketBuffer worldStateBuffer;
    public MnetObject[] incomingObjectData;     // TARVITAAN EHKÄ SE JOKA YHDISTÄÄ ARRAY JA LINKITYKSE?!

    //private int currentPacketNumber;
    //private int lastTickSize;
    //private int currentTickNumber;
    //private float currentTickDuration;
    //private int worldStateTickVersion;          // Current tick number of stored world snapshot in buffer, so we only do it once if multiple need it
    //private List<MnetPacket> writeBuffer;
    public MnetObject[] objectsBeingSynced;
    public Socket[] remoteConnections;
    protected Socket socket;
    protected MnetPacket EiOleOlemassaThisPaketti;
    protected int latestPacketNumber;
    protected int latestTickNumber;
    protected float latestDeltaTime = 1f;
    protected int timeoutCounter;
    protected int VANHATICKSIZENOTINUSE = 0;
    protected bool isServer;

    HashSet<short> objectsMarkedImportant;  // WHAT THE HELL IS THIS?!

    public void Setup()
    {
        genericBuffer = new MnetPacketBuffer();
        latestPacketNumber = 0;
        latestTickNumber = 0;
        latestDeltaTime = 0f;
        timeoutCounter = 0;
    }

    public void DebugPrintPacket()
    {
        int count = genericBuffer.Get(0).extraPacketsInUpdate;
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

    public void WriteRegularPacket(MnetPacketBuffer buffer)
    {
        // Q: Onko edes muita paketti tyyppejä? Eiks se oo tää aina, ainut ero et eka voidaa pakottaa 

        // OTA BUFFERISTA EKA JA SITTE KU UUS PAKETTI TARVITAAN NI SIIRRÄ MYÖS FREE PACKETTI SEURAAVAA ET ON AINA VIIMINEN
        MnetPacket firstPacketInUpdate = buffer.packetForWriting;
        MnetPacket curPacket = firstPacketInUpdate;
        int extraPacketsNeeded = 0;
        int currentPacketNumber = 0;
        firstPacketInUpdate.isActive = true;    // ??? Nollaa sitte ku lähetty mut voi olla kirjottajal turha?
        firstPacketInUpdate.Reset();
        for (int i = 0; i < objectsBeingSynced.Length; i++)
        {   // LOOP ALL OBJECTS
            while (objectsBeingSynced[i].WriteChanges(curPacket))
            {   // WHILE LOOP WRITE OBJET
                if (currentPacketNumber == extraPacketsNeeded)
                {   // NEW PACKET, INCREASE MAX IF NECESSARY
                    extraPacketsNeeded++;
                    curPacket.nextPacket.Reset();
                }
                currentPacketNumber++;
                curPacket = curPacket.nextPacket;
            }
            // RESET TO FIRST PACKET WHEN DONE WITH OBJECT
            curPacket = firstPacketInUpdate;
            currentPacketNumber = 0;
        }
        // *CLIENT PACKET[TYPE 1b] miks ei packet numba? [LAST TICK RECEIVED 4b][SIZE 2b][INPUT DATA ?][PREVIOUS INPUTS...
        // *SERVER PACKET[TYPE 1b][PACKET NUMBER 4b][TICK NUMBER 4b][PACKET SIZE 2b][TICK TIME 4b][SPLIT / TOTAL 2b][DATA X * Yb]
        // Add headers
        
        for (int i = 0; i <= extraPacketsNeeded; i++)
        {
            // Yhteinen headeri luonti MnetToolsis vaikka tai voi if switch tehdä tähä et onks client vai servu
            // Mitä pitää lähettää? Packet ja... ?
            // Set type on first byte
            curPacket.PacketType = MessageType.Regular;
            // !! OLI METODI JO? curPacket[0] = (byte)MessageType.Regular;

            // Packet number
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerPacketNumberPosition,
                MnetSettings.headerPacketNumberLength),
                buffer.nextFreePacketNumber,
                MnetSettings.headerPacketNumberLength);
            // Latest tick number
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerTickNumberPosition,
                MnetSettings.headerTickNumberLength),
                latestTickNumber,
                MnetSettings.headerTickNumberLength);
            // Packet size
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerSizePosition,
                MnetSettings.headerSizeLength),
                curPacket.currentLength - curPacket.headerLength,
                MnetSettings.headerSizeLength);
            // Tick delta time
            MnetTools.FloatToBytes(curPacket.Span
                (MnetSettings.headerDeltaTimePosition,
                MnetSettings.headerDeltaTimeLength),
                latestDeltaTime);
            /*  !!!! Jos kerra paketit järjestykses ja tiedetää mis alotetaa ni index on turha ja total count on vaa tarpee
            // Multi packet index
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerPacketCountInfoPosition,
                MnetSettings.bytesReservedForMultiPacketSize),
                i,
                MnetSettings.bytesReservedForMultiPacketSize);
            */
            // Multi packet total count    
            MnetTools.IntToBytes(curPacket.Span
                (MnetSettings.headerPacketCountInfoPosition,
                MnetSettings.headerPacketCountInfoLength),
                extraPacketsNeeded,
                MnetSettings.headerPacketCountInfoLength);


            // !!!!! ONKO OK ETTÄ REGULAR PAKETISSA ON SAMA HEADERI SERVERIS JA CLIENTIS?
            curPacket = curPacket.nextPacket;
            buffer.nextFreePacketNumber++;
            // latestPacketNumber++; Ottaaks numba messagerist vai bufferist? ehkä bufferist?
        }
        // At the end of the loop curPacket holds the next free packet that is not part of update, so we can store it for the buffer
        buffer.packetForWriting = curPacket;
    }

    public void ReadRegularPacket(MnetPacketBuffer buffer)
    {

        // 1: Ota paketti. 2: Luo loop sen mukaan mikä on pakettien lukumäärä. 3. Lue segmenttejä kunnes tila loppuu.
        // 4. Merkkaa paketti vapaaks. 5. Toista kunnes paketit loppu. 6: Varmista että lopuksi packetToProcess on nexPacket viimisestä
        int packetCount = buffer.packetForProcessing.GetTotalPacketsInUpdate();
        int packetDataAmount = buffer.packetForProcessing.GetPacketSize();
        int readPos = buffer.packetForProcessing.headerLength;

        int objectID = 0;
        int objectSize = 0;
        for(int i = 0; i <= packetCount; i++)
        {
            // Talleta koko mutta muuten vaan bytetoint ja readpos + offset ja lopuks readpos + id + size +data.length
            while (packetDataAmount > 0)
            {
                buffer.packetForProcessing.GetObjectInfo(readPos, out objectID, out objectSize);
                readPos = readPos + MnetSettings.bytesReservedForObjectID + MnetSettings.bytesReservedForSegmentSize;
                objectsBeingSynced[objectID].ReadChanges(buffer.packetForProcessing.Span(readPos, objectSize));
                readPos += objectSize;
                packetDataAmount -= (MnetSettings.bytesReservedForObjectID + MnetSettings.bytesReservedForSegmentSize + objectSize);
            }
            buffer.packetForProcessing.Reset();
            buffer.packetForProcessing = buffer.packetForProcessing.nextPacket;
            packetDataAmount = buffer.packetForProcessing.GetPacketSize();
            readPos = buffer.packetForProcessing.headerLength;
        }
    }

    public void SendPacketsInTick()
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
        int packetCount = genericBuffer.packetForProcessing.GetTotalPacketsInUpdate();
        // Store the position of the first packet in update so we can return to it while looping
        MnetPacket firstPacketInUpdate = genericBuffer.packetForProcessing;
        for (int i = 0; i < remoteConnections.Length; i++)
        {
            // We reset the position here instead of a the end of the loop.
            // This way packetToProcess ends up as the first packet in the next update when the loops are done.
            genericBuffer.packetForProcessing = firstPacketInUpdate;
            for (int p = 0; p <= packetCount; p++)
            {
                remoteConnections[i].Send(genericBuffer.packetForProcessing.WholePacket());
                genericBuffer.packetForProcessing = genericBuffer.packetForProcessing.nextPacket;
            }
        }
    }

    // Send on vähä sama ku lukeminen eli ei packetToProcess ni tää olis iha vaa otetaa kaikki taltee llman miettimist
    // SIIS muista että on prosessontoin kohta bufferissa ja kirjotus kohta ni tää on kirjotus kohta
    public bool ListenForIncoming(Socket socket, MnetPacketBuffer buffer)
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

        1. While loop (socket.available > 0), else timeouttimer++
        2. Talleta oikeeseen kohtaan.
        3. Jos isompi luku ku odotettu, ota taltee ja tee packet.swap. Merkkaa isActive et ku haetaan seuraava ni skipata valmiit
        4. Jos pienempi (vanhempi) ni älä tee mitään ja talleta seuraava saapuva vaan nykyse paketin päälle.
        Puuttuvat ei voi olla vanhempia koska tää jää jumiin odottaa nykystä mikä ei taida olla paras ratkasu?
        5. 
         */

        if (socket.Available > 0)
        {
            MnetPacket firstPacketToWriteOn = buffer.packetForWriting;
            bool checkForMissedPackets = false;
            while(socket.Available > 0)
            {
                // OLisKO PAREMPi OLLA VAAN byte[] receiveBuffer mihi talletattaa ja swapata data sen mukaan mihin menee?
                // Ehkä sit selkeempi tsekata et onko vanhaa dataa, puuttuvaa dataa, oikee data vai liian uutta dataa.
                // Eli sillo tsekata isActive ja jos ei ollu ni missingpackets--? Luku paketti ei hirvee kaukana et jos saapuu
                // tosi vanha paketti ni voi olla että oli jo isactive = false koska oli luettu.
                socket.Receive(buffer.packetForWriting.WholePacket());

                if(buffer.packetForWriting.GetPacketNumber() == buffer.nextFreePacketNumber)
                {
                    buffer.nextFreePacketNumber++;
                    buffer.packetForWriting = buffer.packetForWriting.nextPacket;
                }
                else if(buffer.packetForWriting.GetPacketNumber() > buffer.nextFreePacketNumber)
                {
                    checkForMissedPackets = true;
                    MnetPacket newerPacket = buffer.Get(buffer.packetForWriting.GetPacketNumber());
                    newerPacket.SwapData(buffer.packetForWriting);
                    // JOs ei Puuttuvia nI NEXT FREE PACKET NuMBER = mis mennää
                }
            }

            if (checkForMissedPackets) // && firstPacket.isActive tai siis siihe nyt on talletettu)
            {
                // LOOPPAA ALUsTA LÄPI JA OTA TYLÖS PUUUTTUVAT TAI SUORAAN SANOISIN VOI PISTÄÄ RESEND PYYNNÖN
            }
            return true;
        }
        else
        {
            // If we did not receive anything on this attempt, return false !!! JOS FALSE Ni KUtsuJa LISÄÄ TIMEOUT TIMERII
            return false;
        }

    }

    private void RECEIVEvANHACLIENTVERSIO()
    {
        timeoutTimer++;
        if (timeoutTimer > MnetSettings.maxTimeoutCount)
        {
            connectionState = ConnectionState.Disconnected;
            return;
        }

        while (socket.Available > 0)
        {
            timeoutTimer = 0f;
            socket.Receive(currentPacket.WholePacket());
            int packetNumber = currentPacket.GetPacketNumber();
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
                if (connectionState == ConnectionState.MissingPackets)
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
        if (MissingPacketNumbers.Count > 0) { }
    }

    private void ResendPacket(int packetNumber)
    {
        // !! PITÄÄ EHA TEHDÄ NORMI SEND ET VÄHÄ YMMÄRTÄÄ buffer.Get(packetNumber)
        // TODO
        // VAIHDA TYPE (tavu0) incomingMissedPacket muotoo ja lähetä uudelleen
        // Ei tarvii vaihtaa takas ku jos joku taas sitä pyytää ni sehä on samas tilantees
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
        if (EiOleOlemassaThisPaketti == null)
        {
            EiOleOlemassaThisPaketti = genericBuffer.Get(0);
            EiOleOlemassaThisPaketti.isActive = true;
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
            firstPacketInUpdate = EiOleOlemassaThisPaketti;
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
            /*
             *          OLDIE
             * 
            bool processingObject;      //// EI TARVITA JOS OBJECT PÄÄTTÄÄ
            short sizeOfObject;         //// EI TARVITA KOSKA OBJECT ITSE VERTAA TILAA JA OMAA KOKOA/SEGMENTIN KOKOA
            if (createFullSnapshot)     //// TÄÄ PITÄÄ HUOMIOIDA MUTTA ONKO HYVÄ RATKASU
            {
                processingObject = true;    // Snapshotissa tarvitaa kaikki tieto oli sitte muuttunu tai ei viime frames
                sizeOfObject = activeObject.GetCurrentTotalSize();  // Tarvitaa koko koko (hoho hoho) ku otetaan objekti kokonaisena
            }
            else
            {
                processingObject = activeObject.hasUpdated;
                sizeOfObject = activeObject.currentSize;
            }

            */

            //activeObject.UpdateCurrentSize();

            // ------------------ OIKEETA KOODIA VÄLIAIKASESTI POISTETTU --------------
            /*
            while (activeObject.WriteChanges(activePacket.AvailableSpace(), out activePacket.currentLength, createFullSnapshot))
            {
                // Could not fit object to packet so we need to get the next one
                currentPacketID++;
                activePacket = activePacket.nextPacket;
                // Reserve a new packet if necessary
                if (currentPacketID > numberOfExtraPacketsNeeded)
                {
                    numberOfExtraPacketsNeeded++;
                    activePacket.Reset();
                    activePacket.isActive = true;
                }
            }
            */
            // Start over with next object if remaining
            currentPacketID = 0;
            //// Objecti mahtu niin palataan ekaan pakettiin ja alotetaan alusta
            activePacket = firstPacketInUpdate;

            //// Niin kauan ku objeti tarvii kodin ni loopataan ja etitään pakettia johon mahtuu
            //
            // Eli Objekti olis loopin päättäjä eli tää poistuu ja alla oleva check
            // Objekti saa jäljellä olevan spanni ja tilan ja kattoo riittääkö, jos ei ni siirrytään seuraavaa pakettii
            // Eli ASKELEET
            // 1. Tarjoo paketti
            // 2. Jos false, seuraava paketti
            // 3. Kun valmis, päästää loopista ulos

            // TÄN BOOLI VOIS EHKÄ SÄILYTTÄÄ?
            /*
             *              OLDIE
             * 
            while (processingObject)
            {
                //// Katotaa mahtuuko objekti edes pakettiin
                //  NYT PITÄIS EHKÄ VASTUU SIIRTÄÄ OBJEKTILLE JOKA BOOLILLA KERTOO ONKO VALMIS
                // JA OUT INT TARKASTAIS KULUTETUN TILAN
                // MIKS TÄÄ LASKEE JÄLJELLÄ OLEVAN KOON JOKA KERTA TÄSSÄ KOHTAA?!
                if (sizeOfObject <= (ServerSettings.maxPacketSize - activePacket.currentLength))
                {
                    //// Mahtuu eli otetaan paketista loput tavut ja kirjotetaan objekti niihin
                    activeObject.WriteChanges(activePacket.RemainingPacketSpace(), createFullSnapshot);
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
            */
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
        VANHATICKSIZENOTINUSE = numberOfExtraPacketsNeeded + 1;
        activePacket = firstPacketInUpdate;
        for (int i = 0; i <= numberOfExtraPacketsNeeded; i++)
        {
            if (createFullSnapshot)
            {
                activePacket.PacketType = MessageType.Snapshot;
            }
            else
            {
                activePacket.PacketType = MessageType.Regular;
            }
            // !!! NÄYTTÄÄ VANHALTA MUT VOI OLLA ETTÄ VOI KÄYTTÄÄ VIEL
            /*

            Span<byte> packetNeedingHeader = activePacket.PacketHeader();
            BinaryPrimitives.WriteInt32LittleEndian(packetNeedingHeader.Slice(MnetSettings.headerServerPacketNumberPosition, 4)
                , currentPacketNumber++);//currentPacketNumber + i);
            BinaryPrimitives.WriteInt16LittleEndian(packetNeedingHeader.Slice(MnetSettings.headerServerSizePosition, 2)
                , activePacket.currentLength);

            */
            
            // ! Oikeeta koodia ?!
            /*
            if (createFullSnapshot)
            {
                packetNeedingHeader[ServerSettings.headerServerSizePosition] += (byte)PacketFlag.ContainsWorldSnapshot;                
            }
            */

            // Unity does not support all BinaryPrimitives' methods, so BitConverter is used as a substitute
            /*
            BitConverter.TryWriteBytes(packetNeedingHeader.Slice(MnetSettings.headerServerTickTimePosition, 4), currentTickDuration);
            BinaryPrimitives.WriteInt32LittleEndian(packetNeedingHeader.Slice(MnetSettings.headerServerTickNumberPosition, 4),
                currentTickNumber);
            */
            // If the frame needs multiple packets, add sequence number and total number of packets.
            // If update can fit singular packet and there is no splitting, set values to zero.
            /*
            packetNeedingHeader[MnetSettings.headerServerTickSplitInfoPosition] =
                (numberOfExtraPacketsNeeded > 0) ? (byte)i : (byte)0;
            packetNeedingHeader[MnetSettings.headerServerTickSplitInfoPosition + 1] =
                (numberOfExtraPacketsNeeded > 0) ? (byte)numberOfExtraPacketsNeeded : (byte)0;
            */
            //buffer.Add(currentPacketNumber + i, packetNeedingHeader);
            activePacket = activePacket.nextPacket;
        }
        EiOleOlemassaThisPaketti.extraPacketsInUpdate = numberOfExtraPacketsNeeded;
        //// Jos oli normi päivitys ni tiedetään mistä jatkaaa seuraavassa rundissa
        if (!createFullSnapshot) EiOleOlemassaThisPaketti = activePacket;
    }
}
