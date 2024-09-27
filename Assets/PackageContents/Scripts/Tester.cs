using UnityEngine;
using VInspector;

public class Tester : MonoBehaviour
{
    public Vector3 value;

    public float remapStartMin, remapStartMax;
    public float remapEndMin, remapEndMax;
    public bool remapClamp = true;

    [Button] 
    public void PrintRemappedValue()
    {
        Debug.Log(CBUtils.Remap(value, remapStartMin, remapStartMax, remapEndMin, remapEndMax, remapClamp));
    }
}
