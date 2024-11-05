using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Mobject : MonoBehaviour
{
    public bool active;
    public int idNumber;
    public int flagByteCount;
    public int headerSize;
    protected MdataItem[] allNetworkItems;
    public byte[] bytesToSend;
    private int newBytesCount;

    protected void Setup()
    {
        // How many full bytes are needed for bit flags
        flagByteCount = allNetworkItems.Length / 8;
        // Add final byte if needed for stragglers
        if (allNetworkItems.Length % 8 != 0 )
        {
            flagByteCount++;
        }
        int maxSizeOfObjects = 0;
        // Setup items and get their sizes
        foreach ( MdataItem item in allNetworkItems )
        {
            item.Initialize();
            if(item.sizeAsBytes > ServerSettings.maxSyncedObjects)
            {
                Debug.LogError(nameof(item)+" size is over the limit.");
            }
            maxSizeOfObjects += item.sizeAsBytes;
        }
        // Set header size
        headerSize = ServerSettings.idLength + flagByteCount;
        Debug.Log("HEADER = " + headerSize + ". MAXSIZE = " + maxSizeOfObjects);
        // Set up the container for items with its maximum possible size
        bytesToSend = new byte[headerSize + maxSizeOfObjects];
        byte[] idToBytes = BitConverter.GetBytes(idNumber); 
        for (int i = 0; i < ServerSettings.idLength; i++)
        {
            bytesToSend[i] = idToBytes[i];
        }


    }
    public int CheckChanges()
    {

        // Setting all flag bytes to zero
        for (int flagBytePos = 0; flagBytePos < flagByteCount; flagBytePos++)
        {
            bytesToSend[ServerSettings.idLength+flagBytePos] = 0;
        }

        // Array to store bytes from current item
        byte[] curItemAsBytes;
        // Number of current item
        int curItemNumber = 0;
        // Number of current byte slot
        int curByte = headerSize;
        // Section of bytes reserved for varying size value
        int sizeBytes = headerSize;
        // There can be multiple flag bytes, so current one is stored here
        int curFlagByte = 0;
        // Current flag position to set
        int bitMask = 1;
        // Reset count of new bytes
        newBytesCount = 0;

        // Käydään läpi kaikki osat
        foreach (MdataItem item in allNetworkItems)
        {
            Debug.Log(curItemNumber);
            // Set active flag byte
            curFlagByte = (curItemNumber / 8) + ServerSettings.idLength;
            // If the current item has changed, add it and set the flag
            if (item.hasChanged)
            {
                // Reserve bytes for size
                if (item.varyingSize)
                {
                    bytesToSend[curByte] = 0;
                    bytesToSend[curByte + 1] = 0;
                    curByte += 2;
                    newBytesCount += 2;
                }

                // Get item data as bytes, so it can be added to the message
                curItemAsBytes = item.ToBytes();
                // Add bytes one by one to final collection
                for (int i = 0; i < curItemAsBytes.Length; i++)
                {
                    bytesToSend[curByte] = curItemAsBytes[i];
                    curByte++;
                    newBytesCount++;
                }
                // Set the flag on curret flagByte in the header
                bytesToSend[curFlagByte] = (byte)(bytesToSend[curFlagByte] | bitMask);
                /*
                 * Debug.Log("FLAG TILANNE " + curItemNumber + ": " + Convert.ToString(bytesToSend[curFlagByte], toBase: 2));
                */
                // !!!!!!!!!! Add size to bytes
                if (item.varyingSize)
                {
                    int size = curItemAsBytes.Length;
                    bytesToSend[sizeBytes] = (byte)(size >> 8);
                    bytesToSend[sizeBytes+1] = (byte)size;
                }
                sizeBytes = curByte;
            }



            // Moving on to the next item
            curItemNumber++;

            // Return flag writing position to start at each new byte
            if (curItemNumber % 8 == 0)
            {
                bitMask = 1;
            }
            // Move flag writing position to the next bit
            else
            {
                bitMask = bitMask << 1;
            }
        }

        // Create array of returning bytes
        /*        byte[] curBytesToSend = new byte[headerSize + curByte];
                for (int i = 0; i <curBytesToSend.Length; i++)
                {
                    curBytesToSend[i] = bytesToSend[i];
                }*/

        // Return number of changes
        return curByte - headerSize;
    }

    public void SetChanges(byte[] bytesReceived)
    {
        int readPos = headerSize;

        // Lue flagit ja sen mukaan aseta
        int curFlagByte = ServerSettings.idLength;
        int flagReadhead = 1;
        int flagValue;
        foreach (MdataItem item in allNetworkItems)
        {
            flagValue = bytesReceived[curFlagByte] & flagReadhead;
            /*
            Debug.Log("flagreaderi "+flagReadhead+" "+Convert.ToString(flagValue, toBase:2));
            */
            if (flagValue != 0)
            {
                item.FromBytes(bytesReceived[readPos..(readPos + item.sizeAsBytes)]);
                if (item.varyingSize) { readPos += 2; }
                readPos += item.sizeAsBytes;

            }
            flagReadhead = flagReadhead << 1;
            if (flagReadhead == 256 )
            {
                flagReadhead = 1;
                curFlagByte++;
            }
        }
    }

}
