using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using VInspector;
using VInspector.Libs;

public class ChunkLoader : MonoBehaviour
{
    public GameObject ChunkPrefab;

    public float Radius;
    public int Spacing = 8;
    public Vector2Int InitialChunks = new Vector2Int(8,8);

    [ShowInInspector]
    private List<int2> chunks = new List<int2>();
    private VoxelChunk[,] voxelChunks;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        voxelChunks = new VoxelChunk[InitialChunks.x, InitialChunks.y];

        for (int x = 0; x < InitialChunks.x; x++)
        {
            for (int z = 0; z < InitialChunks.y; z++)
            {
                LoadChunk(new int2(x, z));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        int2 steppedPos = new int2(Mathf.FloorToInt(transform.position.x / Spacing), Mathf.FloorToInt(transform.position.z / Spacing));
        LoadChunk(steppedPos);

    }

    private void LoadChunk(int2 pos)
    {
        if (!chunks.Contains(pos))
        {
            chunks.Add(pos);
            VoxelChunk newChunk = Instantiate(ChunkPrefab).GetComponent<VoxelChunk>(); ;
            voxelChunks[pos.x, pos.y] = newChunk;
            newChunk.ChunkCoord = pos;
            newChunk.ChunkLoader = this;
            newChunk.transform.position = new Vector3(pos.x, 0, pos.y) * Spacing;
        }
    }

    public VoxelChunk GetAdjacentChunk(int2 pos, int2 dir)
    {
        return voxelChunks[pos.x + dir.x, pos.y + dir.y];
    }
}
