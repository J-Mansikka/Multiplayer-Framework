using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NewArray<T> : MnetVariable 
    where T : MnetVariable
{
    public MnetVariableType<T>[] Items { get; private set; }
    public int Count { get; private set; }
    public int size;
    protected VariableSize itemSizeCategory;
    protected HashSet<int> changes;
    public MnetVariableType<T> this[int key]
    {
        get { return Items[key]; }
        set
        {
            Items[key] = value;
            changes.Add(key);
        }
    }

    public NewArray()
    {
        Setup();
    }

    public void Get()
    {

    }

    public override void Deserialize(Span<byte> receivedBytes)
    {
        throw new NotImplementedException();
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        throw new NotImplementedException();
    }

    public override void SetSize()
    {
        throw new NotImplementedException();
    }

    public override void Setup()
    {
        if (size == 0) size = 10;
        Items = new MnetVariableType<T>[size];
        changes = new HashSet<int>();
    }
}
