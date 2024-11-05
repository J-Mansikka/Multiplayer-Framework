using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class MnetVariableArray<T> : MnetVariable
{
    [SerializeField]
    protected T[] _value;
    private bool[] changed;
    public T[] Value
    {
        get { return _value; }
        set
        {
            _value = value;
            hasChanged = true;
            owner.hasUpdated = true;
            SetSize();
            owner.currentSize += sizeInBytes;
        }
    }



    public T this[int i]
    {
        get { return _value[i]; }
        set
        {
            _value[i] = value;
            changed[i] = true;
            hasChanged = true;
        }
    }

}
