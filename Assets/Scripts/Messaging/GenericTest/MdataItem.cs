using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MdataItem
{
    [Tooltip("Ignore older versions of item, if packets arrive out of order (e.g. object position)")]
    public bool ignoreOlder;
    [Tooltip("If item changes arrives out of order, perform reconciliation")]
    public bool reconciliation;
    [Tooltip("Check if item size is not constant (e.g. strings or arrays)")]
    public bool varyingSize;
    [Tooltip("Maximum size of the item as bytes")]
    public int sizeAsBytes = 0;
    [HideInInspector]
    public bool hasChanged;
    [HideInInspector]
    public byte flagIndex = 0;
    [HideInInspector]
    public byte orderIndex = 0;

    public void Initialize()
    {
        hasChanged = true;
        if (!varyingSize && sizeAsBytes == 0)
        {
            sizeAsBytes = ToBytes().Length;
        }
        // Ei ehkä tarpeellinen ellei objectin spawnaaminen tarvitse
        hasChanged = true;
    }
    public abstract byte[] ToBytes();
    public abstract void FromBytes(byte[] newDataAsBytes);

}
