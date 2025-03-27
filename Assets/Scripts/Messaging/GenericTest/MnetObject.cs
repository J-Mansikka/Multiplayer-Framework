using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;

/*
public enum ObjectSplit
{
    Auto, Always, Never
}
*/
public class MnetObject : MonoBehaviour
{
    //[Tooltip("Messaging mode determines who has control over the object. AUTO = Copy from Instance Messenger. ")]
    private bool bidirectional;
    //public MnetObjectInstanceMessenger handler;   /// parempi että spawneri hoitaa objectin kommunikoinnin. Turhia välikäsiä muute
    public int objectTypeID;//short         // ID number used by the ObjectHandler to communicate what type of object is being spawned/despawned
    public int objectInstanceID;//short     // ID of object instance that is active and being synced
    protected MnetVariable[] variables;     // All the variables used by the object to act and stay in sync
    [HideInInspector]
    public int flagByteCount;               // Amount of bytes reserved for the bit flags, that inform the receiver of which variables to process
    //protected byte[] bytes;                 
    protected int headerLength;//short      // Length of the object's header in bytes
    //public bool hasUpdated;               // If anything changes between ticks, this bool is set so that the changes will be send
    public int currentSize;//short          // Current size of the object during the tick
    public MnetObject prevActiveObject;
    public MnetObject nextActiveObject;
    private int firstVariableToSerialize;   // Starting index of variables for serializing vittu keksi joku parempi kuvaus
    protected void Initialize(bool communicateBothWays = false)//MnetObject owner)
    {
        //// Poistin this parametrin ja kaikki näyttää silti toimivan? miks se tarvittiin alunperin?
        /// Oikeesti whats up?
        
        //Type type = owner.GetType();
        //Debug.Log(type);
        bidirectional = communicateBothWays;
        FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);//type.GetFields();
        SortedList<string,MnetVariable> vars = new SortedList<string,MnetVariable>();
        MnetVariable extractedVar;
        foreach (FieldInfo field in fields)
        {
            //Debug.Log(this.name + " " + field.Name);
            if (field.FieldType.IsSubclassOf(typeof(MnetVariable)))
            {
                extractedVar = (MnetVariable)field.GetValue(this);//owner);

                extractedVar.owner = this;
                extractedVar.Setup();

                //if(extractedVar.itemMessagingMode == MessagingDirection.Auto) extractedVar.itemMessagingMode = messagingMode;
                vars.Add(field.Name,extractedVar);
                extractedVar.variableName = field.Name;
                //Debug.Log("NIMI "+field.Name);
            }
        }
        /*owner.*/variables = vars.Values.ToList().ToArray();
        // !!! Poistettu variableBitFlags = new byte[1 + ((variables.Length - 1) / 8)];
        flagByteCount = 1 + ((variables.Length - 1) / 8);

        /*  !! !!  Olis selkee jos olis variaabeleil sijainti näinki talletettu mutta ylimääräset fieldit tuhlaa rammia joten käytetää vaa järjestyst
        MnetVariable curVar;
        for (int i = 0; i < variables.Length; i++)
        {
            curVar = variables[i];
            
            curVar.flagIndex = (byte)(i / 8);
            curVar.flagValue = (byte)Math.Pow(2, i - curVar.flagIndex * 8);
            
        }
        */
        // !!! INT 
        //!!!! Poiistettu headerLength = (short)(variableBitFlags.Length + MnetSettings.objectHeaderIdAndSizeLength);
        headerLength = flagByteCount + MnetSettings.objectHeaderIdAndSizeLength;
        // !!! EIKS OO PAREMPI ET CURRENT SIZE ON VAAN DATA? EI BIT FLAGIT VARMAA KOSKAA OO HIRVEE MÄÄRÄ
        //currentSize = headerLength;
        firstVariableToSerialize = 0;
        //Debug.Log("BYTES NEEDED FOR VARIABLES "+variableBitFlags.Length);
        foreach (MnetVariable var in variables)
        {
            /*
            try
            {
                var.owner = this; //owner;
            }
            catch
            {
                //Debug.LogError("At least one variable in "+owner+" has not been initialized!\n"
                //    +"Variables must be initialized, either by Unity by making them public or using their constructor in the owner object. ");
            }
            */
            // Variable setup sets the category, static byte size and steps to ensure that the starting value is not null
            var.Setup();
            // !!!!! Korjaa sen ku nullattavat merkataa ekassa initialisoinnis (mnetbool, mnetstring etc)
            var.hasChanged = false;
            // If the variable can be split, we need to intialize its byte array that temporarily contains the data
            if(var.sizeCategory > VariableSize.Varies)
            {
                // Most likely the size will surpass one segment, but we can use it as a starting size since it grows automatically when needed
                var.bytes = new byte[MnetSettings.bytesReservedForSegmentSize];
            }
            //
            //
            //
            //currentSize += var.sizeInBytes;
            //
            //
            //
            // !!!!! KATO ALEMPI EIKS NÄÄ O TURHII
            /*
            if (var.sizeInBytes < 0 && !var.varyingSize)
            {
                //Debug.LogError("[ERROR: sizeInBytes not set.] "+var+" on "+owner+" is set to have a specific size in bytes , but it was not set in Setup method.");
            }
            */

            // ! !!! !  Jos koko vaihtelee ni eihä sillä välttämättä ole kokoa aluks. Pitää vaan tsekata ku lähettää et onks koko 0 ja sitte yrittä päöivttää
            /*
            if (var.varyingSize)
            {
                var.SetSize();
            }
            */
        }

        currentSize = 0;

        //Debug.Log("CURRENT OBJECT SIZE " + currentSize);

    }


    /*
    public void AttachHandler(MnetObjectInstanceMessenger handler, short objectTypeID)
    {
        this.objectTypeID = objectTypeID;
        this.handler = handler;
    }

    public void OnLocalSpawn()
    {
        handler.AddSpawnMessage(this);
    }

    public void OnLocalDespawn()
    {
        handler.AddDespawnMessage(this);
    }

    */

    /*
    public int CurrentSize()
    {
        int objectSize = 0;
        objectSize += variableBitFlags.Length;
        foreach (var variable in variables)
        {
            if (variable.hasChanged)
            {
                objectSize += variable.sizeInBytes;
                if (variable.varyingSize)
                {
                    objectSize += ServerSettings.bytesReservedForItemSizeValue;
                }
            }
        }
        return objectSize;
    }
    */


    // !! !  Onks tää joku mitä ei tarvita enää?
    /*
    public void UpdateCurrentSize(bool getFullSize = false)
    {
        foreach(MnetVariable var in variables)
        {
            if (getFullSize || var.hasChanged)
            {
                if (var.sizeCategory != VariableSize.Static)
                {
                    var.SetSize();
                }
                currentSize += var.sizeInBytes;
            }
        }
    }
    */

    public virtual void Tick()
    {
        throw new NotImplementedException("Base implemention of Tick() on "+name+" was called." +
            " Tick() has to be overridden and is required for all synced objects.");
    }


    // ! ! !! !  WRITE TESTIS KÄYTÖS VOIT POISTAA KOSKA KOKO TULEE MUUTOKSISTA
    public void DEBUGSize()
    {
        for (int i = 0; i < variables.Length; i++)
        {
            currentSize += variables[i].sizeInBytes;
        }
    }
    public void DebugPrintOut()
    {
        Debug.Log("OBJECT ID: " + objectInstanceID);
        for (int i = 0; i < variables.Length; i++)
        {
            Debug.Log("ITEM " + i + " :" + variables[i] + " " + (variables[i] as MnetInt).Value);
        }
    }

    public void ForceSizeTesti()
    {
        for (int i = 0; i < variables.Length; i++)
        {
            //byte updatedFlag = (byte)(variableBitFlags[variables[i].flagIndex] | variables[i].flagValue);
            //variableBitFlags[variables[i].flagIndex] = updatedFlag;
            variables[i].UpdateSize();
            //byte flag = variableBitFlags[variables[i].flagIndex];
            //variableBitFlags[variables[i].flagIndex] = (byte)(flag | variables[i].flagValue);
        }
    }

    // The incoming bytes are sliced so that they only contain the bit flags and the variable data
    public void ReadChanges(Span<byte> objectAsBytes)
    {
         //Convert.ToString(objectAsBytes[0], toBase: 2));
        /*
        for (int i = 0; i < objectAsBytes.Length; i++)
        {
            print("RECEIVED "+i+" "+objectAsBytes[i]);
        }
        */

        int bitFlag = 1;
        int currentFlagByte = 0;

        print("R_FLAGS " + Convert.ToString(objectAsBytes[0], toBase: 2));

        for (int i = 1; i < 25; i++)
        {
            print("R_BYTE " + i + ": " + objectAsBytes[i]);
        }
        //int itemsProcessed = 0;
        // Move the read head to the data portion


        // ! ! ! ! !! ! DEBUG DELETE JA UNKOMMAA TAKAS OIKEE ! !  ! !!  ! !!  ! !
        // 0-16 = PACKET HEADER
        // 17-18 = INSTANCE ID
        // 19 = KOKO
        // 20- = bitflagit + data
        // ELI OIKEESSA TAPAUKSESSA TÄÄ SAA KAIKKI BITFLAGISTA ETEENPÄIN JA SERVER/CLIENT KÄYTTÄÄ ID JA KOON

        int readPosition = flagByteCount;

        //// EIKS ID KATOTA CLIENT/SERVER PUOLELLA EIKÄ OBJECTISSA ?!
        //// TÄÄ VOI SEOTA !?
        //objectInstanceID = BinaryPrimitives.ReadInt16LittleEndian(objectAsBytes);



        // TÄÄL ON JO VARIABLE LENGTH LOOPIS MIT VIT
        for (int i = 0; i < variables.Length; i++)
        {
            //print(i+" READING FROM "+readPosition);
            //print("VARI "+bitFlag + ". PAKETTI " + objectAsBytes[currentFlagByte]);
            // Miks ei vaa suoraa objectAsbytes[i] == bitFlag & objectAsBytes???
            // Ja onks tää operaatio oikein? bitflag == bitflag? Wot... Ai joo bitflag on esim. 0010 ja & muuttais 0000 jos ei oo flagBytes sitä kans
            if (bitFlag == (bitFlag & objectAsBytes[currentFlagByte]))//variableBitFlags[currentFlagByte]))
            {
                MnetVariable curVar = variables[i];

                if (curVar.sizeCategory < VariableSize.Dividable)
                {

                    if (curVar.sizeCategory == VariableSize.Static)
                    {
                        curVar.Deserialize(objectAsBytes.Slice(readPosition, curVar.sizeInBytes));
                        readPosition += curVar.sizeInBytes;
                    }
                    else
                    {
                        curVar.sizeInBytes = MnetTools.BytesToInt(objectAsBytes.Slice(readPosition), MnetSettings.bytesReservedForSegmentSize);
                        readPosition += MnetSettings.bytesReservedForSegmentSize;
                        curVar.Deserialize(objectAsBytes.Slice(readPosition, curVar.sizeInBytes));
                        readPosition += curVar.sizeInBytes;
                    }
                }
                else
                {
                    Debug.Log("(SPLIT READ) TOTAL: " + MnetTools.BytesToInt(objectAsBytes.Slice(readPosition,2), MnetSettings.bytesReservedForSplitItemSize) +
                    ". START: " + MnetTools.BytesToInt(objectAsBytes.Slice(MnetSettings.bytesReservedForSplitItemSize, 2), 2) +
                    ". AMOUNT: " + objectAsBytes[readPosition+4]);
                    readPosition += curVar.ReadSplitSegment(objectAsBytes.Slice(readPosition));
                }
            }

            bitFlag = bitFlag << 1;
            //itemsProcessed++;

            // Roll over to next byte holding flags
            if (bitFlag == 256) //itemsProcessed % 8 == 0)
            {
                bitFlag = 1;
                currentFlagByte++;
            }
        }
    }

    public bool WriteChanges(MnetPacket packet)//Span<byte> currentPacket, out short currentPacketLength, bool snapshotOverride = false)
    {

        // EARLY EXITS:
        // 1. Has not changed & Not creating a snapshot = We skip, return false (Writing complete, restart loop with next object)
        // 2. Cannot fit and cannot split = We skip, return true (writing changes continues, but need next packet)
        // ----------------------------------
        // If passed, we are writing so check variable, write if can fit and flip the bit.

        // 1. Check if variable can fit and write it
        // Final: If wrote even one variable, add header info for the object

        // WHAT IS THE SIZE
        // IF SPLIT
        // IF CAN FIT

        // Object's size has not increased meaning it has not changed, so we do an early exit
        if(currentSize == 0)
        {
            return false;
        }

        // We need to skip full packets ( TUHLAAVA SYSTEEMI, VAIHDA TULEVAISUUDESSA)
        if (packet.isFull)
        {
            return true;
        }

        // Object is meant to stay whole, but cannot fit in this packet so we need another one
        //if(currentSize > (ServerSettings.maxPacketDataSize - packet.currentLength))
        //{
        //    return true;
        //}



        int headerPos;
        bool serializingSegment = true;
        int smallestSkippedItem = Int32.MaxValue;

        while (serializingSegment)
        {
                //print("WRITING! CURRENT SIZE "+currentSize);
            // Keep track if we actually managed to serialize a variable. If we did, we need to add a header to the segment
            int startSize = currentSize;
            // Store the header position of the segment
            headerPos = packet.currentLength;
            // Reserver space for the object header
            packet.currentLength += MnetSettings.objectHeaderIdAndSizeLength;

            // Get remaining space for data on the packet
            int spaceRemaining = MnetSettings.maxPacketDataSize - packet.currentLength;

            // We are limited by either the segment size or the remaining space in the packet so we have to check which is smaller
            spaceRemaining = Math.Min(MnetSettings.maxSegmentSize, spaceRemaining);

            // Bytes containing the bit flags is belongs to the object instance, so it will take up space in the segment
            spaceRemaining -= flagByteCount;
            packet.currentLength += flagByteCount;
            int writePos = packet.currentLength;
            // Set up values used to edit the bytes that contain the bit flags
            int flagBytesPos = headerPos + MnetSettings.objectHeaderIdAndSizeLength;
            int flagIndex = 0;
            int flagValue = 1;
            int itemSize = 0;
            // Set flag bytes to zero to erase old data
            for (int i = 0; i < flagByteCount; i++)
            {
                packet[flagBytesPos + i] = 0;
            }

            // Adding the header will increase packet's length and move the writing position of the actual variable data
            //packet.currentLength += headerLength;

            //int bytesWritten = 0;

            for (int v = firstVariableToSerialize; v < variables.Length; v++)
            {
                MnetVariable curVar = variables[v];
                if (curVar.hasChanged)
                {
                    // In items that the size can change, we need to update it when writing
                    // !! !!! EI TÄS OO MITÄÄ JÄRKEE!

                    itemSize = curVar.sizeInBytes + (int)curVar.sizeCategory;
                    /*
                    if(curVar.sizeInBytes == 0)
                    {
                        curVar.SetSize();
                    }
                    */
                    // !!!!! Pitäis olla ok. Eli jos splitataa ni skipataa tää osio
                    // If variable is too large and is not meant to be split, we have to skip it and later resume writing with a fresh segment
                    

                    //if (curVar.sizeInBytes + (int)curVar.sizeCategory > spaceRemaining && curVar.sizeCategory != VariableSize.DividableSplit)
                    // If the item cannot fit in the remaining space, but would fit in a single segment, we will skip it and write it whole later
                    if(itemSize < MnetSettings.maxSegmentSize && itemSize > spaceRemaining)
                    {
                        if (firstVariableToSerialize == 0)
                        {
                            firstVariableToSerialize = v;
                        }
                        if(itemSize < smallestSkippedItem) smallestSkippedItem = itemSize;
                        continue;
                    }

                    // ! ! !! JOKU TURHAKE
                    //byte curFlag = variables[v].flagValue;

                    // ! !! ! Vähä turhaa sehlausta näitte flaggien kanssa. Jos tiedetään et objekti on muuttunu ni miks se asetettais nyt ja miks?
                    //byte flagByte = variableBitFlags[curVar.flagIndex];
                    //variableBitFlags[curVar.flagIndex] = (byte)(flagByte | curVar.flagValue);

                    // !!! SERIALISOINTI BLOKKI MUT MIS VITUS KATOTAAN ETTÄ MAHTUU? YLEMPÄNÄ ON EARLY EXIT ELI
                    // KAIKKI MUU PAITSI SPLITTABLE TARKASTAA KOON EKA
                    if (curVar.sizeCategory < VariableSize.Dividable) //!curVar.canBeSplit)
                    {
                        // If the item size can change between updates, we need to add it before the serialized value
                        if (curVar.sizeCategory == VariableSize.Varies) //curVar.varyingSize)
                        {
                            //MnetTools.IntegerToBytes(packet.AvailableSpace(), curVar.sizeInBytes);
                            MnetTools.IntToBytes(packet.Span(writePos,MnetSettings.bytesReservedForSegmentSize,curVar.variableName), curVar.sizeInBytes);
                            writePos += MnetSettings.bytesReservedForSegmentSize;
                                //curVar.Serialize(packet.Span(writePos, curVar.sizeInBytes));
                                //print(curVar.variableName+" SIZE: "+curVar.sizeInBytes);
                            //!!!! Muutos automatisoitu variabletypeen bytesWritten += MnetSettings.bytesReservedForSegmentSize;
                            // Pitäiskö sitte lisätä currentSizee vaan täässä kohtaa +bytesForSegmentSize ? Jeeeeeeesus
                            //writePos += MnetSettings.bytesReservedForSegmentSize;
                            // !!!! Turhaa tsekataa välis kokoa ku tiedetää et mahtuu
                            //spaceRemaining -= MnetSettings.bytesReservedForSegmentSize;

                            // ! ! ! Current size tulee variaabeleist ja headerist eli jos ei oo varissa enää ni ei oo myöskää currentSizes
                            //currentSize -= ServerSettings.bytesReservedForItemSizeValue;
                            //packet.Span(packet.currentLength, 1)[0] = curVar.sizeInBytes;
                            //curVar.SetSize();
                                //Debug.Log(curVar.variableName + " READ SIZE IS " + (curVar.sizeInBytes));
                        }
                        //else
                        //{
                        curVar.Serialize(packet.Span(writePos, curVar.sizeInBytes, curVar.variableName));
                        //}

                        // ! ! !!  Eiks splitti tarvii oman serialisointi metodin samanlai ku on toi vitun yhdistäminen?
                        // Ei koska serialisointi vaan luo tavuja ni.. tarvitaan vaan erikseen headeri ja sitte... Ei ku se mene varin kautta ni tarvitaa
                        // ELi vois hyödyntää ehkä sizeInBytesia ku antaa lohkon koon? Vai ihan erillinen kutsu? Tee metodi ja kato site

                        // We serialize the value into the packet bytes, account for the amount written and adjust the item to mark it as done
                        writePos += curVar.sizeInBytes;
                        curVar.hasChanged = false;
                        // ! !! Miks nollataa jos hasChanged on se joka määrää? Onko snapshottia varten?
                        //if(curVar.sizeCategory != VariableSize.Static) curVar.sizeInBytes = 0;
                    }
                    else
                    {
                        if (itemSize > MnetSettings.maxSegmentSize) currentSize += MnetSettings.variableDividableHeaderLength;
                        writePos += curVar.WriteSplitSegment(packet.Span(writePos, spaceRemaining, curVar.variableName), spaceRemaining);
                    }
                    // ! !!! ! Eiks nää pari vois pistää sitte ku kirjotetaan headeri ku kerra spaceRemaining kuitenki seuraa tilannetta yksistää?
                    ///print("SIZE: " + currentSize + ", SPACE: " + spaceRemaining + ", LENGTH: " + packet.currentLength + ", WRITTEN: " + bytesWritten);
                    spaceRemaining -= (writePos - packet.currentLength);
                    currentSize -= (writePos - packet.currentLength);
                    packet.currentLength = writePos;
                    //Debug.Log("CHANGES AFTER WRITE: REMAINER = " + spaceRemaining + ". PACKET LENGTH = " + writePos);

                    // ! ! ! TESTI MUT TOIMIVA KAI ELI EI SWAPATA BITTEI VAA LISÄTÄÄ FLAG DESIMAALI LUKUNA
                    //packet.Span(headerPos + ServerSettings.objectHeaderIdAndSizeLength + variables[v].flagIndex, 1)[0] += curVar.flagValue;

                    // Variable has been serialized



                    /* ! !!! TOsi sotkunen vanha ja alla oleva pitäis toimai yhtä hyvin ja on miljoona kertaa selkeämpi
                    byte updateFlag = packet.Span(headerPos + MnetSettings.objectHeaderIdAndSizeLength + variables[v].flagIndex, 1)[0];
                    packet.Span(headerPos + MnetSettings.objectHeaderIdAndSizeLength + flagIndex, 1)[0]
                        = (byte)(updateFlag | flagValue);//variables[v].flagValue);
                    */
                    // ! ! !!  MITÄ JOS PÄIVITTÄÄ FLAGINDEXIIN SUORAAN OIKEEN KOHDAN ELI ALOTUS + HEADERLENGTH
                    // !!!! vähä myöhään tein mutta eiks tää oo okein ku flagIndex haetaan heti alussa ja varit käydää järjestää ni kasvaa vaa ++

                    // After serializing the variable, we will update its bit flag in the outgoing segment
                    // First we calculate the byte position where we store the flag. Everytime we have checked 8 bits we move to next byte
                    flagIndex = (v / 8);
                    // Second we calculate the bit we need to flip. We use the flagIndex value to reset back to 1 when previous byte is full
                    flagValue = 1 << v - (flagIndex * 8);
                    // Flip the bit on the correct byte
                    packet[flagBytesPos + flagIndex] = (byte)(packet[flagBytesPos + flagIndex] | flagValue);
                    //print(curVar.variableName+". FLAGS: " + Convert.ToString(packet[flagIndex],toBase:2));
                    // Switch to next bit flag

                    // Check if we have gone outside the byte and if true, start using the next byte
                    /*
                    if (flagValue == 256)
                    {
                        flagValue = 1;
                        flagIndex++;
                    }
                    */
                    // ! !!  TURHA ? Now we will remove the flag bit from the object since it's done
                    //byte clearFlag = variableBitFlags[variables[v].flagIndex];
                    //variableBitFlags[variables[v].flagIndex] = (byte)(clearFlag ^ variables[v].flagValue);

                    // Segments have a minimum treshold of space that need to exist to attemp writing
                    // and if that is reached, we need to move on to another segment
                    if (spaceRemaining < MnetSettings.minimumSpaceNeededForWriting)
                    {
                            //print("RAN OUT OF SPACE WITH " + currentSize + " REMAINING.");
                        // If all previous variables have fit, start next serialization loop at were we left off
                        if (firstVariableToSerialize == 0) firstVariableToSerialize = v + 1;

                        // If packet size passes the minimum threshold, we will mark it as full and swap to a new packet
                        if ((MnetSettings.maxPacketDataSize - packet.currentLength) < MnetSettings.minimumSpaceNeededForWriting)
                        {
                            serializingSegment = false;
                            packet.isFull = true;
                        }
                        break;
                    }
                }
            }

            for(int i = 0; i < packet.currentLength; i++)
            {
                //print("WROTE[" + i + "] " + packet[i]);
            }

                //print("SIZE " + currentSize);
            //print("START: " + startSize + ". SIZE: " + currentSize);
            // Mikä tää on? SEURAA ONKO KIRJOTETU JOTAIN ELI TARVII HEADERIN. ELI HEADER WRITE VAAN.
            // ON AINUT KOHTA MIS KIRJOTETAAN HEADERI MIKÄ ON JUST OIKEIN JA TÄHÄ SITTE LISÄTÄÄ BITFLAGIT JA NOLLATAA NE SEURAAVAA VARTE JEES JEES
            // VAI KIRJOTETAANKO SUORAAN [] OPERAATTTORIL KU MUUTETAAN?
            if (startSize != currentSize)
            {
                // ! !! ! ! !  MAGIC NUMBER 2. KÄYTÄ TOOLSSEJA?!
                // HEADER KOHTAA EI SAA MUUTTAA KOSKA SE ON ALOTUS KOHTA ELI SIITÄ VOIDAAN ESIM. LASKEA KUINKA ISO MUUTOS KIRJATTIIN
                // EI OO WRITE POS KÄYTÖSSÄ KU KOKO AJAN PÄIVITETÄÄN PACKET CURRENTLENGTH NI PITÄÄ LUODA UUS KAI 
                //BinaryPrimitives.WriteInt16LittleEndian(packet.Span(headerPos, 2), objectInstanceID);

                writePos = headerPos;
                MnetTools.IntToBytes(packet.Span(writePos, MnetSettings.bytesReservedForObjectID,"ID WRITE"), objectInstanceID, MnetSettings.bytesReservedForObjectID);
                writePos += MnetSettings.bytesReservedForObjectID;
                // ! !! TÄÄ PERSE ON KAI SITTEN KOKO JOKA ON LYÖ LUKKOON TAVUKS. TAITAA OLLA TEMP KOODIA TAAS JÖSSES
                // !! ! ! EI SAA OLLA VAAN YHEN TAVUN MUUTOS JOS SEGMENTTI MUUTETAAN ISOMMAKS
                //packet[headerPos + 2] = (byte)(packet.currentLength - headerPos);
                int amountWritten = packet.currentLength - headerPos - MnetSettings.objectHeaderIdAndSizeLength;
                MnetTools.IntToBytes(packet.Span(writePos, MnetSettings.bytesReservedForSegmentSize,amountWritten.ToString()), amountWritten);
                //print("ADDIGN HEADER! ID: "+MnetTools.BytesToInt(packet.Span(headerPos),MnetSettings.bytesReservedForObjectID)+". SIZE: "+(packet.currentLength-headerPos)+".");
                //!!! LUE ALA writePos += MnetSettings.bytesReservedForSegmentSize;
                /* !!! Eiks bit flagit aseteta suoraan pakettiin ku vari talletetaan? Kyl mun mielest ni fuck this osa
                for (int i = 0; i < variableBitFlags.Length; i++)
                {
                    
                }
                */
            }

            print("HEADER ID: "+MnetTools.BytesToInt(packet.Span(headerPos, 2,"ID READ"), 2));
            print("HEADER SIZE: " + packet[headerPos+MnetSettings.bytesReservedForObjectID]);
            print("HEADER FLAGS: " + Convert.ToString(packet[headerPos+MnetSettings.objectHeaderIdAndSizeLength],toBase:2));
            for (int i = 4; i < 25;i++)
            {
                print("BYTE "+i+": "+packet[headerPos+i]);
            }

            //print("!!!!!!!!!!!KOKO!!!!!!!!!!!! " + currentSize);
            if (currentSize == 0)
            {
                // We have managed to serialize all the data and can now stop
                print("NORMAL FINISH");
                firstVariableToSerialize = 0;
                serializingSegment = false;
            }
            else if (smallestSkippedItem > MnetSettings.maxPacketDataSize - packet.currentLength)
            {
                // If the current packet cannot fit even the smallest item, we need to get a new one
                print("NEW PACKET");
                return true;
            }
            // ELSE WE LOOP TO NEW SEGMENT


        }

        // PITÄÄ TEHDÄ UUS KIRJOTUS JA TÄYTTÄÄ HEADERI JOS TULEE 255 TÄYTEEN. SWAPPAAKO KESKEN VARIAABELIEN VAI ALOTTAAKO LOOPIN ALUST?


        // Data remaining, so writing continues on with the next packet


        /*
        for (int i = 0; i < packet.currentLength-1; i++)
        {
            print("SENDING " + i + " " + packet.WholePacket()[i]);
        }
        */

        // If we made it to the last return, object has been fully serialized and we move on the next one
        return false;
    }

    // RESETTI KU POISTUU NI VARMISTA ETTÄ SISÄLTÄÄ TARVITTAVAT JA ET VARIAABELIT RESETOIDAA KANSSA KUNNOL
    public void Reset()
    {
        // TÄYSI TURHA?
    }

    //  !!!!! Jää varmuuden vuoks mut ei oo mitää virkaa mun mielest. Esim. split puuttuu ni ei missää nimessä voi palata takas
    /*
    public void OldWriteChanges(Span<byte> currentPacket, bool getObjectState = false)
    {
        int bitFlag = 1;
        int currentFlagByte = 0;
        int itemsProcessed = 0;
        int writePosition = headerLength;

        // Convert object id and size into bytes
        BinaryPrimitives.WriteInt16LittleEndian(currentPacket, objectInstanceID);
        BinaryPrimitives.WriteInt16LittleEndian(currentPacket.Slice(2,2), currentSize);

        for (int i = 0; i < variableBitFlags.Length; i++)
        {
            variableBitFlags[i] = 0;
        }


        foreach (var variable in variables)
        {
            if (variable.hasChanged || getObjectState)
            {
                if (variable.varyingSize)
                {
                    if(getObjectState)
                    {
                        variable.SetSize();
                    }
                    //BitConverter.TryWriteBytes(currentPacket, (UInt16)variable.sizeInBytes);
                    BinaryPrimitives.WriteInt16LittleEndian(currentPacket.Slice(writePosition,MnetSettings.bytesReservedForSegmentSize), variable.sizeInBytes);
                    writePosition += MnetSettings.bytesReservedForSegmentSize;
                }
                variable.Serialize(currentPacket.Slice(writePosition,variable.sizeInBytes));
                writePosition += variable.sizeInBytes;
                variableBitFlags[currentFlagByte] = (byte)(variableBitFlags[currentFlagByte] | bitFlag);
                //Debug.Log("GUBBA BUBBA "+round+" "+ Convert.ToString(variableBitFlags[currentFlagByte],toBase:2));
                //round++;
                //// Jos napataan koko objecti eikä oo osa normaali päivitystä ni ei missää nimessä vaihdeta hasChanged arvoa
                if(!getObjectState) variable.hasChanged = false;

                //print(writePosition + " / " + currentPacket.Length);
            }
            bitFlag = bitFlag << 1;
            itemsProcessed++;
            if(itemsProcessed % 8 == 0)
            {
                bitFlag = 1;
                currentFlagByte++;
            }
        }

        for (int i = 0; i < variableBitFlags.Length; i++)
        {
            currentPacket[i+MnetSettings.objectHeaderIdAndSizeLength] = variableBitFlags[i];
        }

        currentSize = headerLength;
        //if (!getObjectState) hasUpdated = false;
    }

    */

}
