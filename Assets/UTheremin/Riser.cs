
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Riser : UdonSharpBehaviour
{
    float height = 0.1f;

    void Interact()
    {
        height = Mathf.Repeat(height + 0.1f, 1.1f);
        var p = transform.position;
        p.y = height;
        transform.position = p;
    }
}
