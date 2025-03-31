using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is some mad scientist stuff, but we can store the header length AS the category value and use it to increase an objects size accurately
public enum VariableSize
{
    Static = 0,
    Limited = MnetSettings.variableVaryingHeaderLength,
    Splittable = MnetSettings.bytesReservedForSplitItemSize, // + MnetSettings.variableDividableHeaderAdjustment,
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
    [Tooltip("Size of the item as bytes. Set once in setup or changes between updates.")]
    public int sizeInBytes = 0;  // short
    [HideInInspector]
    public bool hasChanged;
    //[HideInInspector]
    //public byte flagIndex = 0;  // ?
    //[HideInInspector]
    //public byte orderIndex = 0; // ?
    [HideInInspector]
    public byte[] bytes;    // Currently only used to store bytes if the item is split
    private int splitBlockWritePos;   // WHAT DIS?
    private int splitBlockBytesRemaining;   // Stores the amount of needed bytes of a split item
    // !!!! DEBUG POISTA KU VALMIS
    [HideInInspector]
    public string variableName;
    /*
    [HideInInspector]
    public byte flagIndex;
    [HideInInspector]
    public byte flagValue;
    */
    [HideInInspector]
    public MnetObject owner;

    public abstract void Serialize(Span<byte> reservedBytes);

    public abstract void Deserialize(Span<byte> receivedBytes);

    // !! !! MITES COUNT LUETAAN? SE ETTÄ OBJEKTI VOI ANTAA TAVUT VAATII ETTÄ TIETÄÄ KOON? ELI OBJEKTI TUNNISTAA ETTÄ ON JAETTTU
    // JA SEN PERIAATTEEL ANTAA OIKEEN MÄÄRÄN DATAA JA TIEDOT ELI TOTAL SIZE JA START POS
    // TOTALI PITÄÄ OTTAA KOSKA OBJEKTI TARVII SEN MUTTA STARTPOS JA MÄÄRÄ VOIDAA KATTOO TÄÄLLÄ
    public int ReadSplitSegment(Span<byte> incomingBytes)
    {
        // !!!! Täältä kai puuttuu mun mahtava idea
        // että splitti voi tulla yhdessä erässä eli tsekkaa bit ja tee kerralla. Voiko vaan oikasta deserialize kautta

        // 
        //int isNotSegmented = incomingBytes[ServerSettings.bytesReservedForSplitItemMaxSize - 1];
        //int totalSize = BinaryPrimitives.ReadInt16LittleEndian(incomingBytes);

        // When writing a variable that can be split, we use the sign as a marker to notify if the variable actually is not split
        // This allows us to save couple of bytes when they are not needed and spend less time processing
        // and in the end works basically like a varyinSize update (size header + data)
        int totalSize = MnetTools.BytesToInt(incomingBytes, MnetSettings.bytesReservedForSplitItemSize);
        // If the sign bit was toggled on, it means that this item was not split on this update so we can process it immediately
        if (totalSize < 0)
        {
            totalSize *= -1;
            Deserialize(incomingBytes.Slice(MnetSettings.bytesReservedForSplitItemSize,totalSize));
            // Return the amount processed, so the size bytes and the actual data byte count
            return MnetSettings.bytesReservedForSplitItemSize + totalSize;
        }

        // If this is the first time this split variable is processed, mark down the size and increase the byte array if needed
        if (splitBlockBytesRemaining == 0)
        {
            splitBlockBytesRemaining = totalSize;
            // If the item is larger than the reserved
            if (bytes.Length < totalSize)
            {
                bytes = new byte[totalSize];
            }
        }

        // !!! Pitäis olla oikein. TAvut talletetaan keskeltä (startPos) ja sourcessa on headeri offsettinä. Pitäis ollakki ok, congratulationssi
        
        int readPos = MnetSettings.bytesReservedForSplitItemSize;
        // Skip first bytes of totalSize and take startPos. It should always have the same byte count as totalSize
        int startPos = MnetTools.BytesToInt(incomingBytes.Slice(readPos), MnetSettings.bytesReservedForSplitItemSize);
        readPos += MnetSettings.bytesReservedForSplitItemSize;
        // Split variables are still limited by the size of a segment in packet so we use that limit for the actual byte count of the data
        int variableSegmentSize = MnetTools.BytesToInt(incomingBytes.Slice(readPos),MnetSettings.bytesReservedForSegmentSize);
        readPos += MnetSettings.bytesReservedForSegmentSize;
        // Copy data to the byte array and substract the number processed from the remaining amount
        for (int i = 0; i < variableSegmentSize; i++)
        {
            bytes[startPos+i] = incomingBytes[i+readPos];
        }
        splitBlockBytesRemaining -= variableSegmentSize;

        // If remaining data amount is zero, we should be finished and have all the bytes the variable needs so we can now deserialize it
        if(splitBlockBytesRemaining == 0)
        {
            Debug.Log("GOT ALL PIECES SO WE CAN READ WHOLE THING");
            Deserialize(bytes.AsSpan(0, totalSize));
        }

        // Split read and write methods require that the item returns the amount of bytes processed to the object,
        // so the header and data in this case
        return MnetSettings.variableMultipartHeaderLength + variableSegmentSize;
    }

    public int WriteSplitSegment(Span<byte> outgoingBytes, int spaceRemaining)
    {
            //Debug.Log("SPACE REMAINING IN SPLIT " + spaceRemaining);
        // Aika paljo juttui mitä tarvitaan on vaan object puolella hmmmMMMMmmmmm

        /*
         * Sama idea ku lukiessa. Onko tää eka kohta eli jos alotus 0, ni pitää Serialize(this.bytes)
         * Sitte vaan kopioidaa niin paljo ku on tilaa ja lopetetaan ja merkataa mihi jäätiin. Sitte ku remaining = 0 ni hasChanged = false
         * Pitää lisätä myös headeri eli seurata jos koko menee yli segmenti koon
         * Millo päivitetään koko?
         * 
         * BIT FLAG ON SMALLL ENDIAN JEESUS
         * 
         * Minus flipt toimii eli nyt vaan kirjotetaan niin kauan ku mahtuu ja huomioidaa ei splitatu
         * 
         * 
         * 
        */

        // UPDATE SIZE ?
        // CHECK IF CAN FIT WITHOUT SPLITTING
        // IF SPLITTING, REDUCE SPACE REMAINING BY HEADER AND PUSH IN AS MUCH POSSIBLE
        // SET HEADER AND RETURN WRITTEN + HEADER
        // Mahdolliset askeleet: Menee kerralla. Eka split. Keski split. Final split.


        // Okei onko tää ny sitä varte et jos ei oo splitattu vai final? Vähä sekava. Kato uusiks mut logiikka pitäis olla selkee

        // If the update is small enough on this item, we don't need to split


        // If this value is zero, it means that this is the first write attempt for this item in this update
        if (splitBlockBytesRemaining == 0)
        {

            //SetSize();

            // If we can fit whole item on the first write attempt, there is no need to split it
            if (MnetSettings.bytesReservedForSplitItemSize + sizeInBytes <= spaceRemaining) //(sizeInBytes + MnetSettings.bytesReservedForSplitItemSize) <= spaceRemaining) tää on ny VariableType puolella
            {
                // Recipient needs to identify splittable items that are not split and we can use the sign bit on the size for that.
                // Multiplying the size with a negative one is a simple operation and can be also used to turn it into usable form when read
                MnetTools.IntToBytes(outgoingBytes, (sizeInBytes * -1 ), MnetSettings.bytesReservedForSplitItemSize);
                // Instead of using the bytes array, we can serialize directly into the outgoing bytes. We leave space for the size on the slice
                Serialize(outgoingBytes.Slice(MnetSettings.bytesReservedForSplitItemSize));
                hasChanged = false;
                // We need to return the whole amount of written bytes, so the size of the header and the data combined
                return MnetSettings.bytesReservedForSplitItemSize + sizeInBytes;
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
                Serialize(bytes.AsSpan(0,sizeInBytes));
                // We need to track how much we have left to send, so set up the initial value
                splitBlockBytesRemaining = sizeInBytes;                
            }
        }

        int writePos = 0;
        int writeAmount = 0;

        // ELI TOTAL, STARTPOS, KOKO
        // MUUTEN FLIP EKA BIT JA LISÄÄ KOKO EKAAN KAHTEEN TAVUUN
        // Set up space of the header (total size of the item + start position of the write + amount of bytes)
        writePos = MnetSettings.variableMultipartHeaderLength;
        // Account of the space taken by the header
        spaceRemaining -= writePos;
        // The amount that we will write is limited by the space available or by the amount we have left to write
        writeAmount = Math.Min(spaceRemaining, splitBlockBytesRemaining);
        //todo // Tarvii ehkä talletaa erikseen alotus kohta jos sitä ei voi laskea splitBlockBytesRemainingilla ja bytes (miinus?)
        //int startPos = splitBlockBytesRemaining ??? // vastakkainen hmmm
        // Copy as much bytes into the outgoing span as we can
        for(int i = 0; i < writeAmount; i++)
        {
            outgoingBytes[writePos + i] = bytes[splitBlockWritePos + i];
        }
        // Fill out the header data
        MnetTools.IntToBytes(outgoingBytes, sizeInBytes, MnetSettings.bytesReservedForSplitItemSize);   // Total size of item
        MnetTools.IntToBytes(outgoingBytes.Slice(MnetSettings.bytesReservedForSplitItemSize),           // Start position
            splitBlockWritePos, MnetSettings.bytesReservedForSplitItemSize);
        MnetTools.IntToBytes(outgoingBytes.Slice(MnetSettings.bytesReservedForSplitItemSize * 2),       // Amount of bytes
            writeAmount, MnetSettings.bytesReservedForSegmentSize);

        // Remove the amount written from the total
        splitBlockBytesRemaining -= writeAmount;
        // Move the write position forward by the same amount
        splitBlockWritePos += writeAmount;

        // If we have finished writing, we will mark the variable as done and reset the write position
        if (splitBlockBytesRemaining == 0)
        {
            hasChanged = false;
            splitBlockWritePos = 0;
        }
        
        /*
        Debug.Log("(SPLIT WROTE) TOTAL: " + MnetTools.BytesToInt(outgoingBytes, MnetSettings.bytesReservedForSplitItemSize) +
            ". START: " + MnetTools.BytesToInt(outgoingBytes.Slice(MnetSettings.bytesReservedForSplitItemSize, 2), 2) +
            ". AMOUNT: " + outgoingBytes[4]);
        */
        // We return the amount of space that was used, meaning the header bytes and the actual data bytes
        return writePos + writeAmount;
    }

    // PITÄÄ AINAKI TESTATES OLLA ERIKSEEN (EHKÄ MUUTENKI HYVÄ VARAL) ET VOI KUTSUA MILLO VAAN
    public void UpdateSize()
    {
        if (sizeCategory != VariableSize.Static)
        {
            SetSize();

            /*
            // !!!! TÄÄLTÄ LÄHTEE MELKEEN KAIKKI VITTUU, MIKÄ VARMASTI PROSESSOIKI NOPEEMMI
            if (sizeCategory > VariableSize.Varies)
            {
                // Pitää varata bufferi vai lisätä vaan split koko ja sen mukaan erotella?
                if (sizeInBytes + owner.flagByteCount + MnetSettings.bytesReservedForSplitItemSize
                    <= MnetSettings.bytesReservedForSegmentSize)
                {
                    sizeCategory = VariableSize.DividableSingle;
                }
                else
                {
                    sizeCategory = VariableSize.DividableSplit;
                }
                // Divided items have an extra adjustment to ensure enum values are always unique, so we need to remove it from the size
                sizeInBytes -= MnetSettings.variableDividableHeaderAdjustment;
            }
            // Add the size of the header to the size of the item
            sizeInBytes += (int)sizeCategory;
            */
        }
        // Add the size of the item and possibly the header to objects total size
            //Debug.Log("ITEM SIZE OF "+variableName + " IS " + sizeInBytes);
        if (sizeInBytes <= MnetSettings.maxSegmentSize)
        { 
            owner.currentSize += (int)sizeCategory;
        }
        owner.currentSize += sizeInBytes;
        hasChanged = true;

    }

    // Used to setup the variable before use. Set the category of the item here and the bytes needed if the size is static.
    // E.g. MnetInt only needs the byte size of the 32-bit integer and to be set static:
    // sizeInBytes = 4;
    // sizeCategory = VariableSize.Static;
    public abstract void Setup();

    // This method is used by variables that do not have a static size. It needs to calculate the byte size of the data.
    // E.g. String in Unicode format is two bytes per character, so the size calculation was simply: sizeInBytes = Value.Length * 2
    public abstract void SetSize();
}
