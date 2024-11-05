using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Reflection;
using System;

public class MtestOrc : Mobject
{
    public MnetString orcName;
    public Mfloat x;
    public Mfloat y;
    public Mint hitpoints;
    public Mint lives;
    public Mint weaponCategory;
    public Mint weaponDamage;
    public Mint phoneNumber;
    public Mint numberOfSons;
    public Mint address;

    public MnetString[] strinkki;

    private void Awake()
    {
        //allNetworkItems = new MdataItem[] {orcName, x, y, hitpoints, lives, weaponCategory,weaponDamage,phoneNumber,numberOfSons,address };
        FieldInfo[] fields = typeof(MtestOrc).GetFields(BindingFlags.Public);
        allNetworkItems = new MdataItem[fields.Length];
        for (int i = 0; i < fields.Length; i++) 
        {
            allNetworkItems[i] = (MdataItem)fields[i].GetValue(null);
            Debug.Log(fields[i].Name);
        }
        byte[] hullo = new byte[4];
        int numba = BitConverter.ToInt32(hullo, 0);
        //Setup();


    }

}
