using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Mstring : MdataItem
{
    [SerializeField]
    private string _value;

    public string Value
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
        int stringLength = newDataAsBytes[0];
        Debug.Log("LENGTH " + Convert.ToString(newDataAsBytes[0],toBase:2));
        Debug.Log("LENGTH " + Convert.ToString(newDataAsBytes[1], toBase: 2));
        stringLength += newDataAsBytes[1];
        Debug.Log("LENGTH " + stringLength);
        //TODO Magic number vaihtuu asetettuun kokoon jos haluu yhen tavun s‰‰st‰‰ ja osat on aina 255 tai alle
        _value = Encoding.ASCII.GetString(newDataAsBytes[2..(2+stringLength)]);
    }

    public override byte[] ToBytes()
    {
        if (hasChanged)
        {
            hasChanged = false;
            return Encoding.ASCII.GetBytes(Value);
        }
        else
        {
            return null;
        }
    }


}
