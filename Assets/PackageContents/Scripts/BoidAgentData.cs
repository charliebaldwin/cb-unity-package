using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public struct BoidAgentData
{
    public Vector3 position;
    public Vector3 direction;
     
    public Vector3 flockCenter;
    public Vector3 flockDirection;
    public Vector3 avoidDirection;
     
    public float viewRadius;
    public float avoidRadius;
     
    public int numFlockmates;

    public static int Size
    {
        get
        {
            return 5 * (sizeof(float) * 3) + 2 * sizeof(float) + 1 * sizeof(int);
        }
    }
}
