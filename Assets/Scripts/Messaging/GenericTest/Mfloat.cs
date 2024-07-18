using System;
using UnityEngine;

[Serializable]
public class Mfloat : MdataItem
{
    [SerializeField]
    private float _value;
    
    public float Value
    {
        get { return _value; }
        set
        {
            _value = value;
            hasChanged = true;
        }
    }
    public override void FromBytes(byte[] newDataAsBytes)
    {
        _value = BitConverter.ToSingle(newDataAsBytes);
    }

    public override byte[] ToBytes()
    {
        if (hasChanged)
        {
            hasChanged = false;
            return BitConverter.GetBytes(Value);
        }
        else
        {
            return null;
        }
    }


}
