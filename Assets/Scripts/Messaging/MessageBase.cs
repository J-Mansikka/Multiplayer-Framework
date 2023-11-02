using System;


public enum TransferType
{
    Repeating = 1,      // Receiving this message is not too important and a missing one can be interpolated.
    Verifiable = 2,     // Message should arrive and in order. Ask for resend if missing some and do a manual reconciliation.
    Important = 3,      // Message is important and needs an acknowledgement message back. Good timer would be average RTT * 1.2 ?
    ACK = 4             // Acknowledgement message to notify of successful receiving and handling.
                        // 10 FREE SPOTS for future types but these should be plenty.
}

public enum MessageType
{
    NotSet = 0,
    ClientInput = 1,

}

public abstract class MessageBase
{
    bool setupMessageContainer = true;

    public bool notInUse;                                       // Marks if the message can be reused.
    public int messageMaxLength = 0;                            // Calculated max size the message in bytes.
    public byte[] messageAsBytes;                               // Contains the message values in bytes.
    TransferType transferMethod;                    
    public MessageType messageType;                             // Message type.
    // Message pakolliset osat,
    // jotka tulee mukaan pakettiin
    private int knownCommonSegments = 4;                     // Length = 0-1, MessageType = 2, MessageNumber = 3
    public ushort count;                                        // Length of the current message in bytes.
    //public byte[] messageSizeAndCategory = new byte[2];
    public byte sequenceNumber;                                 // Sequence number of the message.
    //public byte id;
    public bool[] segmentBools = new bool[16];                  //     
    //public byte[] segmentFlags = new byte[2];                   // 16 mahdollista flag arvoa ett‰ kyll‰ pit‰isi riitt‰‰

    public int writeHead = 0;
    //public ushort readHead;
    
    //public ushort toggleFlagIndex;

    public MessageSegmentBase[] messageSegments;
    public int flagByteCount;

    protected bool messageHasChanged;

    public MessageBase()
    {
        //messageSegments = new MessageSegmentBase[16];           // 16 flags means 16 max segments. Feel free to increase the size if necessary.
        notInUse = true;
    }


    public virtual void Reset()
    {
        for (int i = 0; i < flagByteCount; i++)
        {
            messageAsBytes[ServerSettings.flagSegmentsStart + i] = 0;
        }
    }

    public void AutoSetup()
    {
        flagByteCount = (ushort)((messageSegments.Length / 8) + 1);
        messageMaxLength = knownCommonSegments + flagByteCount;

        foreach (var segment in messageSegments)
        {
            if (segment.MaxAllottedSize == 0) throw new Exception(segment + " MaxAllottedSize is not set");
            messageMaxLength += segment.MaxAllottedSize;
        }
        messageAsBytes = new byte[messageMaxLength];

        messageAsBytes[ServerSettings.typeSegment] = (byte)messageType;      // Message Type will never change so it can be set here.
        setupMessageContainer = false;
    }

    /*
    protected void FlagsToBytes()
    {
        for (int i = 0; i < 8; i++)
        {
            // L‰htee vasemmalta oikealle
            if (segmentBools[i]) messageAsBytes[4] |= (byte)(1 << (7 - i));
            if (segmentBools[i + 8]) messageAsBytes[5] |= (byte)(1 << (7 - i));
        }
    }
    */

    public byte[] ToBytes()
    {

        // First time setup
        if (messageHasChanged)
        {
            /*
            if (setupMessageContainer)
            {

                flagByteCount = (ushort)((messageSegments.Length / 8) + 1);
                messageMaxLength = knownCommonSegments + flagByteCount;

                foreach (var segment in messageSegments)
                {
                    if (segment.MaxAllottedSize == 0) throw new Exception(segment + " MaxAllottedSize is not set");
                    messageMaxLength += segment.MaxAllottedSize;
                }
                messageAsBytes = new byte[messageMaxLength];

                messageAsBytes[ServerSettings.typeSegment] = (byte)messageType;      // Message Type will never change so it can be set here.
                setupMessageContainer = false;
            }
            */


            // Move writehead to data portion
            writeHead = knownCommonSegments + flagByteCount;
            //toggleFlagIndex = 0;
            //[writeHead++],messageSizeAndCategory[1], id , indexNumber.Bytes[0],segmentFlags[0],segmentFlags[1]


            // Turn items into bytes and set the relevant flags to true.

            int bytesLeftToProcess = flagByteCount;
            int segmentsProcessed = 0;

            while(bytesLeftToProcess > 0)
            {
                int bitsLeftToProcess = 8;
                while(bitsLeftToProcess > 0 && segmentsProcessed < messageSegments.Length)
                {

                    if (messageSegments[segmentsProcessed].ToBytes(messageAsBytes, ref writeHead))
                    { 
                        // Set the flag
                        messageAsBytes[ServerSettings.flagSegmentsStart + bytesLeftToProcess - 1]
                            = (byte)(messageAsBytes[ServerSettings.flagSegmentsStart + bytesLeftToProcess - 1] | (1 << (segmentsProcessed + ((bytesLeftToProcess - flagByteCount) * 8))));
                        // Convert segment to bytes and move writehead.
                        //???????????
                    }

                    bitsLeftToProcess--;
                    segmentsProcessed++;
                }
                bytesLeftToProcess--;
            }

            /*
            foreach (var segment in messageSegments)
            {
                segmentBools[toggleFlagIndex] = segment.ToBytes(messageAsBytes, ref writeHead);
                toggleFlagIndex++;
            }
            */

            // Finalize by checking and storing message length and adding the sequence number
            if (writeHead <= ServerSettings.maxPacketSize)
            {
                // Combining message length and message priority. These two bytes will always be the first segment in every message.
                ushort messageLength = (ushort)writeHead;
                //int transferMethodAsBits = (int)transferMethod << 12 & 0xFF00;                // With shifting the size portion gets pushed out. Just to explain sloppy comparison.
                //ushort sizeAndType = (ushort)(messageLength | (ushort)transferMethodAsBits);
                messageAsBytes[0] = (byte)(messageLength);//sizeAndType;
                messageAsBytes[1] = (byte)(messageLength >> 8);//(sizeAndType >> 8);
                messageAsBytes[ServerSettings.sequenceSegment] = sequenceNumber;
                //FlagsToBytes();

            }
            else
            {
                throw new Exception("Message size surpassed the space reserved in package");
            }

            // Message has been set. In case the bytes are needed later, we will just skip these step and send the bytes again.
            messageHasChanged = false;

        }


        return messageAsBytes;
    }

    public virtual void ToData(byte[] incoming, int messageStartPosition = 0)
    {
        // SIZE & TRANS 2
        // ID 1
        // INDEX 1
        // FLAGS 2
        // DATA

        //ushort messageLengthAndMethod = BitConverter.ToUInt16(incoming, messageStartPosition);     // First two bytes contain transfer method and message length.
        //transferMethod = (TransferType)(messageLengthAndMethod >> 12 & 0xF);    // Shift stored TransferMethod to start of the bytes and read it.
        //count = (ushort)(messageLengthAndMethod & 0x07FF);                      // Read the first eleven bits that is used to store the length of the message.

        // Get length of message
        count = BitConverter.ToUInt16(incoming, messageStartPosition);

        // Get sequence
        sequenceNumber = incoming[ServerSettings.sequenceSegment + messageStartPosition];

        // Get data with flags
       //ebug.Log(flagByteCount);
        int flagBytesLeft = flagByteCount;
        int flagsLeft = messageSegments.Length;
        int readHead = messageStartPosition + ServerSettings.flagSegmentsStart + flagByteCount;

        while (flagBytesLeft > 0)
        {
            int bitsChecked = 0;
            while (flagsLeft > 0 && bitsChecked < 8)
            {
                if ((incoming[messageStartPosition + ServerSettings.flagSegmentsStart + flagBytesLeft - 1] & (1 << bitsChecked)) != 0)
                {
                    // Set data from bytes
                    messageSegments[bitsChecked].ToData(incoming, readHead);
                    // Clear out the flag
                    incoming[messageStartPosition + ServerSettings.flagSegmentsStart + flagBytesLeft]
                        = (byte)(incoming[messageStartPosition + ServerSettings.flagSegmentsStart + flagBytesLeft] & ~(1 << bitsChecked));
                }

                readHead += messageSegments[bitsChecked].MaxAllottedSize;
                bitsChecked++;
                flagsLeft--;
            }
            flagBytesLeft--;
        }

        /*
        for (int i = 0; i < messageSegments.Length; i++)
        {
            if(incoming[] & (1 << pos)) != 0;)
            //if (segmentBools[i]) { messageSegments[i].ToData(incoming, i); Debug.Log(messageSegments[i]); }
        }

        // Convert flag bytes into the bool array
        
        for (int i = 1; i < 8; i++)
        {
            //Debug.Log(incoming[4 + messageStartPosition] + " AND " + incoming[5 + messageStartPosition]);
            segmentBools[i+8] = (incoming[4 + messageStartPosition] & (1 << i)) != 0;
            segmentBools[i] = (incoming[5 + messageStartPosition] & (1 << i)) != 0;
            Debug.Log("AS BOOLS " + segmentBools[i] + " " + segmentBools[i+8]);
        }
        */
        /*
        for (int i = 1; i < 8; i++)
        {
            //Debug.Log(incoming[4 + messageStartPosition] + " AND " + incoming[5 + messageStartPosition]);
            segmentBools[i + 8] = (incoming[4 + messageStartPosition] & (1 << i)) != 0;
            segmentBools[i] = (incoming[5 + messageStartPosition] & (1 << i)) != 0;
            Debug.Log("AS BOOLS " + segmentBools[i] + " " + segmentBools[i + 8]);
        }
        */




        
    }
}
