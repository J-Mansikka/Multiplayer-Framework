using System;
using UnityEngine;

[System.Serializable]
public class Mint : MdataItem
{
    [SerializeField]
    private int _value;

    public int Value
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
        _value = BitConverter.ToInt32(newDataAsBytes);
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
