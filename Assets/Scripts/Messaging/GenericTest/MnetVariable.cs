using System;
using UnityEngine;

// This is some mad scientist stuff, but we can store the header length AS the category value and use it to increase an objects size accurately
public enum VariableSize
{
    // These values mark how many bytes are needed to store the size of the variable if it can change between ticks
    Static = 0, // Static variables have no need to store the size between changes so this should ALWAYS be 0
    Limited = 1, // Limited size is set to 1 (max 255 bytes) but if increased, the splittable size will need to increase to 3 or more
    Splittable = 2, // Splittable items can be over the max packet size so 2 or more always
    //DoNotUseSplitAutoDetection = MnetSettings.variableMultipartHeaderLength   // + MnetSettings.variableDividableHeaderAdjustment
}
[Serializable]
public abstract class MnetVariable
{
    //[Tooltip("The mode in which the item can be set. (same as the object, server, client, shared)")]
    //public MessagingDirection itemMessagingMode;
    //[Tooltip("Ignore older versions of item, if packets arrive out of order (e.g. object position)")]
    //public bool ignoreOlder;
    //[Tooltip("If item changes arrives out of order, perform reconciliation")]
    //public bool reconciliation;
    /*
    [Tooltip("The byte length is not constant and can change (e.g. strings or arrays)")]
    public bool varyingSize;
    [Tooltip("The item can be split over multiple segments and even packets")]
    public bool canBeSplit;
    */

    //Size category of the item.
    //Static: Stays the same every update.
    //Varies: Can change between updates, but is limited to the max size of the segment.
    //Dividable: Size varies and its total size can exceed the segment size,
    // meaning it can arrive in multiple parts in different segments and packets.
    [Tooltip("Size category of the variable. \n" +
        "STATIC: Does not change between updates and is limited by the segment size (E.g. 32-bit integer is always 4 bytes).\n" +
        "LIMITED: Byte size can vary between updates, but is limited by the segment size (E.g. A string for chat messages with a 120 character limit).\n" +
        "SPLITTABLE: Varying size that is not limited by the segment size and can be sent in pieces, even in multiple packets" +
        " (E.g. Items that are too large to fit in a single segment or might grow past the segment limit).")]
    public VariableSize sizeCategory;
    [Tooltip("Size of the item as bytes. Set once in setup or leave empty if it changes between updates.")]
    public int sizeInBytes = 0;  // short
    [HideInInspector]
    public bool hasChanged;
    //[HideInInspector]
    //public byte flagIndex = 0;  // ?
    //[HideInInspector]
    //public byte orderIndex = 0; // ?
    [HideInInspector]
    public byte[] bytes;    // Currently only used to store bytes if the item is split
    //private bool storedBytes = false;
    private int splitWritePos;   // WHAT DIS?
    private int splitReadPos;
    private int splitBytesRemaining;   // Stores the amount of needed bytes of a split item
    //       !!!! DEBUG POISTA KU VALMIS !!!!!!!!
    [HideInInspector]
    public string variableName;
    /*
    [HideInInspector]
    public byte flagIndex;
    [HideInInspector]
    public byte flagValue;
    */
    [HideInInspector]
    public WANHAMnetObject WANHAowner;

    [HideInInspector]
    public NewObject owner;
    [HideInInspector]
    public int variableID;
    [HideInInspector]
    public PacketManager packetManager;

    public abstract void Serialize(Span<byte> reservedBytes);

    public abstract void Deserialize(Span<byte> receivedBytes);

    // !! !! MITES COUNT LUETAAN? SE ETTÄ OBJEKTI VOI ANTAA TAVUT VAATII ETTÄ TIETÄÄ KOON? ELI OBJEKTI TUNNISTAA ETTÄ ON JAETTTU
    // JA SEN PERIAATTEEL ANTAA OIKEEN MÄÄRÄN DATAA JA TIEDOT ELI TOTAL SIZE JA START POS
    // TOTALI PITÄÄ OTTAA KOSKA OBJEKTI TARVII SEN MUTTA STARTPOS JA MÄÄRÄ VOIDAA KATTOO TÄÄLLÄ

    public void UpdateVariable()
    {
        if (sizeCategory != VariableSize.Static) SetSize();
        owner.local.activeManager.WriteVariable(this);
    }

    /*
    public void SnapshotUpdate(PacketManager snapshotManager)
    {
        if (sizeCategory != VariableSize.Static) SetSize();
        snapshotManager.WriteVariable(this);
    }
    */

    public void PrepareForSplitWrite()
    {
        // Check if local byte array can hold the current value or create the array if this is the first time
        if (bytes.Length < sizeInBytes || bytes == null)
        {
            bytes = new byte[sizeInBytes];
        }
        // We need these numbers to keep track of the write progress
        splitWritePos = 0;
        splitBytesRemaining = sizeInBytes;
        // Convert to bytes
        Serialize(bytes.AsSpan(0,sizeInBytes));
    }

    public void PrepareForSplitRead()
    {
        // Check if local byte array can hold the incoming value or create the array if this is the first time
        if (bytes.Length < sizeInBytes || bytes == null)
        {
            bytes = new byte[sizeInBytes];
        }
        // We need to track bytes that have already been copied
        splitReadPos = 0;
        splitBytesRemaining = sizeInBytes;
    }

    // Return value is the amount of bytes written. PacketManager needs to keep track of how many bytes are written and when to change packets
    public int TryWriteSplittable(Span<byte> bytesLeftInPacket)
    {
        // Check if this is the first write attempt on this update
        if (splitBytesRemaining == 0)
        {
            // Check if the value can be written without splitting
            if (bytesLeftInPacket.Length >= sizeInBytes)
            {
                Serialize(bytesLeftInPacket);
                return sizeInBytes;
            }
            else
            {
                // We need to prepare the item by converting it to bytes
                PrepareForSplitWrite();
            }
        }

        // The amount we write is limited either by the available space or the amount of bytes left
        int writeAmount = Math.Min(bytesLeftInPacket.Length, sizeInBytes - splitWritePos);



        bytes.AsSpan(splitWritePos,writeAmount).CopyTo(bytesLeftInPacket);
        /*
        for (int i = 0; i < writeAmount; i++)
        {
            Debug.Log("BYTE " + (splitWritePos + i) + ": " + bytes[splitWritePos + i]);
        }
        */
        splitWritePos += writeAmount;

        /*
        // Copy bytes one by one to packet
        for (int i = 0; i < writeAmount; i++)
        {
            bytesLeftInPacket[i] = bytes[splitWritePos++];
        }
        */
        splitBytesRemaining -= writeAmount;

        return writeAmount;
    }

    public void TryReadSplittable(Span<byte> bytesLeftInPacket)
    {
        
        // 1. Tunnista eka luku (preparation)
        // 2. Lue dataa ja palauta tarviiko lisää kakkua. HEADER PITÄÄ MANAGERIN SKIPATA? TARVITAANKO HEADERI JÄLKIMMÄISIS?
        
        // If true, it means that this will be the first read attempt so we need to prepare for the process
        if(splitBytesRemaining == 0)
        {
            PrepareForSplitRead();
        }
        
        // Amount to read is either remainder of the variable or rest of the packet
        //int readAmount = Math.Min(bytesLeftInPacket.Length, splitBytesRemaining);

        /*
        for(int i = 0;i < readAmount; i++)
        {
            bytes[splitReadPos++] = bytesLeftInPacket[i];
        }
        */
        bytesLeftInPacket.CopyTo(bytes.AsSpan(splitReadPos));
        /*
        for (int i = 0; i < readAmount; i++)
        {
            Debug.Log("R_BYTE " + (splitReadPos+i) + ": " + bytes[splitReadPos + i]);
        }
        */
        splitReadPos += bytesLeftInPacket.Length;
        splitBytesRemaining -= bytesLeftInPacket.Length;

        // Check if we are finished and can deserialize, or if we need to continue on to another packet
        if(splitBytesRemaining == 0)
        {            
            Deserialize(bytes.AsSpan(0,sizeInBytes));
        }

    }
    /*
    public void WANHAReadSplitSegment(Packet packet)//Span<byte> incomingBytes)
    {
        // !!!! Täältä kai puuttuu mun mahtava idea
        // että splitti voi tulla yhdessä erässä eli tsekkaa bit ja tee kerralla. Voiko vaan oikasta deserialize kautta

        // 
        //int isNotSegmented = incomingBytes[ServerSettings.bytesReservedForSplitItemMaxSize - 1];
        //int totalSize = BinaryPrimitives.ReadInt16LittleEndian(incomingBytes);

        // When writing a variable that can be split, we use the sign as a marker to notify if the variable actually is not split
        // This allows us to save couple of bytes when they are not needed and spend less time processing
        // and in the end works basically like a varyinSize update (size header + data)

        //int totalSize = MnetTools.BytesToInteger(incomingBytes.Slice(0, Mnet.bytesReservedForSplitItemSize));
        int totalSize = MnetTools.BytesToInteger(packet.Read(Mnet.bytesReservedForSplitItemSize));

        // If the sign bit was toggled on, it means that this item was not split on this update so we can process it immediately
        if (totalSize < 0)
        {
            Debug.Log("Found negative!");
            totalSize *= -1;
            Debug.Log("SIZE IS " + totalSize);
            Deserialize(packet);//incomingBytes.Slice(Mnet.bytesReservedForSplitItemSize,totalSize));
            // Return the amount processed, so the size bytes and the actual data byte count
            return;
            //return Mnet.bytesReservedForSplitItemSize + totalSize;
        }

        // If this is the first time this split variable is processed, mark down the size and increase the byte array if needed
        if (splitBytesRemaining == 0)
        {
            splitBytesRemaining = totalSize;
            // If the item is larger than the reserved
            if (bytes.Length < totalSize)
            {
                bytes = new byte[totalSize];
            }
        }

        // !!! Pitäis olla oikein. TAvut talletetaan keskeltä (startPos) ja sourcessa on headeri offsettinä. Pitäis ollakki ok, congratulationssi
        
        //int readPos = Mnet.bytesReservedForSplitItemSize;
        // Skip first bytes of totalSize and take startPos. It should always have the same byte count as totalSize
        //int startPos = MnetTools.BytesToInteger(incomingBytes.Slice(readPos, Mnet.bytesReservedForSplitItemSize));
        int startPos = MnetTools.BytesToInteger(packet.Read(Mnet.bytesReservedForSplitItemSize));
        //readPos += Mnet.bytesReservedForSplitItemSize;
        // Split variables are still limited by the size of a segment in packet so we use that limit for the actual byte count of the data
        //int variableSegmentSize = MnetTools.BytesToInteger(incomingBytes.Slice(readPos,Mnet.bytesReservedForSegmentSize));
        int receivedAmount = MnetTools.BytesToInteger(packet.Read(Mnet.bytesReservedForSplitItemSize));
        //readPos += Mnet.bytesReservedForSegmentSize;
        // Copy data to the byte array and substract the number processed from the remaining amount
        for (int i = 0; i < receivedAmount; i++)
        {
            bytes[startPos + i] = packet.ReadSingleByte();//[packet.readPosition++];//incomingBytes[i+readPos];
        }
        splitBytesRemaining -= receivedAmount;

        // If remaining data amount is zero, we should be finished and have all the bytes the variable needs so we can now deserialize it
        if(splitBytesRemaining == 0)
        {
            Debug.Log("GOT ALL PIECES SO WE CAN READ WHOLE THING");
            Deserialize(packet); //bytes.AsSpan(0, totalSize));
        }

        // Split read and write methods require that the item returns the amount of bytes processed to the object,
        // so the header and data in this case
        //return Mnet.variableMultipartHeaderLength + variableSegmentSize;
    }

    public bool WANHAWriteSplitSegment(Packet packet)
    {
            //Debug.Log("SPACE REMAINING IN SPLIT " + spaceRemaining);
        // Aika paljo juttui mitä tarvitaan on vaan object puolella hmmmMMMMmmmmm



        // UPDATE SIZE ?
        // CHECK IF CAN FIT WITHOUT SPLITTING
        // IF SPLITTING, REDUCE SPACE REMAINING BY HEADER AND PUSH IN AS MUCH POSSIBLE
        // SET HEADER AND RETURN WRITTEN + HEADER
        // Mahdolliset askeleet: Menee kerralla. Eka split. Keski split. Final split.


        // Okei onko tää ny sitä varte et jos ei oo splitattu vai final? Vähä sekava. Kato uusiks mut logiikka pitäis olla selkee

        // If the update is small enough on this item, we don't need to split


        // If this value is zero, it means that this is the first write attempt for this item in this update
        if (splitBytesRemaining == 0)
        {

            //SetSize();

            // If we can fit whole item on the first write attempt, there is no need to split it
            if (Mnet.bytesReservedForSplitItemSize + sizeInBytes <= packet.SpaceRemaining)//spaceRemaining) //(sizeInBytes + MnetSettings.bytesReservedForSplitItemSize) <= spaceRemaining) tää on ny VariableType puolella
            {

                // Recipient needs to identify splittable items that are not split and we can use the sign bit on the size for that.
                // Multiplying the size with a negative one is a simple operation and can be also used to turn it into usable form when read
                MnetTools.IntegerToBytes(packet.Write(Mnet.bytesReservedForSplitItemSize), (sizeInBytes * -1 ));
                // Instead of using the bytes array, we can serialize directly into the outgoing bytes. We leave space for the size on the slice
                Serialize(packet);
                hasChanged = false;
                // We need to return the whole amount of written bytes, so the size of the header and the data combined
                return false;
                //return Mnet.bytesReservedForSplitItemSize + sizeInBytes;
            }
            // This first step is mandatory when item is going to be send in pieces
            else
            {
                // We will use the byte array belonging to this variable to hold the data until it can all be send.
                // If the array cannot hold it because it is too small, we need to increase its size
                if(bytes.Length < sizeInBytes)
                {
                    bytes = new byte[sizeInBytes];
                }
                // Serialize the value into stored bytes so we can send it in pieces
                Serialize(packet);
                // We need to track how much we have left to send, so set up the initial value
                splitBytesRemaining = sizeInBytes;                
            }
        }

        //int writePos = 0;
        int writeAmount = 0;

        // Account of the space taken by the header
        //spaceRemaining -= Mnet.variableMultipartHeaderLength;

        //todo // Tarvii ehkä talletaa erikseen alotus kohta jos sitä ei voi laskea splitBlockBytesRemainingilla ja bytes (miinus?)
        //int startPos = splitBlockBytesRemaining ??? // vastakkainen hmmm

        // Fill out the header data
        MnetTools.IntegerToBytes(packet.Write(Mnet.bytesReservedForSplitItemSize), sizeInBytes);        // Total size of item
        MnetTools.IntegerToBytes(packet.Write(Mnet.bytesReservedForSplitItemSize), splitWritePos); // Start position
        MnetTools.IntegerToBytes(packet.Write(Mnet.bytesReservedForSplitItemSize), writeAmount);          // Amount of bytes

        // The amount that we will write is limited by the space available or by the amount we have left to write
        writeAmount = Math.Min(packet.SpaceRemaining, splitBytesRemaining);

        int writePos = packet.currentLength;
        // Copy as much bytes into the outgoing span as we can
        for (int i = 0; i < writeAmount; i++)
        {
            packet[writePos + i] = bytes[splitWritePos + i];
            //outgoingBytes[writePos + i] = bytes[splitBlockWritePos + i];
        }

        packet.currentLength += writeAmount;


        // Remove the amount written from the total
        splitBytesRemaining -= writeAmount;
        // Move the write position forward by the same amount
        splitWritePos += writeAmount;

        // If we have finished writing, we will mark the variable as done and reset the write position
        if (splitBytesRemaining == 0)
        {
            hasChanged = false;
            splitWritePos = 0;
        }
        

        // We return the amount of space that was used, meaning the header bytes and the actual data bytes
        //return Mnet.variableMultipartHeaderLength + writeAmount;
    }
        
    // PITÄÄ AINAKI TESTATES OLLA ERIKSEEN (EHKÄ MUUTENKI HYVÄ VARAL) ET VOI KUTSUA MILLO VAAN
    public void UpdateSize()
    {
        if (sizeCategory != VariableSize.Static)
        {
            SetSize();


        }
        // Add the size of the item and possibly the header to objects total size
            //Debug.Log("ITEM SIZE OF "+variableName + " IS " + sizeInBytes);


        //owner.currentSize += sizeInBytes;
        //hasChanged = true;

    }
*/
    // Used to setup the variable before use. Set the category of the item here and the bytes needed if the size is static.
    // E.g. MnetInt only needs the byte size of the 32-bit integer and to be set static:
    // sizeInBytes = 4;
    // sizeCategory = VariableSize.Static;
    public abstract void Setup();

    // This method is used by variables that do not have a static size. It needs to calculate the byte size of the data.
    // E.g. String in Unicode format is two bytes per character, so the size calculation was simply: sizeInBytes = Value.Length * 2
    public abstract void SetSize();
}
