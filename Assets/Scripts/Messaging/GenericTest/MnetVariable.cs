using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class MnetVariable
{
    [Tooltip("The mode in which the item can be set. (same as the object, server, client, shared)")]
    public MessagingDirection itemMessagingMode;
    //[Tooltip("Ignore older versions of item, if packets arrive out of order (e.g. object position)")]
    //public bool ignoreOlder;
    //[Tooltip("If item changes arrives out of order, perform reconciliation")]
    //public bool reconciliation;
    [Tooltip("True if item byte length is not constant and can change (e.g. strings or arrays)")]
    public bool varyingSize;
    [Tooltip("Maximum size of the item as bytes")]
    public short sizeInBytes = -1;

    public bool hasChanged;
    [HideInInspector]
    public byte flagIndex = 0;
    [HideInInspector]
    public byte orderIndex = 0;
    [HideInInspector]
    public byte[] bytes;
    [HideInInspector]
    public MnetObject owner;

    public abstract void Serialize(Span<byte> reservedBytes);

    public abstract void Deserialize(Span<byte> receivedBytes);

    public abstract void Setup();
    public abstract void SetSize();
}
