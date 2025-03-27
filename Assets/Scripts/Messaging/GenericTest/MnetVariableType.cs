using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class MnetVariableType<T> : MnetVariable
{
    [SerializeField]
    protected T _value; // Current value of the item.
    //private T previous; // Previous value is stored so that it can be used in comparisons etc.
    //private T stored;   // Last valid value will be saved before extrapolation. Will be used in a rollback when lost packets finally arrive.

    public T Value
    {
        get { return _value; }
        set
        {
            //previous = _value;    !!!! Bufferointi pitäis tulla userilta tai käyttää paketteja
            _value = value;
            //GetAndSet();  // OIkeesit eiks tää oo vaa vitu array messenger paskaa?
            // When item changes and it does not have a static size, we need to call SetSize() to calculate it
            UpdateSize();
            /*
            hasChanged = true;
            if (varyingSize)
            {
                SetSize();
            }
            owner.currentSize += sizeInBytes;
            */
        }
    }

    /// Nolo ratkasu. Käytäjän pitää pistää :base(owner) itte
    /*
    public MnetVariableType(MnetObject owner = null)
    {
        this.owner = owner;
    }
    */

    // TÄÄ PITÄÄ SIIVOTA MIKÄ HELVETTI ON LOGIIKKA?!
    // Logiikka on array juttu mutta ehkä siihen löytyis joku parempi ratkaisu sittenki
    public T GetAndSet()
    {
        if (!hasChanged)
        {
            if (sizeCategory != VariableSize.Static)
            {
                SetSize();
            }
            owner.currentSize += sizeInBytes;
        }
        hasChanged = true;
        //owner.hasUpdated = true;

        return _value;
    }

}
