using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMMitemMethodsInterface
{
    public byte[] Serialize();
    public void Deserialize(byte[] bytes);
}
