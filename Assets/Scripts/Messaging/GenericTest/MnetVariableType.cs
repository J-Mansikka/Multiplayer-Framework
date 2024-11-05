using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class MnetVariableType<T> : MnetVariable
{
    [SerializeField]
    protected T _value;
    public T Value
    {
        get { return _value; }
        set
        {
            _value = value;
            GetAndSet();
            /*
            hasChanged = true;
            owner.hasUpdated = true;
            if (varyingSize)
            {
                SetSize();
            }
            owner.currentSize += sizeInBytes;
            */
        }
    }

    /// Nolo ratkasu. K‰yt‰j‰n pit‰‰ pist‰‰ :base(owner) itte
    public MnetVariableType(MnetObject owner = null)
    {
        this.owner = owner;
    }

    public T GetAndSet()
    {
        if (!hasChanged)
        {
            if (varyingSize)
            {
                SetSize();
            }
            owner.currentSize += sizeInBytes;
        }
        hasChanged = true;
        owner.hasUpdated = true;

        return _value;
    }

}
