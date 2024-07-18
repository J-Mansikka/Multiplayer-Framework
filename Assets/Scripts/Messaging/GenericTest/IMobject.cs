using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMobject
{
    public bool Tick();
    public void Updated(bool yup);
}
