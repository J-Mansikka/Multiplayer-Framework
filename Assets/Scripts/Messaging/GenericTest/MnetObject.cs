using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

/*
public enum ObjectSplit
{
    Auto, Always, Never
}
*/

// Blueprint eli metodi joka palauttaa tavun ja ei ota parametrei
public delegate byte SyncedAction();
public class MnetObject : MonoBehaviour
{
    public Ownership ownership;
    //public MnetObjectInstanceMessenger handler;   /// parempi että spawneri hoitaa objectin kommunikoinnin. Turhia välikäsiä muute
    public int objectTypeID = -1;//short         // ID number used by the ObjectHandler to communicate what type of object is being spawned/despawned
    public int objectInstanceID = -1;//short     // ID of object instance that is active and being synced
    protected MnetVariable[] variables;     // All the variables used by the object to act and stay in sync
    [HideInInspector]
    public int variableflagByteCount;               // Amount of bytes reserved for the bit flags, that inform the receiver of which variables to process
    //[HideInInspector]
    //public int methodFlagByteCount;
    protected MnetActions actions;
    //protected byte[] bytes;                 
    //protected int headerLength;//short      // Length of the object's header in bytes
    public bool hasUpdated;               // If anything changes between ticks, this bool is set so that the changes will be send
    //!!     public int currentSize;//short          // Current size of the object during the tick
    //  public MnetObject prevActiveObject;
    //  public MnetObject nextActiveObject;
    private int firstVariableToSerialize;   // Starting index of variables for serializing vittu keksi joku parempi kuvaus
    //public bool createSnapshotUpdateNext;

    //public Dictionary<int, Func<int>> syncedActions;
    protected SortedDictionary<byte, SyncedAction> methodDictionary;
    //protected LinkedList<byte> newActions;
       



    protected void Initialize()//MnetObject owner)
    {
        // !!! TESTING SHIT


        //syncedActions = new Dictionary<int, Func<int>>();
        methodDictionary = new SortedDictionary<byte, SyncedAction>();
        //newActions = new LinkedList<byte>();


        //// Poistin this parametrin ja kaikki näyttää silti toimivan? miks se tarvittiin alunperin?
        /// Oikeesti whats up?
        
        //Type type = owner.GetType();
        //Debug.Log(type);
        FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);//type.GetFields();
        MethodInfo[] methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        SortedList<string,MnetVariable> vars = new SortedList<string,MnetVariable>();
        SortedList<string,MethodInfo> methodCollection = new SortedList<string,MethodInfo>();
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
        foreach (MethodInfo meth in methods)
        {
            //Debug.Log(this.name + " " + meth.Name);



            string methodName = meth.Name;
            if (meth.Name.Length >= 6 && meth.Name.Substring(0,6) == "Action")
            {
                Debug.Log("FOUND METHOD "+meth.Name);
                SyncedAction newAct = (SyncedAction)Delegate.CreateDelegate(typeof(SyncedAction), this, meth);
                //Func<int> newFunction = del;
                byte returnID = newAct.Invoke();
                methodDictionary.Add(returnID, newAct);
                /*
                extractedVar = (MnetVariable)field.GetValue(this);//owner);

                extractedVar.owner = this;
                extractedVar.Setup();

                //if(extractedVar.itemMessagingMode == MessagingDirection.Auto) extractedVar.itemMessagingMode = messagingMode;
                vars.Add(field.Name, extractedVar);
                extractedVar.variableName = field.Name;

                */
            }
        }
        actions = new MnetActions();


        /*owner.*/
        variables = vars.Values.ToList().ToArray();
        // !!! Poistettu variableBitFlags = new byte[1 + ((variables.Length - 1) / 8)];
        variableflagByteCount = 1 + ((variables.Length - 1) / 8);
        //methodFlagByteCount = 1 + ((syncedActions.Count - 1) / 8);


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
        //headerLength = MnetSettings.objectHeaderIdAndSizeLength + variableflagByteCount;
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
            if(var.sizeCategory > VariableSize.Limited)
            {
                // Most likely the size will surpass one segment, but we can use it as a starting size since it grows automatically when needed
                // Segmentointi poistettu ja tää oli väärin. Koko oli 1 vaikka ideana oli kai olla 256
                var.bytes = new byte[256];
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

        //!!    currentSize = 0;

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

    public void Sync(byte methodID)
    {
        actions.Add(methodID);
        //newActions.AddLast(callerID);
    }

    public virtual void Activate()
    {
    }

    public virtual void SyncUp()
    {
        throw new NotImplementedException();
    }

    public void PawnTick()
    {
        Span<byte> calledMethods = actions.GetActions();
        // Iterate through the received action keys and invoke the method

        for (int i = 0; i < calledMethods.Length; i++)
        {
            methodDictionary[calledMethods[i]].Invoke();
        }
        /*
        foreach(byte index in newActions)
        {
            methodDictionary[index].Invoke();
        }
        */
        // We need to clear the actions before next tick
        //newActions.Clear();
    }

    public void Tick(bool createSnapshot)
    {
        if (createSnapshot)
        {
            SnapshotTick();
        }
        else
        {
            RegularTick();
        }
    }

    public virtual void SnapshotTick()
    {
        throw new NotImplementedException("Base implemention of SnapshotTick() on "+name+" was called." +
            " SnapshotTick() has to be overridden and is required for all synced objects.");
    }

    public virtual void RegularTick()
    {
        throw new NotImplementedException("Base implemention of RegularTick() on " + name + " was called." +
            " RegularTick() has to be overridden and is required for all synced objects.");
    }


    // The most basic version of preparing an object for a snapshot update would be to send all of its variables.
    // In practical use this method should be overridden with a specific implementation per object for sane results.
    // !!!! EI KUTSUTA MUN MIELEST SUORAAN VAAN KATOTAAN KU TULEE OBJEKTIN UPDATE ELI LISÄÄN FIELDIIN BOOLI
    public virtual void PrepareSnapshotUpdate()
    {
        for (int i = 0; i < variables.Length; i++)
        {
            variables[i].hasChanged = true;
        }
    }

    /*
    // ! ! !! !  WRITE TESTIS KÄYTÖS VOIT POISTAA KOSKA KOKO TULEE MUUTOKSISTA
    public void CountSize()
    {
        for (int i = 0; i < variables.Length; i++)
        {
            if (variables[i].hasChanged) currentSize += variables[i].sizeInBytes;
        }
    }
    */
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
            MnetVariable curVar = variables[i];
            if (curVar.sizeCategory > VariableSize.Static)
            {
                curVar.SetSize();
            }
            //byte flag = variableBitFlags[variables[i].flagIndex];
            //variableBitFlags[variables[i].flagIndex] = (byte)(flag | variables[i].flagValue);
        }
    }

    // The incoming bytes are sliced so that they only contain the bit flags and the variable data
    public void ReadChanges(MnetPacket packet)//Span<byte> objectAsBytes)
    {
        //Convert.ToString(objectAsBytes[0], toBase: 2));
        /*
        for (int i = 0; i < objectAsBytes.Length; i++)
        {
            print("RECEIVED "+i+" "+objectAsBytes[i]);
        }
        */

        // If a locally owned object is given remote data, this means there is a bug or some client is sending malicious data.
        // We will ignore this data and skip over to the next object
        if(ownership == Ownership.Local)
        {
            Debug.LogError("Locally owned object was sent remote data, which should not be possible.");
            return;
        }

        //int readPosition = 0;
        int numberOfActions = packet.ReadSingleByte();//objectAsBytes[readPosition++];


        
        /*
        if (newActions.Count == 0)
        {
            for (int i = 0; i < numberOfActions; i++)
            {
                newActions.AddLast(packet.ReadSingleByte());//objectAsBytes[readPosition++]);
            }
        }
        */

        int bitFlag = 1;
        // Action count value + actions
        //readPosition = 1 + numberOfActions;
        int currentFlagByte = packet.readPosition;//readPosition;

        // * print("R_FLAGS " + Convert.ToString(objectAsBytes[0], toBase: 2));



        // !!! DEBUG READ BYTES PRINT OUT. DELETE!
        for (int i = 1; i < 25; i++)
        {
            // * print("R_BYTE " + i + ": " + objectAsBytes[i]);
        }
        //int itemsProcessed = 0;
        // Move the read head to the data portion


        // ! ! ! ! !! ! DEBUG DELETE JA UNKOMMAA TAKAS OIKEE ! !  ! !!  ! !!  ! !
        // 0-16 = PACKET HEADER
        // 17-18 = INSTANCE ID
        // 19 = KOKO
        // 20- = bitflagit + data
        // ELI OIKEESSA TAPAUKSESSA TÄÄ SAA KAIKKI BITFLAGISTA ETEENPÄIN JA SERVER/CLIENT KÄYTTÄÄ ID JA KOON

        // We jump over the flag bytes to get to the actual variable data
        packet.readPosition += variableflagByteCount;

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
            if (bitFlag == (bitFlag & packet[currentFlagByte]))//variableBitFlags[currentFlagByte]))
            {
                MnetVariable curVar = variables[i];

                if (curVar.sizeCategory < VariableSize.Splittable)
                {

                    if (curVar.sizeCategory == VariableSize.Static)
                    {
                        curVar.Deserialize(packet);//objectAsBytes.Slice(readPosition, curVar.sizeInBytes));
                        //readPosition += curVar.sizeInBytes;
                    }
                    else
                    {
                        curVar.sizeInBytes = MnetTools.BytesToInteger(packet.Read(Mnet.bytesReservedForLimitedItemSize));//objectAsBytes.Slice(readPosition, Mnet.bytesReservedForSegmentSize));
                        //readPosition += Mnet.bytesReservedForSegmentSize;
                        curVar.Deserialize(packet);//objectAsBytes.Slice(readPosition, curVar.sizeInBytes));
                        //readPosition += curVar.sizeInBytes;
                    }
                }
                else
                {
                    /*
                    Debug.Log("(SPLIT READ) TOTAL: " + MnetTools.BytesToInteger(objectAsBytes.Slice(readPosition,2)) +
                    ". START: " + MnetTools.BytesToInteger(objectAsBytes.Slice(Mnet.bytesReservedForSplitItemSize, 2)) +
                    ". AMOUNT: " + objectAsBytes[readPosition+4]);
                    */
                    curVar.ReadSplitSegment(packet);//objectAsBytes.Slice(readPosition));
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

        //print("Need to write " + currentSize);

        // Object's size has not increased meaning it has not changed, so we do an early exit
        if(!hasUpdated)
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



        //int headerPos;
        //bool serializingObject = true;
        //int smallestSkippedItem = Int32.MaxValue;

        bool didSerializeSomething = false;

        // Add the object ID at first. If for some reason we end up skipping every item, we can return the bytes to the packet later
        MnetTools.IntegerToBytes(packet.Write(Mnet.bytesReservedForObjectID), objectInstanceID);

        // TEMP CHECK TAI EHKÄ LOPULLINEN (KATOTAA ENNE VAREJA ETTÄ ACTIONIT MAHTUU)

        if (actions.Length > 0)
        {
            if (packet.SpaceRemaining < actions.Length + 1)//newActions.Count + Mnet.bytesReserverdForActionCount)
            {
                // Get another packet
                packet.currentLength -= Mnet.bytesReservedForObjectID;
                // Actions should take relatively little space so if we can't fit them, we won't even bother with the variables
                return true;
            }
            else
            {
                actions.Serialize(packet);
                didSerializeSomething = true;
            }
        }


        // SEGMENT LOOP ALKU NYT POISTETTU
        //while (serializingObject)
        //{
        //print("WRITING! CURRENT SIZE "+currentSize);
        // Keep track if we actually managed to serialize a variable. If we did, we need to add a header to the segment
        //!!    int startSize = currentSize;
        // Store the header position of the segment
        //headerPos = packet.currentLength;
        // Reserver space for the object header
        //packet.currentLength += Mnet.objectHeaderIdLength;

        // Get remaining space for data on the packet
        //int spaceRemaining = Mnet.maxPacketDataSize - packet.currentLength;

        // We are limited by either the segment size or the remaining space in the packet so we have to check which is smaller
        //spaceRemaining = Math.Min(Mnet.maxSegmentSize, spaceRemaining);
        //int writePos = packet.currentLength;
        //int actionBytes = newActions.Count + 1;
        // We need one byte to store how many actions there were
        //  MnetTools.IntegerToBytes(packet.Write(Mnet.bytesReserverdForActionCount), newActions.Count);

        //writePos++;
        //spaceRemaining -= 1;
        // Then each action gets one byte
        /*
        bool serializeActions = true;
        while (serializeActions)
        {
            packet.WriteSingleByte(newActions.)
        }
        foreach(byte act in newActions)
        {
            packet.WriteSingleByte(act);
        }
        */
        //spaceRemaining -= newActions.Count;
        // Bytes containing the bit flags is belongs to the object instance, so it will take up space in the segment
        //spaceRemaining -= variableflagByteCount;
        //packet.currentLength += newActions.Count + 1;
        //packet.currentLength += variableflagByteCount;
        //writePos = packet.currentLength;
        // Set up values used to edit the bytes that contain the bit flags
        int flagBytesPos = packet.currentLength;//headerPos + Mnet.objectHeaderIdAndSizeLength + newActions.Count+1;
        int flagIndex = 0;
        int flagValue = 1;
        int itemSize = 0;
        // Set flag bytes to zero to erase old data
        for (int i = 0; i < variableflagByteCount; i++)
        {
            packet[flagBytesPos + i] = 0;
        }

        // Adding the header will increase packet's length and move the writing position of the actual variable data
        //packet.currentLength += headerLength;

        //int bytesWritten = 0;

        bool finishedSerializing = true;
        int startPos = firstVariableToSerialize;

        for (int v = startPos; v < variables.Length; v++)
        {
            MnetVariable curVar = variables[v];
            if (curVar.hasChanged)
            {
                // In items that the size can change, we need to update it when writing
                // !! !!! EI TÄS OO MITÄÄ JÄRKEE!

                //print("OBO: " + currentSize + ", " + curVar.variableName + " " + curVar.sizeInBytes);

                itemSize = curVar.sizeInBytes + (int)curVar.sizeCategory;
                Debug.Log(curVar.variableName + " " + curVar.sizeInBytes);
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
                // Segmentointi poistettu eli tää on turha. Vois ehkä yhdellä boolilla merkata että tarvitaan toinen paketti?
                if (itemSize > packet.SpaceRemaining)// spaceRemaining)
                {
                    /*
                    if (firstVariableToSerialize == 0)
                    {
                        firstVariableToSerialize = v;
                    }
                    if(itemSize < smallestSkippedItem) smallestSkippedItem = itemSize;
                    */
                    firstVariableToSerialize = v;
                    finishedSerializing = false;
                    continue;
                }

                // ! ! !! JOKU TURHAKE
                //byte curFlag = variables[v].flagValue;

                // ! !! ! Vähä turhaa sehlausta näitte flaggien kanssa. Jos tiedetään et objekti on muuttunu ni miks se asetettais nyt ja miks?
                //byte flagByte = variableBitFlags[curVar.flagIndex];
                //variableBitFlags[curVar.flagIndex] = (byte)(flagByte | curVar.flagValue);

                // !!! SERIALISOINTI BLOKKI MUT MIS VITUS KATOTAAN ETTÄ MAHTUU? YLEMPÄNÄ ON EARLY EXIT ELI
                // KAIKKI MUU PAITSI SPLITTABLE TARKASTAA KOON EKA
                if (curVar.sizeCategory < VariableSize.Splittable) //!curVar.canBeSplit)
                {
                    // If the item size can change between updates, we need to add it before the serialized value
                    if (curVar.sizeCategory == VariableSize.Limited) //curVar.varyingSize)
                    {
                        //MnetTools.IntegerToBytes(packet.AvailableSpace(), curVar.sizeInBytes);
                        MnetTools.IntegerToBytes(packet.Write(Mnet.bytesReservedForLimitedItemSize), curVar.sizeInBytes);//packet.Span(writePos), curVar.sizeInBytes, Mnet.bytesReservedForSegmentSize);
                        //writePos += Mnet.bytesReservedForSegmentSize;
                        //currentSize += (int)curVar.sizeCategory;
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
                    curVar.Serialize(packet);//packet.Span(writePos, curVar.sizeInBytes));
                    //}

                    // ! ! !!  Eiks splitti tarvii oman serialisointi metodin samanlai ku on toi vitun yhdistäminen?
                    // Ei koska serialisointi vaan luo tavuja ni.. tarvitaan vaan erikseen headeri ja sitte... Ei ku se mene varin kautta ni tarvitaa
                    // ELi vois hyödyntää ehkä sizeInBytesia ku antaa lohkon koon? Vai ihan erillinen kutsu? Tee metodi ja kato site

                    // We serialize the value into the packet bytes, account for the amount written and adjust the item to mark it as done
                    //writePos += curVar.sizeInBytes;
                    curVar.hasChanged = false;
                    // ! !! Miks nollataa jos hasChanged on se joka määrää? Onko snapshottia varten?
                    //if(curVar.sizeCategory != VariableSize.Static) curVar.sizeInBytes = 0;
                }
                else
                {
                    /*
                    if (itemSize > Mnet.maxSegmentSize)
                    {
                        currentSize += Mnet.variableMultipartHeaderLength;
                    }
                    else
                    {
                        currentSize += Mnet.bytesReservedForSplitItemSize;
                    }
                    */
                    //writePos +=
                    curVar.WriteSplitSegment(packet);//packet.Span(writePos, spaceRemaining), spaceRemaining);
                }
                // ! !!! ! Eiks nää pari vois pistää sitte ku kirjotetaan headeri ku kerra spaceRemaining kuitenki seuraa tilannetta yksistää?
                ///print("SIZE: " + currentSize + ", SPACE: " + spaceRemaining + ", LENGTH: " + packet.currentLength + ", WRITTEN: " + bytesWritten);
                // Adjust changes by the variable size
                //!!    spaceRemaining -= (writePos - packet.currentLength);
                //!!    currentSize -= (writePos - packet.currentLength);
                //!!    packet.currentLength = writePos;
                //Debug.Log("CHANGES AFTER WRITE: REMAINER = " + spaceRemaining + ". PACKET LENGTH = " + writePos);

                // ! ! ! TESTI MUT TOIMIVA KAI ELI EI SWAPATA BITTEI VAA LISÄTÄÄ FLAG DESIMAALI LUKUNA
                //packet.Span(headerPos + ServerSettings.objectHeaderIdAndSizeLength + variables[v].flagIndex, 1)[0] += curVar.flagValue;

                // Variable has been serialized

                didSerializeSomething = true;


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
            /*    
            if (packet.SpaceRemaining < Mnet.minimumSpaceNeededForWriting)
                {
                        //print("RAN OUT OF SPACE WITH " + currentSize + " REMAINING.");
                    // If all previous variables have fit, start next serialization loop at were we left off
                    if (firstVariableToSerialize == 0) firstVariableToSerialize = v + 1;
                    //serializingObject = false;
                    packet.isFull = true;
                    break;
                }
                */

            }
        }

        /*
        for(int i = 0; i < packet.currentLength; i++)
        {
            //print("WROTE[" + i + "] " + packet[i]);
        }
        */

            //print("SIZE " + currentSize);
        //print("START: " + startSize + ". SIZE: " + currentSize);
        // Mikä tää on? SEURAA ONKO KIRJOTETU JOTAIN ELI TARVII HEADERIN. ELI HEADER WRITE VAAN.
        // ON AINUT KOHTA MIS KIRJOTETAAN HEADERI MIKÄ ON JUST OIKEIN JA TÄHÄ SITTE LISÄTÄÄ BITFLAGIT JA NOLLATAA NE SEURAAVAA VARTE JEES JEES
        // VAI KIRJOTETAANKO SUORAAN [] OPERAATTTORIL KU MUUTETAAN?

        // Vois olla vaan bool kai? Eli heti jos jotai kirjotetaan -> true?
        // Tää on kai kaiken jälkeen eli on mahdollista että kelataan kaikki ohi ku ei mahdukkaan ni sit tarvitaan tää kai okei okei
        if (!didSerializeSomething)//startSize != currentSize)
        {
            // If we end up skipping every variable, we need to return the space taken by the object ID
            packet.currentLength -= Mnet.bytesReservedForObjectID;
            // HEADER KOHTAA EI SAA MUUTTAA KOSKA SE ON ALOTUS KOHTA ELI SIITÄ VOIDAAN ESIM. LASKEA KUINKA ISO MUUTOS KIRJATTIIN
            // EI OO WRITE POS KÄYTÖSSÄ KU KOKO AJAN PÄIVITETÄÄN PACKET CURRENTLENGTH NI PITÄÄ LUODA UUS KAI 
            //BinaryPrimitives.WriteInt16LittleEndian(packet.Span(headerPos, 2), objectInstanceID);

            //int writePos = headerPos;
            //MnetTools.IntegerToBytes(packet.Span(writePos, Mnet.bytesReservedForObjectID), objectInstanceID);
            ///writePos += Mnet.bytesReservedForObjectID;
            // ! !! TÄÄ PERSE ON KAI SITTEN KOKO JOKA ON LYÖ LUKKOON TAVUKS. TAITAA OLLA TEMP KOODIA TAAS JÖSSES
            // !! ! ! EI SAA OLLA VAAN YHEN TAVUN MUUTOS JOS SEGMENTTI MUUTETAAN ISOMMAKS
            //packet[headerPos + 2] = (byte)(packet.currentLength - headerPos);
            
            //!!!! KOKOA EI TARVITA ENÄÄ
            //int amountWritten = packet.currentLength - headerPos - Mnet.objectHeaderIdLength;
            //MnetTools.IntegerToBytes(packet.Span(writePos, Mnet.bytesReservedForSegmentSize), amountWritten);
            //print("ADDIGN HEADER! ID: "+MnetTools.BytesToInt(packet.Span(headerPos),MnetSettings.bytesReservedForObjectID)+". SIZE: "+(packet.currentLength-headerPos)+".");
            //!!! LUE ALA writePos += MnetSettings.bytesReservedForSegmentSize;
            /* !!! Eiks bit flagit aseteta suoraan pakettiin ku vari talletetaan? Kyl mun mielest ni fuck this osa
            for (int i = 0; i < variableBitFlags.Length; i++)
            {
                    
            }
            */
        }

        // !!! ______________DEBUG STUFF________________
        /*
        print("HEADER ID: "+MnetTools.BytesToInteger(packet.Span(headerPos, 2)));
        print("HEADER SIZE: " + packet[headerPos+Mnet.bytesReservedForObjectID]);
        print("HEADER FLAGS: " + Convert.ToString(packet[headerPos+Mnet.objectHeaderIdLength],toBase:2));
        for (int i = 4; i < 25;i++)
        {
            // * print("BYTE "+i+": "+packet[headerPos+i]);
        }
        */
        //print("!!!!!!!!!!!KOKO!!!!!!!!!!!! " + currentSize);

        /*


         CHECK: Eli käyt varit läpi ja kato onko viel jäljel -> early exit

         PATCH WORK PASKAA TAAS

         */
        /*
        bool finished = true;

        for (int i = 0; i < variables.Length; i++)
        {
            if (variables[i].hasChanged)
            {
                finished = false;
                break;
            }
        }
        */
        if(packet.SpaceRemaining < Mnet.minimumSpaceNeededForWriting)
        {
            packet.isFull = true;
        }

        if (finishedSerializing)
        {
            // We have managed to serialize all the data and can now stop
            print("NORMAL FINISH");
            firstVariableToSerialize = 0;
            //newActions.Clear();
        }
        // Segmentointi poistettu ni vaihtoehdot on vaan että mahtuu tai tarvitaan seuraava paketti
        else //if (smallestSkippedItem > packet.SpaceRemaining)//Mnet.maxPacketDataSize - packet.currentLength)
        {
            // If the current packet cannot fit even the smallest item, we need to get a new one
            print("NEW PACKET");
            return true;
        }


        //} SEGMENTOINTI LOOP LOPPU POISTETTU

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
        ownership = Ownership.Auto;
        objectInstanceID = -1;
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
