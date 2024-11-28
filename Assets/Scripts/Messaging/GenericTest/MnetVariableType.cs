using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class MnetVariableType<T> : MnetVariable
{
    [SerializeField]
    protected T _value; // Current value of the item.
    private T previous; // Previous value is stored so that it can be used in comparisons etc.
    private T stored;   // Last valid value will be saved before extrapolation. Will be used in a rollback when lost packets finally arrive.

    public T Value
    {
        get { return _value; }
        set
        {
            previous = _value;
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
