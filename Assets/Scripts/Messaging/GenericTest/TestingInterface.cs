using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TestingInterface
{
    public byte[] Serialize();
    public void Deserialize(byte[] bytes);
}
