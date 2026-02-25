using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VInspector;
using static Perlin;
using static UnityEditor.PlayerSettings;

public class VoxelChunk : MonoBehaviour
{
    public int Size = 3;
    public Vector3Int Size3D = new Vector3Int(16,32,16);
    ComputeBuffer cBuffer;
    ComputeBuffer vBuffer;
    ComputeBuffer nBuffer; 
    ComputeBuffer tBuffer;
    ComputeBuffer voxelBuffer;

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
        Gizmos.color = Color.green;
        //for (int x = 0; x < Size3D.x; x++)
        //{
        //    for (int y = 0; y < Size3D.y; y++)
        //    {
        //        for (int z = 0; z < Size3D.z; z++)
        //        {
        //            if (voxelData[x, y, z] == 1)
        //            {
        //               // Gizmos.DrawCube(transform.position + new Vector3(x, y, z), Vector3.one);
        //            }
        //        }
        //    }
        //}

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

    private void Update()
    {
        Size3D.y = Mathf.Clamp(Size3D.y, 1, 32);

        if (transform.position != lastPos || NoiseScale != lastScale || lastThresh != NoiseThreshold || lastSize != Size3D)
        {
            lastPos = transform.position;
            lastScale = NoiseScale;
            lastThresh = NoiseThreshold;
            lastSize = Size3D;

            VoxelNoise(Compute);
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

        int kernel = compute.FindKernel("GenerateGrid");

        //voxelTex.Release();

        //voxelTex.height = Size;
        //voxelTex.width = Size;
        //voxelTex.volumeDepth = Size;

        //voxelTex.Create();

        voxelBuffer = new ComputeBuffer(Size3D.x * Size3D.y * Size3D.z, sizeof(int));


        //compute.SetTexture(kernel, "VoxelTex", voxelTex);
        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetVector("TranslateNoise", transform.position * NoiseScale);
        compute.SetFloat("Scale", NoiseScale);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.SetFloat("Threshold", 0.2f);
        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        int[] vData = new int[Size3D.x * Size3D.y * Size3D.z];
        voxelBuffer.GetData(vData);
        voxelData = FlatTo3DArray(vData);

    }

    private void GenerateMeshCompute(ComputeShader compute)
    {
        int size3d = Size3D.x * Size3D.y * Size3D.z;
        vBuffer = new ComputeBuffer(24 * size3d, 3 * sizeof(float));
        nBuffer = new ComputeBuffer(24 * size3d, 3 * sizeof(float));
        cBuffer = new ComputeBuffer(24 * size3d, 4 * sizeof(float));
        tBuffer = new ComputeBuffer(24 * size3d, 2 * sizeof(float));

        int kernel = compute.FindKernel("ComputeMesh");

        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetFloat("Threshold", NoiseThreshold);
        compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));

        compute.SetBuffer(kernel, "Vertices", vBuffer);
        compute.SetBuffer(kernel, "Normals", nBuffer);
        compute.SetBuffer(kernel, "Colors", cBuffer);
        compute.SetBuffer(kernel, "TexCoords", tBuffer);



        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        Vector3[] vData = new Vector3[24 * size3d];
        Vector3[] nData = new Vector3[24 * size3d];
        Color[] cData = new Color[24 * size3d];
        Vector2[] tData = new Vector2[24 * size3d];
 
        vBuffer.GetData(vData);
        nBuffer.GetData(nData);
        cBuffer.GetData(cData);
        tBuffer.GetData(tData);

       // vData = TrimArrayVec3(vData);
       // nData = TrimArrayVec3(nData);
       // cData = TrimArrayColor(cData);

        meshFilter.sharedMesh = null;
        mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = vData;
        mesh.uv = tData;
        mesh.normals = nData;
        mesh.colors = cData;
        mesh.triangles = GenerateIndices(vData.Length);
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;


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
        for (int i=0; i < vertexCount/4; i++)
        {
            result[i * 6 + 0] = i * 4 + 0;
            result[i * 6 + 1] = i * 4 + 1;
            result[i * 6 + 2] = i * 4 + 2;
            result[i * 6 + 3] = i * 4 + 0;
            result[i * 6 + 4] = i * 4 + 2;
            result[i * 6 + 5] = i * 4 + 3;
        }
        return result;
    }

    private int[,,] FlatTo3DArray(int[] flat)
    {
        int[,,] result = new int[Size3D.x, Size3D.y, Size3D.z];
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {
                    result[x, y, z] = flat[x + Size3D.x * y + Size3D.x * Size3D.y * z];
                }
            }
        }
        return result;
    }
    private int[] ThreeDToFlatArray(int[,,] threeDarray)
    {
        int[] result = new int[Size3D.x * Size3D.y * Size3D.z];
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {
                    result[x + Size3D.x * y + Size3D.x * Size3D.y * z] = threeDarray[x,y,z];
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

        float stepDist = 0.1f;
        int stepCount = 100;

        VoxelHitData hitData = new VoxelHitData(false);

        Vector3 stepPos = origin;

        for (int i = 0; i < stepCount; i++)
        {
            Vector3Int voxPos = WorldPosToVoxel(stepPos);
            stepPos = stepPos + direction * stepDist;

           // Debug.Log($"checking voxel {voxPos}");

            if (IsPosInGridBounds(voxPos, Size3D))
            {
                if (voxelData[voxPos.x, voxPos.y, voxPos.z] > 0)
                {
                    //return new Vector3(voxPos.x, voxPos.y, voxPos.z) + transform.position;
                    tempCubes.Add(new Vector4(voxPos.x, voxPos.y, voxPos.z, 1.0f) + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0.0f));

                    hitData.didHit = true;
                    hitData.hitPos = stepPos;
                    hitData.localVoxelPos = voxPos;
                    //DeleteVoxel(Compute, voxPos);

                }
                else
                {
                    tempCubes.Add(new Vector4(voxPos.x, voxPos.y, voxPos.z, 0.0f) + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0.0f));
                }
            }
            else
            {
                // ray is outside bounds
                //hitData.didHit = false;
                //hitData.hitPos = stepPos;
                //return hitData;

            }
            stepPos = stepPos + direction * stepDist;
            hitData.hitPos = stepPos;
        }
        return hitData;
    }

    public void BreakBlock(Vector3Int position)
    {
        DeleteVoxel(Compute, position);
    }
    private void DeleteVoxel (ComputeShader compute, Vector3Int voxPosition)
    {
        voxelData[voxPosition.x, voxPosition.y, voxPosition.z] = 0;
        voxelBuffer.SetData(ThreeDToFlatArray(voxelData));
        //int kernel = compute.FindKernel("DeleteVoxel");
        //compute.SetTexture(kernel, "VoxelTex", voxelTex);
        //compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        //compute.SetVector("DeletePos", new Vector4(voxPosition.x, voxPosition.y, voxPosition.z, 0.0f));
        //compute.SetVector("Size", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));


        //compute.Dispatch(kernel, 1, 1, 1);

        //VoxelNoise(Compute);
        GenerateMeshCompute(Compute);


    }

    private Vector3Int WorldPosToVoxel(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        Vector3Int result = new Vector3Int(Mathf.RoundToInt(localPos.x), Mathf.RoundToInt(localPos.y), Mathf.RoundToInt(localPos.z));
        //result.Clamp(new Vector3Int(0, 0, 0), new Vector3Int(Size3D.x - 1, Size3D.y - 1, Size3D.z - 1));
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
    public Vector3Int localVoxelPos;
    public Vector3 hitPos;
    public Vector3 hitNormal;

    public VoxelHitData(bool didHit)
    {
        this.didHit = didHit;
        localVoxelPos = Vector3Int.zero;
        hitPos = Vector3.zero;
        hitNormal = Vector3.up;
    }
}
