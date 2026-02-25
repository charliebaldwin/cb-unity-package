using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using VInspector;

public class ChunkLoader : MonoBehaviour
{
    public GameObject ChunkPrefab;

    public float Radius;
    public int Spacing = 8;

    [ShowInInspector]
    private List<int3> chunks = new List<int3>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //int2 steppedPos = int2
        //for (float x = -1f * Radius; x <= Radius; x += Spacing)
        //{
        //    for (float z = -1f * Radius; z <= Radius; z += Spacing)
        //    {
        //        int2 posToTry = 
        //    }
        //}

        int3 steppedPos = new int3(Mathf.FloorToInt(transform.position.x / Spacing), 0, Mathf.FloorToInt(transform.position.z / Spacing));
        if (!chunks.Contains(steppedPos))
        {
            chunks.Add(steppedPos);
            GameObject newChunk = Instantiate(ChunkPrefab);
            newChunk.transform.position = new Vector3(steppedPos.x, 0, steppedPos.z) * Spacing;
        }
    }
}
