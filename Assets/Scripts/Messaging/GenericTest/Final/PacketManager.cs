using System;
using System.Collections.Generic;
using UnityEngine;

public class PacketManager
{

    // OutgoingPacketNumber: Current packet number for writing
    // IncomingPacketNumber: 
    //
    //
    //
    //
    //
    //
    //

    //public int writePosition; Ei tarvita koska headerin osat vaihdetaan suoraan ja dataa ku kirjotetaan ni kirjotetaan vaan aina kerran ja olemassa olevan per‰‰n
    //public int packetNumber;
    //public bool isActive;
    //public bool isFull;
    //public int tickNumber;
    //private PacketType regularPacketType;
    public PacketType BufferType { get; private set; }
    public int currentPacketNumber;
    //public int incomingPacketNumber;
    public int expectedPacketNumber;
    public int highestPacketNumberReceived;
    public int currentTickNumber;
    private bool incomingPlayerPackets;
    private int playerID;
    private HashSet<int> validObjectIDs;
    /*
    public int TickBuffer
    {
        get { return lastTickReceived - lastTickProcessed; }
    }
    */
    // Pit‰is riitt‰‰ et palauttaa nyt 1 jos client hakee snapshotin p‰‰ttymist
    public int lastTickProcessed; // Pit‰is olla oikein
    public int lastTickReceived; // Kattoo vaan saadusta korkeimman tickin eli ei valmiita. Yhden v‰‰r‰s siis

    //public bool isCircularBuffer;

    private MnetNetwork local;

    public Packet[] packetBuffer;

    public Packet outgoingPacket;   // LOCAL: Packet to be send with local data
    public Packet incomingPacket;   // REMOTE: Packet to receive incoming data
    public Packet packetToRead;     // REMOTE: Current packet to read received data
    public Packet packetToWriteOn;  // LOCAL: Current packet to write on with local data

    public PacketManager(int bufferSize, MnetNetwork localMnetInstance, PacketType packetType, bool incomingPlayerPackets, int playerID)
    {
        //buffer = new MnetPacket[ServerSettings.packetBufferSize];
        //packetBuffer = new byte[ServerSettings.packetBufferSize][ServerSettings.maxPacketSize];
        //packetBuffer = new byte[ServerSettings.packetBufferSize][];
        local = localMnetInstance;
        packetBuffer = new Packet[bufferSize];
        for (int i = 0; i < packetBuffer.Length; i++)
        {
            packetBuffer[i] = new Packet(packetType);
            packetBuffer[i].debugIndex = i;
        }
        BufferType = packetType;
        // Link packets in buffer together
        for (int i = 0; i < packetBuffer.Length - 1; i++)
        {
            packetBuffer[i].nextPacket = packetBuffer[i + 1];
        }
        // Connect last packet to the first
        packetBuffer[packetBuffer.Length - 1].nextPacket = packetBuffer[0];
        // Skip index 0 for easier comparison
        int startIndex = 1;
        packetToWriteOn = packetBuffer[startIndex];
        packetToRead = packetBuffer[startIndex];
        incomingPacket = packetBuffer[startIndex];
        outgoingPacket = packetBuffer[startIndex];
        currentPacketNumber = startIndex;
        expectedPacketNumber = startIndex;
        currentTickNumber = 1;
        lastTickProcessed = 0;
        lastTickReceived = 0;
        this.incomingPlayerPackets = incomingPlayerPackets;
        if (incomingPlayerPackets)
        {
            validObjectIDs = new HashSet<int>();
            this.playerID = playerID;
            int indexOfPlayerObject = 1 + (playerID - 1) * Mnet.objectsPerPlayer;
            for (int i = 0; i < Mnet.objectsPerPlayer; i++)
            {
                validObjectIDs.Add(indexOfPlayerObject + i);
            }
        }
        //packetToWriteOn.WriteHeader(outgoingPacketNumber, owner.currentTickNumber, owner.deltaTime);
        incomingPacket.Clear();
        //regularPacketType = packetType;
    }

    public Packet GetPacket(int packetID)
    {
        return packetBuffer[packetID % packetBuffer.Length];
    }

    // Returns true if there is enough space for a write attempt
    // Returns false if a new packet was needed
    public void CheckSpaceLeftForWriting(int spaceRequired)
    {
        if (packetToWriteOn.SpaceRemaining < spaceRequired)
        {
            NextWritePacket();
        }
    }

    public void NextWritePacket()
    {
        Debug.Log(local + " HEADER TICK WROTE " + currentTickNumber);
        packetToWriteOn.WriteHeader(currentPacketNumber, currentTickNumber, local.deltaTime);
        packetToWriteOn.isNew = true;
        packetToWriteOn = packetToWriteOn.nextPacket;
        packetToWriteOn.Clear();
        currentPacketNumber++;
    }

    public void NextReadPacket()
    {
        packetToRead = packetToRead.nextPacket;
    }

    public void NextOutgoingPacket()
    {
        outgoingPacket = outgoingPacket.nextPacket;
        outgoingPacket.Clear();
    }

    public void InsertPacket(Packet receivedPacket)
    {
        int packetNumber = receivedPacket.GetPacketNumber();
        // We got the correct packet number so we can just insert the data and move on to the next one
        Debug.Log(local.name + " "+BufferType+" got packet#" + packetNumber + ", expected packet# " + expectedPacketNumber);
        Debug.Log("INSERTING WITH TICK " + receivedPacket.GetTickNumber());
        if (packetNumber == expectedPacketNumber)
        {
            incomingPacket.SwapData(receivedPacket);
            incomingPacket.isNew = true;

            // Find the next packet that is needed
            while (incomingPacket.isNew)
            {
                int tick = incomingPacket.GetTickNumber();
                if (lastTickReceived < tick)  lastTickReceived = tick;
                Debug.Log("TICK IN INSERTION " + lastTickReceived);
                incomingPacket = incomingPacket.nextPacket;
                expectedPacketNumber++;
            }

        }
        // We received newer packet than expected, so we will mark its number down and take the data if needed
        else if(packetNumber > expectedPacketNumber)
        {
            Packet wrongPacket = GetPacket(packetNumber);
            if (packetNumber > highestPacketNumberReceived)  highestPacketNumberReceived = packetNumber;

            if (!wrongPacket.isNew)
            {
                wrongPacket.SwapData(receivedPacket);
                wrongPacket.isNew = true;
            }
        }
    }

    public void WriteVariable(MnetVariable updatedVariable)
    {
        // 1. Check koko 3 versioo
        // 2. Kirjota headeri
        // 3. Kirjota dataa

        // Jos vari yli 10, ei mahtuis. Jos vari alle 10 ni voidaan jo siirt‰‰ uuteen ilma et menetet‰‰ hirveesti

        //Debug.Log("OBJ = "+updatedVariable.owner.instanceID+","+updatedVariable.variableName + " ID = " + updatedVariable.id);

        Debug.Log(local+" WRITING AT " + packetToWriteOn.currentLength);

        if (updatedVariable.sizeCategory != VariableSize.Splittable)
        {
            CheckSpaceLeftForWriting(Mnet.bytesReservedForInstanceID + Mnet.bytesReservedForVariableID + (int)updatedVariable.sizeCategory + updatedVariable.sizeInBytes);
            MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForInstanceID),updatedVariable.owner.instanceID);
            MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForVariableID), updatedVariable.variableID);
            //Debug.Log("OBJECTID: " + MnetTools.BytesToInteger(packetToWriteOn.Span(15,2)));
            //Debug.Log("PACKET: " + packetToWriteOn.GetPacketNumber() + ", TICK: " + packetToWriteOn.GetTickNumber()+ ", TIME: " + packetToWriteOn.GetDeltaTime() + ", SIZE: " + packetToWriteOn.GetSize());
            if (updatedVariable.sizeCategory == VariableSize.Limited)
            {
                MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes((int)updatedVariable.sizeCategory), updatedVariable.sizeInBytes);
            }
            updatedVariable.Serialize(packetToWriteOn.WriteBytes(updatedVariable.sizeInBytes));
        }
        else
        {
            CheckSpaceLeftForWriting(Mnet.minimumPacketSpaceNeededForWrite);
            MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForInstanceID), updatedVariable.owner.instanceID);
            MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForVariableID), updatedVariable.variableID);
            MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes((int)updatedVariable.sizeCategory), updatedVariable.sizeInBytes);

            // LOOP WRITE -> SWAP eli katotaan remaining -> kopioi -> jos j‰jell‰ -> uusi paketti
            // !!!! Currentlength ei p‰ivity ku data vaihtuu
            int bytesRemaining = updatedVariable.sizeInBytes;
            int writtenAmount;
            while (bytesRemaining > 0)
            {
                writtenAmount = updatedVariable.TryWriteSplittable(packetToWriteOn.AllAvailableBytes());
                bytesRemaining -= writtenAmount;
                packetToWriteOn.currentLength += writtenAmount;
                //Debug.Log("Written Amount " + writtenAmount);
                CheckSpaceLeftForWriting(Mnet.minimumPacketSpaceNeededForWrite);
            }
        }
    }

    public void ReadTick()
    {
        Debug.Log(local.name+" received: " + packetToRead.GetTickNumber() + "/" + currentTickNumber);
        if (packetToRead.GetTickNumber() == currentTickNumber)
        {
            Debug.Log(local.name + " " + BufferType + " currentTick = " + currentTickNumber + ", packetTick = " + packetToRead.GetTickNumber());
            while (packetToRead.GetTickNumber() == currentTickNumber)
            {
                // Toivottavasti oikee kohta
                packetToRead.PrepareForRead();
                // Check for empty packet
                if (packetToRead.BytesLeftToRead == 0)
                {
                    NextReadPacket();
                    continue;
                }
                bool checkTickNumber = false;
                Debug.Log(local + " read PACKET " + packetToRead.GetPacketNumber() + " LENGTH " + packetToRead.GetSize() + " WITH TICK " + packetToRead.GetTickNumber());
                Debug.Log("READ PACKET START POS " + packetToRead.readPosition);
                while (!checkTickNumber)
                {
                    checkTickNumber = ReadVariable();
                }
            }
            lastTickProcessed = currentTickNumber;
            currentTickNumber++;
        }

    }

    public bool ReadVariable()
    {
        // 1. Lue headeri
        // 2. Hae variaabeli
        // 3. Lue dataa kunnes valmis?
        
        int objInstanceID = MnetTools.BytesToInteger(packetToRead.ReadBytes(Mnet.bytesReservedForInstanceID));
        int variableID = MnetTools.BytesToInteger(packetToRead.ReadBytes(Mnet.bytesReservedForVariableID));

        // TƒHƒ BLOCKI MUT MITE TUNNISTAA ET ON PLAYER PACKETS JA OLLAAN SERVER?

        Debug.Log("INSTANCE: " + objInstanceID + ", VAR: " + variableID+", PACKET: "+packetToRead.GetPacketNumber()+", BYTES LEFT: "+packetToRead.BytesLeftToRead);
        // Check if we got a method call instead
        if (variableID == 0)
        {
            ReadMethodCall(objInstanceID);
        }
        else
        {
            NewObject test = local.GetActiveObject(objInstanceID);
            if(test == null )Debug.Log("NULL CHECK: "+objInstanceID);//+" VAR = "+ local.GetActiveObject(objInstanceID).GetVariable(variableID));
            MnetVariable receivedVar = local.GetActiveObject(objInstanceID).GetVariable(variableID);

            // Get size of the updated value if necessary
            if (receivedVar.sizeCategory > VariableSize.Static)
            {
                receivedVar.sizeInBytes = MnetTools.BytesToInteger(packetToRead.ReadBytes((int)receivedVar.sizeCategory));
            }

            // If the value was not split, we can immediately deserialize it from the bytes
            // T‰‰ olis ehk‰ seonnus jos olis viimiset tavut ollu just varin koon verra ni lis‰sin =  eli oli <, nyt <=
            if (receivedVar.sizeInBytes <= packetToRead.BytesLeftToRead)
            {
                if (!incomingPlayerPackets || validObjectIDs.Contains(objInstanceID))
                {
                    receivedVar.Deserialize(packetToRead.ReadBytes(receivedVar.sizeInBytes));
                }
                else
                {
                    Debug.LogError("PLAYER " + playerID + " SENT FALSE DATA FOR OBJECT " + local.worldObjectInstances[objInstanceID]);
                    // Skip the bytes
                    packetToRead.currentLength += receivedVar.sizeInBytes;
                }
            }
            // The received value is split into multiple packets
            else
            {
                // Since this variable is split between multiple packets, we need to track the reading process
                int splitBytesLeftToRead = receivedVar.sizeInBytes;
                int readAmount;
                while (splitBytesLeftToRead > 0)
                {
                    // Remaining bytes might be the whole packet or a smaller segment so we need to check which is the case
                    readAmount = Math.Min(splitBytesLeftToRead, packetToRead.BytesLeftToRead);
                    if (!incomingPlayerPackets || validObjectIDs.Contains(objInstanceID))
                    {
                        receivedVar.TryReadSplittable(packetToRead.ReadBytes(readAmount));
                    }
                    else
                    {
                        Debug.LogError("PLAYER " + playerID + " SENT FALSE DATA FOR OBJECT " + local.worldObjectInstances[objInstanceID]);
                        // Skip the bytes
                        packetToRead.currentLength += readAmount;
                    }

                    splitBytesLeftToRead -= readAmount;
                    // Check if we are done or if we need another packet for the variable. No need to check the tick, we know this is still part of same update.
                    if (splitBytesLeftToRead != 0)  NextReadPacket();
                }
                Debug.Log("READ DONE. PACKET: " + packetToRead.GetPacketNumber() + ", LENGTH: " + packetToRead.BytesLeftToRead);
            }
        }

        // Check if we have reached the end of the packet
        if(packetToRead.BytesLeftToRead == 0)
        {
            Debug.Log(local.name+" done reading packet.");
            NextReadPacket();
            return true;
        }
        return false;

    }

    public void WriteMethodCall(int objectID, int methodID)
    {
        // Object Instance ID + Method Identifier (value 0) + Method ID
        // 0 = Method Call, 1... = Variables
        Debug.Log("WRITING METHOD ON " + local.worldObjectInstances[objectID]);
        CheckSpaceLeftForWriting(Mnet.bytesReservedForInstanceID + 1 + Mnet.bytesReservedForMethodID);

        MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForInstanceID), objectID);
        MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(1), 0);
        MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForMethodID), methodID);
    }

    public void ReadMethodCall(int objectInstanceID)
    {
        Debug.Log("INSTANCE " + objectInstanceID+" IS "+local.GetActiveObject(objectInstanceID));

        if (!incomingPlayerPackets || validObjectIDs.Contains(objectInstanceID))
        {
            local.GetActiveObject(objectInstanceID).InvokeMethod(MnetTools.BytesToInteger(packetToRead.ReadBytes(Mnet.bytesReservedForMethodID)));
        }
        else
        {
            Debug.LogError("PLAYER " + playerID + " SENT FALSE DATA FOR OBJECT " + local.worldObjectInstances[objectInstanceID]);
            // Skip the bytes
            packetToRead.currentLength += Mnet.bytesReservedForMethodID;
        }

    }



    // !!!- Pakettien vastaanottaminen ja l‰hetys ei oo koskaan n‰in yksinkertasta niin pit‰‰ hoitaa core luokan puolella -!!!

    /*
    public Span<byte> ReceivePacket()
    {
        Packet receivePacket = incomingPacket;
        incomingPacket = incomingPacket.nextPacket;
        incomingPacketNumber++;
        return receivePacket.EmptyPacket();
    }
    

    public Span<byte> SendPacket()
    {
        Packet sendPacket = outgoingPacket;
        outgoingPacket = outgoingPacket.nextPacket;
        outgoingPacketNumber++;
        return sendPacket.Data;
    }

    */


    /*
    public int GetPacketNumber()
    {
        return MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerPacketNumberPosition, Mnet.headerPacketNumberLength));
    }

    public void SetPacketNumber(int packetNumber)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerPacketNumberPosition, Mnet.headerPacketNumberLength), packetNumber);
    }

    public int GetTickNumber()
    {
        return MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerTickNumberPosition, Mnet.headerTickNumberLength));
        //, Mnet.headerTickNumberLength);
    }

    public void SetTickNumber(int tickNumber)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerTickNumberPosition, Mnet.headerTickNumberLength), tickNumber);
    }

    public void UpdatePacketSize()
    {

        currentLength = MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerSizePosition, Mnet.headerSizeLength));
        //, Mnet.headerSizeLength);
        //get { return BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(MnetSettings.headerSizePosition, 2)); }
    }

    public void SetPacketSize(int packetSize)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerSizePosition, Mnet.headerSizeLength), packetSize);
    }

    public float GetDeltaTime()
    {
        return MnetTools.BytesToFloat(_data.AsSpan(Mnet.headerDeltaTimePosition, Mnet.headerDeltaTimeLength));
    }

    public void SetDeltaTime(float deltaTime)
    {
        MnetTools.FloatToBytes(_data.AsSpan(Mnet.headerDeltaTimePosition, Mnet.headerDeltaTimeLength), deltaTime);
    }

    public int GetSequenceNumber()
    {
        return MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerSequencePosition, Mnet.headerSequenceLength));
        //, Mnet.headerSequenceLength);
    }
    public void SetSequenceNumber(int sequenceNumber)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerSequencePosition, Mnet.headerSequenceLength), sequenceNumber);
    }

    public int GetTotalPacketsInUpdate()
    {
        return MnetTools.BytesToInteger(_data.AsSpan(Mnet.headerPacketCountPosition, Mnet.headerPacketCountLength));
        //, Mnet.headerPacketCountLength);
    }

    public void SetTotalPacketsInUpdate(int packetCount)
    {
        MnetTools.IntegerToBytes(_data.AsSpan(Mnet.headerPacketCountPosition, Mnet.headerPacketCountLength), packetCount);
    }

    public int GetObjectID()
    {
        return MnetTools.BytesToInteger(Read(Mnet.bytesReservedForObjectID));//_data.AsSpan(readPos, Mnet.bytesReservedForObjectID));//, Mnet.bytesReservedForObjectID);
                                                                             //objectSize = MnetTools.BytesToInteger(Read(Mnet.bytesReservedForSegmentSize));//_data.AsSpan(readPos + Mnet.bytesReservedForObjectID,
                                                                             //Mnet.bytesReservedForSegmentSize));//, Mnet.bytesReservedForSegmentSize);
                                                                             //Debug.Log("INFO IS: ID " + objectID + " SIZE " + objectSize);
    }


    
    public MnetPacket(bool reserveSpaceForFullHeader)
    {
        _data = new byte[Mnet.maxPacketDataSize];
        headerLength = reserveSpaceForFullHeader ? Mnet.headerCombinedLength : 0;
        //      Reset();
    }


    public void Reset()
    {
        Array.Clear(_data, 0, headerLength);    // Onko pakollinen? Eikˆs vastaanotossa aina ylikirjoteta kaikki?
        currentLength = headerLength;
        readPosition = 0;
        isFull = false;
        //      extraPacketsInUpdate = 0;
    }

    public string ReadMessage()
    {
        readPosition += Mnet.messageLength;
        return System.Text.Encoding.ASCII.GetString(_data.AsSpan(readPosition - Mnet.messageLength, Mnet.messageLength));
    }

    public void WriteMessage(string message)
    {
        if (currentLength < headerLength) currentLength = headerLength;
        System.Text.Encoding.ASCII.GetBytes(message, _data.AsSpan(currentLength, Mnet.messageLength));
        currentLength += Mnet.messageLength;
    }


    public void SetupServerPacket()
    {
        // Aseta length etc
    }

    // public void InitClientPacket()
    // public void SetupClientPacket()

    // !!! OLIKO Tƒƒ VAI SWAP KƒYT÷S WHAT!   Copies the data portion from another packet.
    public void CopyDataFrom(MnetPacket packetWithNewData)
    {
        currentLength = packetWithNewData.currentLength;
        packetWithNewData.DataOnly().CopyTo(DataOnly());
    }


    // Returns the available space in the packet as a span
    public Span<byte> AvailableSpace()
    {
        // Get remaining space on the buffer
        return _data.AsSpan(currentLength, Mnet.maxPacketDataSize - currentLength);//, ServerSettings.maxPacketSize - currentLength);
    }


    // Returns a custom slice of the packet. If no length is specified, will give all the data from the start point to the end
    public Span<byte> Span(int start = 0, int length = 0)
    {
        //Debug.Log(nimi+" ASKED FOR " + length + " BYTES STARTING AT " + start+" WITH REMAINDING SPACE "+(MnetSettings.maxPacketDataSize-currentLength));
        if (length == 0) length = Mnet.maxPacketDataSize - start;
        return _data.AsSpan(start, length);
    }

    public Span<byte> Write(int writeSegmentLength)
    {
        currentLength += writeSegmentLength;
        return _data.AsSpan(currentLength - writeSegmentLength, writeSegmentLength);
    }

    public Span<byte> Read(int readSegmentLength)
    {
        readPosition += readSegmentLength;
        return _data.AsSpan(readPosition - readSegmentLength, readSegmentLength);
    }

    public void WriteSingleByte(byte newByte)
    {
        _data[currentLength++] = newByte;
    }

    public byte ReadSingleByte()
    {
        return _data[readPosition++];
    }

    // Returns the packet in its current form. This is the one we want to send through sockets
    public Span<byte> CurrentMessage()
    {
        return _data.AsSpan(0, currentLength);
    }

    // Returns the entire array as a span
    public Span<byte> AllBytes()
    {
        return _data.AsSpan();
    }

    // Returns only the header part of the packet
    public Span<byte> HeaderOnly()
    {
        return _data.AsSpan(0, headerLength);
    }

    // Returns only the data portion of the packet, leaving out the packet's header
    public Span<byte> DataOnly()
    {
        return _data.AsSpan(headerLength, currentLength - headerLength);
    }


    // Swap the data between two packets. Since _data is private, we have to use two methods to finish the process
    public void SwapData(MnetPacket otherPacket)
    {
        byte[] tempPointer = otherPacket.Data;
        otherPacket.OverrideData(this);
        _data = tempPointer;

    }

    // Since _data is private, we have to use this method to gain access to change the value
    public void OverrideData(MnetPacket overridingPacket)
    {
        _data = overridingPacket.Data;
    }
    */
}
