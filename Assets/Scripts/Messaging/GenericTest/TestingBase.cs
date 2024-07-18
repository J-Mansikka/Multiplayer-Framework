using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class TestingBase<T>
{
    [SerializeField]
    private T _value;
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
    [HideInInspector]
    private byte[] bytes;

    public T Value
    {
        get { return _value; }
        set
        {
            _value = value;
            hasChanged = true;
        }
    }
}
