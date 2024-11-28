using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;

public enum MessagingDirection
{
    Auto, SendOnly, ReceiveOnly, BothWays
}
public class MnetObject : MonoBehaviour
{
    [Tooltip("Messaging mode determines who has control over the object. AUTO = Copy from Instance Messenger. ")]
    public MessagingDirection messagingMode;    // Auto means the object will use the same mode as the Instance Messenger object
    //public MnetObjectInstanceMessenger handler;   /// parempi että spawneri hoitaa objectin kommunikoinnin. Turhia välikäsiä muute
    public short objectTypeID;             // ID number used by the ObjectHandler to communicate what type of object is being spawned/despawned
    public short objectID;                  // ID of object instance that is active and being synced
    protected MnetVariable[] variables;     // All the variables used by the object to act and stay in sync
    protected byte[] variableBitFlags;      // Bitflags bytes to mark changes between ticks
    //protected byte[] bytes;                 
    protected short headerLength;           // Length of the object's header in bytes
    public bool hasUpdated;                 // If anything changes between ticks, this bool is set so that the changes will be send
    public short currentSize;               // Current size of the object during the tick

        protected void Setup(MnetObject owner)
    {
        Type type = owner.GetType();
        FieldInfo[] fields = type.GetFields();
        SortedList<string,MnetVariable> vars = new SortedList<string,MnetVariable>();
        MnetVariable extractedVar;
        foreach (FieldInfo field in fields)
        {
            if (field.FieldType.IsSubclassOf(typeof(MnetVariable)))
            {
                extractedVar = (MnetVariable)field.GetValue(owner);
                extractedVar.Setup();
                if(extractedVar.itemMessagingMode == MessagingDirection.Auto) extractedVar.itemMessagingMode = messagingMode;
                vars.Add(field.Name,extractedVar);
                //Debug.Log(field.Name);
            }
        }
        owner.variables = vars.Values.ToList().ToArray();
        variableBitFlags = new byte[1 + ((variables.Length - 1) / 8)];
        headerLength = (short)(variableBitFlags.Length + ServerSettings.objectHeaderIdAndSizeLength);
        currentSize = headerLength;
        //Debug.Log("BYTES NEEDED FOR VARIABLES "+variableBitFlags.Length);
        foreach (MnetVariable var in variables)
        {
            try
            {
                var.owner = owner;
            }
            catch
            {
                Debug.LogError("At least one variable in "+owner+" has not been initialized!\n"
                    +"Variables must be initialized, either by Unity by making them public or using their constructor in the owner object. ");
            }
            var.Setup();
            //
            //
            //
            //currentSize += var.sizeInBytes;
            //
            //
            //
            if (var.sizeInBytes < 0 && !var.varyingSize)
            {
                Debug.LogError("[ERROR: sizeInBytes not set.] "+var+" on "+owner+" is set to have a specific size in bytes , but it was not set in Setup method.");
            }

            if (var.varyingSize)
            {
                var.SetSize();
            }
        }

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

    public short GetCurrentTotalSize()
    {
        short totalSize = 0;
        foreach(MnetVariable var in variables)
        {
            if (var.varyingSize)
            {
                var.SetSize();
            }
            totalSize += var.sizeInBytes;
        }
        return totalSize;
    }

    public virtual void Tick()
    {
        throw new NotImplementedException("Base implemention of Tick() on "+name+" was called." +
            " Tick() has to be overridden and is required for all synced objects.");
    }

    public void DebugPrintOut()
    {
        Debug.Log("OBJECT ID: " + objectID);
        for (int i = 0; i < variables.Length; i++)
        {
            Debug.Log("ITEM " + i + " :" + variables[i] + " " + (variables[i] as MnetInt).Value);
        }
    }

    // The incoming bytes are sliced so that they only contain the bit flags and the variable data
    public void ReadChanges(Span<byte> objectAsBytes)
    {
        for (int i = 0; i < 50; i++)
        {
            Debug.Log("LOOPING "+i+" " + Convert.ToString(objectAsBytes[i],toBase:2));
        }
        int bitFlag = 1;
        int currentFlagByte = 0;
        int itemsProcessed = 0;
        // Move the read head to the data portion
        int readPosition = variableBitFlags.Length;

        //// EIKS ID KATOTA CLIENT/SERVER PUOLELLA EIKÄ OBJECTISSA ?!
        //// TÄÄ VOI SEOTA !?
        objectID = BinaryPrimitives.ReadInt16LittleEndian(objectAsBytes);

        for (int i = 0; i < variables.Length; i++)
        {

            //print(i+" READING FROM "+readPosition);
            if (bitFlag == (bitFlag & objectAsBytes[currentFlagByte]))
            {

                if (variables[i].varyingSize)
                {
                    int currentVariableSize = BinaryPrimitives.ReadInt16LittleEndian(objectAsBytes.Slice(readPosition));
                    //print("VAR SIZE " + currentVariableSize);
                    readPosition += ServerSettings.bytesReservedForItemSizeValue;
                    variables[i].Deserialize(objectAsBytes.Slice(readPosition,currentVariableSize));
                    readPosition += currentVariableSize;
                }
                else
                {
                    variables[i].Deserialize(objectAsBytes.Slice(readPosition, variables[i].sizeInBytes));
                    readPosition += variables[i].sizeInBytes;
                }
            }

            bitFlag++;
            itemsProcessed++;

            if (itemsProcessed % 8 == 0)
            {
                bitFlag = 1;
                currentFlagByte++;
            }
        }
    }

    // 
    public void WriteChanges(Span<byte> currentPacket, bool getObjectState = false)
    {
        int bitFlag = 1;
        int currentFlagByte = 0;
        int itemsProcessed = 0;
        int writePosition = headerLength;

        // Convert object id and size into bytes
        BinaryPrimitives.WriteInt16LittleEndian(currentPacket, objectID);
        BinaryPrimitives.WriteInt16LittleEndian(currentPacket.Slice(2,2), currentSize);

        for (int i = 0; i < variableBitFlags.Length; i++)
        {
            variableBitFlags[i] = 0;
        }


        foreach (var variable in variables)
        {
            if (variable.hasChanged || getObjectState)
            {
                /*
                for (int i = 0; i < variable.sizeInBytes; i++)
                {
                    currentPacket[writePosition] = variable.bytes[i];
                    writePosition++;
                    updateSize++;
                }
                */
                if (variable.varyingSize)
                {
                    if(getObjectState)
                    {
                        variable.SetSize();
                    }
                    //BitConverter.TryWriteBytes(currentPacket, (UInt16)variable.sizeInBytes);
                    BinaryPrimitives.WriteInt16LittleEndian(currentPacket.Slice(writePosition,ServerSettings.bytesReservedForItemSizeValue), variable.sizeInBytes);
                    writePosition += ServerSettings.bytesReservedForItemSizeValue;
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
            currentPacket[i+ServerSettings.objectHeaderIdAndSizeLength] = variableBitFlags[i];
        }

        currentSize = headerLength;
        if (!getObjectState) hasUpdated = false;
    }

}
