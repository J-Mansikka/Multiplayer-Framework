using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class MnetVariableArray<T> : MnetVariable
{
    [SerializeField]
    protected T[] _value;
    //private bool[] changed;
    private Queue<int> changes;     // Käytä inttiä mutta lähettäessä kutista array koon mukaan (eli alle 256 mahtuu bytee tai sitte short)
    private bool useFlags = true;   //- Sitte ku lukee ni vois käyttää 0 byten lohkoja ja vaan lisätä viimeisin perään eli byte -> int olis 0 0 0 X
    public T[] Value
    {
        get { return _value; }
        set
        {
            _value = value;
            hasChanged = true;
            //owner.hasUpdated = true;
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
            changes.Enqueue(i);
            hasChanged = true;
        }
    }


}
