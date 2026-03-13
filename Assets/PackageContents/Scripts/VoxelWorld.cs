using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    public static VoxelWorld Instance { get; private set; }

    public int2 WorldSize = new int2(32, 32);

    public GameObject ChunkPrefab;
    public int Spacing = 8;

    private List<int2> chunks = new List<int2>();
    private VoxelChunk[,] voxelChunks;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        voxelChunks = new VoxelChunk[WorldSize.x, WorldSize.y];
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddChunk(int2 pos)
    {
        if (!chunks.Contains(pos))
        {
            chunks.Add(pos);
            VoxelChunk newChunk = Instantiate(ChunkPrefab).GetComponent<VoxelChunk>();
            voxelChunks[pos.x, pos.y] = newChunk;
            newChunk.ChunkCoord = pos;
            newChunk.transform.position = new Vector3(pos.x, 0, pos.y) * Spacing;
        }
    }
}
