/*

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class MnetVariableArray<T> : MnetVariable 
    where T : MnetVariable
{
    public int arraySize;
    [SerializeField]
    protected T[] _value;
    //private bool[] changed;
    public HashSet<int> changes;     // Käytä inttiä mutta lähettäessä kutista array koon mukaan (eli alle 256 mahtuu bytee tai sitte short)


    public T[] Value
    {
        get { return _value; }
        set
        {
            _value = value;
            // !!! Ei kai voi merkata hasChanged koko arraylla ku sittehä kokoki vaihtuis. Eli vaan yksittäiset muutokset huomioidaaa. Eli nää on turhia?
            // !!!! POISTA KOKO GET SET JOS ET KEKSI TÄNNE JOTAIN
            //hasChanged = true;
            //owner.hasUpdated = true;
            //SetSize();
        }
    }

    // !!! Jos molemmat deserialize ja serialize käyttää tätä niin eikös muutokset pauku koko ajan? Ei kai koska _value[i].sizeInBytes muutos esimerkiks on setteri action
    // Onkso niin että get set menee sen mukaan kummal puolella on = merkkiä lol ei kai
    public T this[int i]
    {
        get
        {
            return _value[i];
        }
        set
        {
            _value[i] = value;
            changes.Add(i);
            hasChanged = true;
            // Koko pitäis päivittyä talletuksen yhteydes ni pitäis olla up to date ja good to go
            sizeInBytes += _value[i].sizeInBytes + Mnet.bytesReservedForArrayIndex;
            if(_value[i].sizeCategory != VariableSize.Static) sizeInBytes +=  Mnet.bytesReservedForSplitItemSize;
            //SetSize();
        }
    }

    public override void Deserialize(Packet packet) //Span<byte> receivedBytes)
    {
        changes.Clear();

        int index;
        //int readPos = 0;
        int bytesRemaining = sizeInBytes;//receivedBytes.Length;
        int size = 0;
        while (bytesRemaining > 0)
        {
            // LUE INDEX
            index = MnetTools.BytesToInteger(packet.ReadBytes(Mnet.bytesReservedForArrayIndex));//receivedBytes.Slice(readPos,Mnet.bytesReservedForArrayIndex));
            //readPos += Mnet.bytesReservedForArrayIndex;
            bytesRemaining -= Mnet.bytesReservedForArrayIndex;

            // LUE KOKO: RAJOTETTU NYT YHTEEN TAVUUN
            if (_value[index].sizeCategory != VariableSize.Static)
            {
                _value[index].sizeInBytes = MnetTools.BytesToInteger(packet.ReadBytes(Mnet.bytesReservedForLimitedItemSize));//receivedBytes.Slice(readPos, Mnet.bytesReservedForSegmentSize));
                //readPos += Mnet.bytesReservedForSegmentSize;
                bytesRemaining -= Mnet.bytesReservedForLimitedItemSize;
            }
            // LUE DATA
            _value[index].Deserialize(packet);//receivedBytes.Slice(readPos, _value[index].sizeInBytes));
            //readPos += _value[index].sizeInBytes;
            bytesRemaining -= _value[index].sizeInBytes;
            //  changes.Add(index); KUN SAA ARVON NI CHANGES LISÄTÄÄ AUTOMAATTISESTI
        }
    }

    public override void Serialize(Packet packet)//Span<byte> reservedBytes)
    {
        // !!!! KOKO VOI MENNÄ RAJAN YLI ELI TARVITAA YHTEINEN KOHTA MISSÄ TARKASTAA SE

        //int writePos = 0;
        foreach(int i in changes)
        {
            Debug.Log("WROTE "+i);
            // SPLITTI TAIS TOIMII NIIN ETTÄ TÄÄ KUTSUTAAN OMALLA BYTESILLA MUTTA MITES SE KOKO?
            // KOKO PITÄÄ LASKEA EKA.. MUT SIT PITÄIS OLLA LASKETTU JO KU TÄÄ METODI KUTSUTAAN. ELI TEHDÄÄ SAMA KU YKSITTÄISIS ELI KOKO PÄIVITTYY KU ON MUUTOKSIA

            // LISÄÄ INDEX (byte)
            MnetTools.IntegerToBytes(packet.WriteBytes(Mnet.bytesReservedForArrayIndex),i);// reservedBytes.Slice(writePos),i,Mnet.bytesReservedForArrayIndex);
            //writePos += Mnet.bytesReservedForArrayIndex;
            // LISÄÄ KOKO (_Value[i] size in bytes)
            if (_value[i].sizeCategory != VariableSize.Static)
            {
                MnetTools.IntegerToBytes(packet.WriteBytes(Mnet.bytesReservedForLimitedItemSize), _value[i].sizeInBytes);//reservedBytes.Slice(writePos), _value[i].sizeInBytes, Mnet.bytesReservedForSegmentSize);
                //writePos += Mnet.bytesReservedForSegmentSize;
            }
            // LISÄÄ DATA
            _value[i].Serialize(packet);//reservedBytes.Slice(writePos));
            //writePos += _value[i].sizeInBytes;
        }
        changes.Clear();
    }

    public override void SetSize()
    {
        // Nasty hack ettei kutsuta monta kertaa
        int totalSize = 0;

        foreach (var item in _value)
        {
            if (item != null)
            {
                if(item.sizeCategory != VariableSize.Static) item.SetSize();
                totalSize += item.sizeInBytes + Mnet.bytesReservedForArrayIndex;
                if (item.sizeCategory != VariableSize.Static) totalSize += Mnet.bytesReservedForLimitedItemSize;
            }
        }
        sizeInBytes = totalSize;
        Debug.Log("SETSIZE: "+sizeInBytes);
    }

    public override void Setup()
    {
        sizeCategory = VariableSize.Splittable;
        changes = new HashSet<int>();
        if(_value == null)
        {
            _value = new T[arraySize];
        }
        else
        {
            for (int i = 0; i < _value.Length; i++)
            {
                _value[i].Setup();
            }
            SetSize();
        }

        // Nasty hack 2. Nää ei kai oo ongelma lopullises? Mut pitää huomioida se ku luodaan uus että lähtee KAIKKI data. Variaabeliin uus metodi? GetAllData?
        GetAllItems();
    }

    public void GetAllItems()
    {
        for(int i = 0; i < _value.Length; ++i)
        {
            changes.Add(i);
        }
    }


}
*/
