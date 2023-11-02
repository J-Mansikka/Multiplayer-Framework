using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityTest : MonoBehaviour
{
    private void FixedUpdate()
    {
        transform.position = transform.position + (new Vector3(0f, -1f, 0f) * Time.deltaTime);
    }
}
