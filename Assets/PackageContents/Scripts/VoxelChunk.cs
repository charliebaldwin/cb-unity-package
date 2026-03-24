using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using VInspector;
using static Perlin;
using static UnityEditor.PlayerSettings;

public class VoxelChunk : MonoBehaviour
{
    public static bool DrawDebugs = false;

    public Vector3Int Size3D = new Vector3Int(16,32,16);
    private int bufferSizeMult = 24;
    ComputeBuffer cBuffer;
    ComputeBuffer vBuffer;
    ComputeBuffer nBuffer; 
    ComputeBuffer tBuffer;
    ComputeBuffer voxelBuffer;
    

    public int2 ChunkCoord;
    public ChunkLoader ChunkLoader;
    public RenderTexture voxelTex;
    public RenderTexture testTex;

    public int[,,] voxelData = new int[1,1,1];
    
    public Vector3 NoiseTranslate = Vector3.zero;
    public float NoiseScale = 0.1f;
    public float NoiseThreshold = 0.5f;

    public ComputeShader Compute;


    private MeshFilter meshFilter;
    private Mesh mesh;
    private BoxCollider boxCollider;

    private Vector3 lastPos = Vector3.zero;
    private float lastScale = 0f;
    private float lastThresh = 0f;
    private Vector3Int lastSize = Vector3Int.zero;

    private Vector3 tempOrigin = Vector3.zero;
    private Vector3 tempDirection = Vector3.forward;
    private List<Vector4> tempCubes = new List<Vector4>();

    private VoxelChunk adjacentChunkNX;
    private VoxelChunk adjacentChunkPX;
    private VoxelChunk adjacentChunkNZ;
    private VoxelChunk adjacentChunkPZ;

    private IEnumerator computeReadCoroutine;
    public float BufferReadDelay = 0.5f;


    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        boxCollider = GetComponent<BoxCollider>();
        voxelTex = new RenderTexture(voxelTex);

        lastPos = transform.position;
        lastScale = NoiseScale;
        lastThresh = NoiseThreshold;
        lastSize = Size3D;

        boxCollider.size = new Vector3(Size3D.x, Size3D.y, Size3D.z);
        boxCollider.center = boxCollider.size * 0.5f - new Vector3(0.5f, 0.5f, 0.5f);
    }


    private void Start()
    {
        VoxelNoise(Compute);
        GenerateMeshCompute(Compute);

    }

    private void OnDrawGizmos()
    {
        if (DrawDebugs)
        {
            Gizmos.color = Color.green;


            Gizmos.color = Color.white;
            Gizmos.DrawRay(tempOrigin, 100f * tempDirection);
            foreach (Vector4 v in tempCubes)
            {
                if (v.w == 1.0f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(new Vector3(v.x, v.y, v.z), Vector3.one);
                }
                else
                {
                    Gizmos.color = Color.white;
                }
                Gizmos.DrawCube(new Vector3(v.x, v.y, v.z), Vector3.one);
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(tempOrigin, 0.5f);
        }

        //for (int x = 0; x < Size3D.x; x++)
        //{
        //    for (int z = 0; z < Size3D.z; z++)
        //    {
        //        if (threadTest[x, z] == 1)
        //        {
        //            Gizmos.DrawCube(transform.position + new Vector3(x, 0f, z), Vector3.one);
        //        }
        //    }
        //}
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {
                    if (voxelData[x,y,z] > 0)
                    {
                        switch (voxelData[x, y, z])
                        {
                            case 1:
                                Gizmos.color = Color.red; break;
                            case 2:
                                Gizmos.color = Color.green; break;
                            case 3:
                                Gizmos.color = Color.blue; break;
                        }

                      //  Gizmos.DrawCube(new Vector3(x, y,z) + transform.position, Vector3.one);
                    }

                }
            }
            
        }
    }

    private void Update()
    {
        Size3D.y = Mathf.Clamp(Size3D.y, 1, 128);

        if (transform.position != lastPos || NoiseScale != lastScale || lastThresh != NoiseThreshold || lastSize != Size3D)
        {
            lastPos = transform.position;
            lastScale = NoiseScale;
            lastThresh = NoiseThreshold;
            lastSize = Size3D;

            //VoxelNoise(Compute);
            GenerateMeshCompute(Compute);

            boxCollider.size = new Vector3(Size3D.x, Size3D.y, Size3D.z);
            boxCollider.center = boxCollider.size * 0.5f - new Vector3(0.5f,0.5f,0.5f);
        }
        

    }
    private void LateUpdate()
    {
        //vBuffer.Release();
        //nBuffer.Release();
        //cBuffer.Release();

    }


    private void VoxelNoise(ComputeShader compute)
    {

        int kernel = compute.FindKernel("GenerateVoxels");

        voxelBuffer = new ComputeBuffer(Size3D.x * Size3D.y * Size3D.z, sizeof(int));

        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetVector("TranslateNoise", transform.position * NoiseScale);
        compute.SetFloat("Scale", NoiseScale);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.SetFloat("Threshold", NoiseThreshold);

        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        int[] vData = new int[Size3D.x * Size3D.y * Size3D.z];
        voxelBuffer.GetData(vData);
        voxelData = FlatTo3DArray(vData, Size3D);

    }

    private void GenerateMeshCompute(ComputeShader compute)
    {
        int kernel = compute.FindKernel("ComputeMesh");

        int size3d = Size3D.x * Size3D.y * Size3D.z;

        vBuffer = new ComputeBuffer(bufferSizeMult * size3d, 3 * sizeof(float));
        nBuffer = new ComputeBuffer(bufferSizeMult * size3d, 3 * sizeof(float));
        cBuffer = new ComputeBuffer(bufferSizeMult * size3d, 4 * sizeof(float));
        tBuffer = new ComputeBuffer(bufferSizeMult * size3d, 2 * sizeof(float));


        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetFloat("Threshold", NoiseThreshold);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 1.0f));

        compute.SetBuffer(kernel, "Vertices", vBuffer);
        compute.SetBuffer(kernel, "Normals", nBuffer);
        compute.SetBuffer(kernel, "Colors", cBuffer);
        compute.SetBuffer(kernel, "TexCoords", tBuffer);

        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        computeReadCoroutine = BufferReadTimer(BufferReadDelay);
        StartCoroutine(computeReadCoroutine); 
    }

    private IEnumerator BufferReadTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        ReadBufferData();
    }

    private void ReadBufferData()
    {
        int size3d = Size3D.x * Size3D.y * Size3D.z;
        Vector3[] vData = new Vector3[bufferSizeMult * size3d];
        Vector3[] nData = new Vector3[bufferSizeMult * size3d];
        Color[] cData = new Color[bufferSizeMult * size3d];
        Vector2[] tData = new Vector2[bufferSizeMult * size3d];

        vBuffer.GetData(vData);
        nBuffer.GetData(nData);
        cBuffer.GetData(cData);
        tBuffer.GetData(tData);

        vData = TrimArrayVec3(vData);
        nData = TrimArrayVec3(nData);
        cData = TrimArrayColor(cData);
        tData = TrimArrayVec2(tData);

        meshFilter.mesh = null;
        mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = vData;
        mesh.uv = tData;
        mesh.normals = nData;
        mesh.colors = cData;
        mesh.triangles = GenerateIndices(vData.Length);
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;

        //threadTestBuffer.Release();

        Debug.Log($"Final mesh vertex count: {meshFilter.mesh.vertexCount}");

        //vBuffer.Release();
        //nBuffer.Release();
        //cBuffer.Release();
        //tBuffer.Release();
    }

    private Vector3[] TrimArrayVec3(Vector3[] array)
    {
        List<Vector3> trimmedList = new List<Vector3>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != null && array[i] != Vector3.zero)
            {
                trimmedList.Add(array[i]);
            }
        }
        return trimmedList.ToArray();
    }
    private Vector2[] TrimArrayVec2(Vector2[] array)
    {
        List<Vector2> trimmedList = new List<Vector2>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != null && array[i] != Vector2.zero)
            {
                trimmedList.Add(array[i]);
            }
        }
        return trimmedList.ToArray();
    }
    private T[] TrimArray<T>(T[] array)
    {
        List<T> trimmedList = new List<T>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != null )
            {
                trimmedList.Add(array[i]);
            }
        }
        return trimmedList.ToArray();
    }
    private Color[] TrimArrayColor(Color[] array)
    {
        List<Color> trimmedList = new List<Color>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != null && array[i].a != 0.0f)
            {
                trimmedList.Add(array[i]);
            }
        }
        return trimmedList.ToArray();
    }

    private int[] GenerateIndices(int vertexCount)
    {
        int[] result = new int[(vertexCount / 4) * 6];
        for (int i=0; i < vertexCount/4 - 0; i++)
        {
            result[i * 6 + 0] = i * 4 + 0;
            result[i * 6 + 1] = i * 4 + 1;
            result[i * 6 + 2] = i * 4 + 2;
            result[i * 6 + 3] = i * 4 + 0;
            result[i * 6 + 4] = i * 4 + 2;
            result[i * 6 + 5] = i * 4 + 3;

            //Debug.Log($"i={i}, tris are: {result[i * 6 + 0]}, {result[i * 6 + 1]}, {result[i * 6 + 2]}, {result[i * 6 + 3]}, {result[i * 6 + 4]}, {result[i * 6 + 5]}");
        }
        return result;
    }

    private int[,,] FlatTo3DArray(int[] flat, Vector3Int dimensions)
    {
        int[,,] result = new int[dimensions.x, dimensions.y, dimensions.z];
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    result[x, y, z] = flat[x + dimensions.x * y + dimensions.x * dimensions.y * z];
                }
            }
        }
        return result;
    }
    private int[] ThreeDToFlatArray(int[,,] threeDarray, Vector3Int dimensions)
    {
        int[] result = new int[dimensions.x * dimensions.y * dimensions.z];
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    result[x + dimensions.x * y + dimensions.x * dimensions.y * z] = threeDarray[x,y,z];
                }
            }
        }
        return result;
    }

    public VoxelHitData VoxelRaycast(Vector3 origin, Vector3 direction)
    {
        tempOrigin = origin;
        tempDirection = direction;
        tempCubes = new List<Vector4>();

        float stepDist = 0.05f;
        int stepCount = 300;

        VoxelHitData hitData = new VoxelHitData(false);

        Vector3 stepPos = origin;
        Vector3Int lastVoxPos = WorldPosToVoxel(stepPos + 0.5f * direction);

        for (int i = 0; i < stepCount; i++)
        {

            Vector3Int voxPos = WorldPosToVoxel(stepPos);
            stepPos = stepPos + direction * stepDist;
            Debug.Log($"({ChunkCoord.x},{ChunkCoord.y}) - i:{i}, worldVoxPos={voxPos + transform.position}, stepPos={stepPos}");

            // Debug.Log($"checking voxel {voxPos}");

            if (IsPosInGridBounds(voxPos, Size3D))
            {
                if (voxelData[voxPos.x, voxPos.y, voxPos.z] > 0)
                {
                    //return new Vector3(voxPos.x, voxPos.y, voxPos.z) + transform.position;
                    tempCubes.Add(new Vector4(voxPos.x, voxPos.y, voxPos.z, 1.0f) + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0.0f));

                    //if (lastVoxPos - voxPos != Vector3.zero)
                    hitData.hitNormal = lastVoxPos - voxPos;

                    hitData.didHit = true;
                    hitData.hitPos = stepPos;
                    hitData.localVoxelPos = voxPos;
                    hitData.worldVoxelPos = voxPos + transform.position;
                    //DeleteVoxel(Compute, voxPos);
                    Debug.Log($"hit! at {hitData.worldVoxelPos}, normal={hitData.hitNormal}");
                    return hitData;

                }
                else
                {
                    tempCubes.Add(new Vector4(voxPos.x, voxPos.y, voxPos.z, 0.0f) + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0.0f));
                }
            }
            else
            {
                // ray is outside bounds
                hitData.didHit = false;
                hitData.hitPos = stepPos;
                Debug.Log($"miss! at {hitData.worldVoxelPos}, normal={hitData.hitNormal}");
                return hitData;

            }
            stepPos = stepPos + direction * stepDist;
            hitData.hitPos = stepPos;
            lastVoxPos = voxPos;
            //return hitData;
        }
        return hitData;
    }

    public void BreakBlock(Vector3 worldPosition)
    {
        DeleteVoxel(Compute, WorldPosToVoxel(worldPosition));
    }
    public void PlaceBlock(Vector3 worldPosition, int blockType)
    {
        Vector3Int position = WorldPosToVoxel(worldPosition);
        AddVoxel(Compute, position, blockType); 

        //Vector3Int position = WorldPosToVoxel(worldPosition);
        //if (IsPosInGridBounds(position + normal, Size3D))
        //{
        //    AddVoxel(Compute, position + normal, blockType);
        //} else
        //{
        //    VoxelChunk adjacentChunk = ChunkLoader.GetAdjacentChunk(ChunkCoord, new int2(normal.x, normal.z));
        //    adjacentChunk.PlaceBlock(new Vector3Int(Mathf.Abs(position.x +  Size3D.x * normal.x), position.y, Mathf.Abs(position.z + Size3D.z * normal.z)), normal, blockType);
        //}
    }
    private void DeleteVoxel (ComputeShader compute, Vector3Int voxPosition)
    {
        voxelData[voxPosition.x, voxPosition.y, voxPosition.z] = 0;
        voxelBuffer.SetData(ThreeDToFlatArray(voxelData, Size3D));

        GenerateMeshCompute(Compute);
    }
    private void AddVoxel (ComputeShader compute, Vector3Int voxPosition, int blockType)
    {
        if (voxelData[voxPosition.x, voxPosition.y, voxPosition.z] == 0)
        {
            voxelData[voxPosition.x, voxPosition.y, voxPosition.z] = blockType;
            voxelBuffer.SetData(ThreeDToFlatArray(voxelData, Size3D));

            GenerateMeshCompute(Compute);
        }
    }

    public int LookupVoxel(Vector3 worldPos)
    {
        Vector3Int voxPos = WorldPosToVoxel(worldPos);
        if (IsPosInGridBounds(voxPos, Size3D))
        {
            return voxelData[voxPos.x, voxPos.y, voxPos.z];
        } else {
            return 0;
        }
    }

    private Vector3Int WorldPosToVoxel(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        Vector3Int result = new Vector3Int(Mathf.RoundToInt(localPos.x), Mathf.RoundToInt(localPos.y), Mathf.RoundToInt(localPos.z));
        return result;
    }

    private bool IsPosInGridBounds(Vector3Int pos, Vector3Int size)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.z >= 0 && pos.x < size.x && pos.y < size.y && pos.z < size.z;
    }

   


}

public struct VoxelHitData
{
    public bool didHit;
    public int blockID;
    public Vector3Int localVoxelPos;
    public Vector3 worldVoxelPos;
    public Vector3 hitPos;
    public Vector3Int hitNormal;

    public VoxelHitData(bool didHit)
    {
        this.didHit = didHit;
        blockID = 0;
        localVoxelPos = Vector3Int.zero;
        worldVoxelPos = Vector3.zero;
        hitPos = Vector3.zero;
        hitNormal = Vector3Int.up;
    }
}
