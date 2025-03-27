using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MnetMessager : MonoBehaviour
{
    // FAKE SHIT TO STOP ERRORS
    float currentTickDuration = 1f;
    int currentTickNumber = 0;
    int currentPacketNumber = 0;
    int lastTickSize = 0;
    MnetObject[] objectsBeingSynced;


    private MnetPacketBuffer buffer;
    private MnetPacketBuffer worldSnapshotBuffer;
    //private MnetPacketBuffer worldStateBuffer;

    //public MnetObject[] objectsBeingSynced;         // All the objects that are being syncronized
    public MnetObject[] incomingObjectData;     // TARVITAAN EHKÄ SE JOKA YHDISTÄÄ ARRAY JA LINKITYKSE?!

    //private int currentPacketNumber;
    //private int lastTickSize;
    //private int currentTickNumber;
    //private float currentTickDuration;
    //private int worldStateTickVersion;          // Current tick number of stored world snapshot in buffer, so we only do it once if multiple need it
    //private List<MnetPacket> writeBuffer;
    private MnetPacket currentPacket;

    HashSet<short> objectsMarkedImportant;  // WHAT THE HELL IS THIS?!


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
            firstPacketInUpdate.PacketType = MessageType.FullWorldUpdate;
        }
        else
        {
            firstPacketInUpdate = currentPacket;
            firstPacketInUpdate.PacketType = MessageType.Normal;
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
        currentPacket.extraPacketsInUpdate = numberOfExtraPacketsNeeded;
        //// Jos oli normi päivitys ni tiedetään mistä jatkaaa seuraavassa rundissa
        if (!createFullSnapshot) currentPacket = activePacket;
    }
}
