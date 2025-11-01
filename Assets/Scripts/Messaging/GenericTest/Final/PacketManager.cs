using System;
using UnityEngine;

public class PacketManager
{

    //public int writePosition; Ei tarvita koska headerin osat vaihdetaan suoraan ja dataa ku kirjotetaan ni kirjotetaan vaan aina kerran ja olemassa olevan perään
    //public int packetNumber;
    //public bool isActive;
    //public bool isFull;
    //public int tickNumber;
    public int outgoingPacketNumber;
    public int incomingPacketNumber;
    //public bool isCircularBuffer;

    private MnetNetwork local;

    public Packet[] packetBuffer;

    public Packet outgoingPacket;   // LOCAL: Packet to be send with local data
    public Packet incomingPacket;   // REMOTE: Packet to receive incoming data
    public Packet packetToRead;     // REMOTE: Current packet to read received data
    public Packet packetToWriteOn;  // LOCAL: Current packet to write on with local data

    public PacketManager(int bufferSize, MnetNetwork localMnetInstance)
    {
        //buffer = new MnetPacket[ServerSettings.packetBufferSize];
        //packetBuffer = new byte[ServerSettings.packetBufferSize][ServerSettings.maxPacketSize];
        //packetBuffer = new byte[ServerSettings.packetBufferSize][];
        local = localMnetInstance;
        packetBuffer = new Packet[bufferSize];
        for (int i = 0; i < packetBuffer.Length; i++)
        {
            packetBuffer[i] = new Packet();
        }
        // Link packets in buffer together
        for (int i = 0; i < packetBuffer.Length - 1; i++)
        {
            packetBuffer[i].nextPacket = packetBuffer[i + 1];
        }
        // Connect last packet to the first
        packetBuffer[packetBuffer.Length - 1].nextPacket = packetBuffer[0];
        packetToWriteOn = packetBuffer[0];
        packetToRead = packetBuffer[0];
        incomingPacket = packetBuffer[0];
        outgoingPacket = packetBuffer[0];
        //packetToWriteOn.WriteHeader(outgoingPacketNumber, owner.currentTickNumber, owner.deltaTime);
        incomingPacket.readPosition = Mnet.headerCombinedLength;
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
        packetToWriteOn.ClosePacket();
        packetToWriteOn = packetToWriteOn.nextPacket;
        outgoingPacketNumber++;
        Debug.Log("LAST PACKET " + outgoingPacketNumber);
        packetToWriteOn.WriteHeader(outgoingPacketNumber, local.currentTickNumber, local.deltaTime);
    }

    public void NextReadPacket()
    {
        packetToRead = packetToRead.nextPacket;
        packetToRead.readPosition = Mnet.headerCombinedLength;
        packetToRead.currentLength = packetToRead.GetSize();
    }

    public void WriteVariable(MnetVariable updatedVariable)
    {
        // 1. Check koko 3 versioo
        // 2. Kirjota headeri
        // 3. Kirjota dataa

        // Jos vari yli 10, ei mahtuis. Jos vari alle 10 ni voidaan jo siirtää uuteen ilma et menetetää hirveesti

        //Debug.Log("OBJ = "+updatedVariable.owner.instanceID+","+updatedVariable.variableName + " ID = " + updatedVariable.id);

        if (updatedVariable.sizeCategory != VariableSize.Splittable)
        {
            CheckSpaceLeftForWriting(Mnet.bytesReservedForObjectID + Mnet.bytesReservedForVariableID + (int)updatedVariable.sizeCategory + updatedVariable.sizeInBytes);
            MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForInstanceID),updatedVariable.owner.instanceID);
            MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForVariableID), updatedVariable.id);
            //Debug.Log("OBJECTID: " + MnetTools.BytesToInteger(packetToWriteOn.Span(15,2)));
            Debug.Log("PACKET: " + packetToWriteOn.GetPacketNumber() + ", TICK: " + packetToWriteOn.GetTickNumber()
    + ", TIME: " + packetToWriteOn.GetDeltaTime() + ", SIZE: " + packetToWriteOn.GetSize());
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
            MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForVariableID), updatedVariable.id);
            MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes((int)updatedVariable.sizeCategory), updatedVariable.sizeInBytes);

            // LOOP WRITE -> SWAP eli katotaan remaining -> kopioi -> jos jäjellä -> uusi paketti
            // !!!! Currentlength ei päivity ku data vaihtuu
            int bytesRemaining = updatedVariable.sizeInBytes;
            int writtenAmount;
            while (bytesRemaining > 0)
            {
                writtenAmount = updatedVariable.TryWriteSplittable(packetToWriteOn.AllAvailableBytes());
                bytesRemaining -= writtenAmount;
                packetToWriteOn.currentLength += writtenAmount;
                Debug.Log("Written Amount " + writtenAmount);
                CheckSpaceLeftForWriting(Mnet.minimumPacketSpaceNeededForWrite);
            }
        }
    }

    public void ReadTick(int currenTickNumber)
    {
        while(packetToRead.GetTickNumber() == currenTickNumber)
        {
            bool checkTickNumber = false;
            while(!checkTickNumber)
            {
                checkTickNumber = ReadVariable();
            }
        }
    }

    public bool ReadVariable()
    {
        // 1. Lue headeri
        // 2. Hae variaabeli
        // 3. Lue dataa kunnes valmis?

        
        int objInstanceID = MnetTools.BytesToInteger(packetToRead.ReadBytes(Mnet.bytesReservedForInstanceID));
        int variableID = MnetTools.BytesToInteger(packetToRead.ReadBytes(Mnet.bytesReservedForVariableID));
        Debug.Log("OBJ: " + objInstanceID + ", VAR: " + variableID+", PACKET: "+packetToRead.GetPacketNumber()+", LENGTH: "+packetToRead.BytesLeftToRead);
        // Check if we got a method call instead
        if (variableID == 0)
        {
            ReadMethodCall(objInstanceID);
        }
        else
        {
            MnetVariable receivedVar = local.GetObject(objInstanceID).GetVariable(variableID);

            // Get size of the updated value if necessary
            if (receivedVar.sizeCategory > VariableSize.Static)
            {
                receivedVar.sizeInBytes = MnetTools.BytesToInteger(packetToRead.ReadBytes((int)receivedVar.sizeCategory));
            }

            // If the value was not split, we can immediately deserialize it from the bytes
            if (receivedVar.sizeInBytes < packetToRead.BytesLeftToRead)
            {
                receivedVar.Deserialize(packetToRead.ReadBytes(receivedVar.sizeInBytes));
            }
            // The received value is split into multiple packets
            else
            {
                // Since this variable is split between multiple packets, we need to track the reading process
                int bytesLeftToRead = receivedVar.sizeInBytes;
                int readAmount;
                while (bytesLeftToRead > 0)
                {
                    // Remaining bytes might be the whole packet or a smaller segment so we need to check which is the case
                    readAmount = Math.Min(bytesLeftToRead, packetToRead.BytesLeftToRead);
                    receivedVar.TryReadSplittable(packetToRead.ReadBytes(readAmount));
                    bytesLeftToRead -= readAmount;
                    // Check if we are done or if we need another packet for the variable
                    if (bytesLeftToRead != 0)  NextReadPacket();
                }
                Debug.Log("READ DONE. PACKET: " + packetToRead.GetPacketNumber() + ", LENGTH: " + packetToRead.BytesLeftToRead);
            }
        }

        // Check if we have reached the end of the packet
        if(packetToRead.BytesLeftToRead == 0)
        {
            NextReadPacket();
            return true;
        }
        return false;

    }

    public void WriteMethodCall(int objectID, int methodID)
    {
        // Object Instance ID + Method Identifier (value 0) + Method ID
        // 0 = Method Call, 1... = Variables
        CheckSpaceLeftForWriting(Mnet.bytesReservedForInstanceID + 1 + Mnet.bytesReservedForMethodID);

        MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForInstanceID), objectID);
        MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(1), 0);
        MnetTools.IntegerToBytes(packetToWriteOn.WriteBytes(Mnet.bytesReservedForMethodID), methodID);
    }

    public void ReadMethodCall(int objectInstanceID)
    {
        local.GetObject(objectInstanceID).InvokeMethod(MnetTools.BytesToInteger(incomingPacket.ReadBytes(Mnet.bytesReservedForMethodID)));
    }



    // !!!- Pakettien vastaanottaminen ja lähetys ei oo koskaan näin yksinkertasta niin pitää hoitaa core luokan puolella -!!!

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
        Array.Clear(_data, 0, headerLength);    // Onko pakollinen? Eikös vastaanotossa aina ylikirjoteta kaikki?
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

    // !!! OLIKO TÄÄ VAI SWAP KÄYTÖS WHAT!   Copies the data portion from another packet.
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
