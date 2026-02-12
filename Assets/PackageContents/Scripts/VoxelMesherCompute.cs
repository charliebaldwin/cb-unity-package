using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VInspector;
using static Perlin;
using static UnityEditor.PlayerSettings;

public class VoxelMesherCompute : MonoBehaviour
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

    public int[,,] voxelData;

    public Vector3 NoiseTranslate = Vector3.zero;
    public float NoiseScale = 0.1f;
    public float NoiseThreshold = 0.5f;

    public ComputeShader Compute;


    private MeshFilter meshFilter;
    private Mesh mesh;
    private BoxCollider boxCollider;

    private Vector3 lastPos;
    private float lastScale;
    private float lastThresh;
    private Vector3Int lastSize;

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
    }


    private void Start()
    {
        VoxelNoise(Compute);
        GenerateMeshCompute(Compute);

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int x = 0; x < Size3D.x; x++)
        {
            for (int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {
                    if (voxelData[x, y, z] == 1)
                    {
                       // Gizmos.DrawCube(transform.position + new Vector3(x, y, z), Vector3.one);
                    }
                }
            }
        }

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
           // Gizmos.DrawCube(new Vector3(v.x, v.y, v.z), Vector3.one);
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

        voxelTex.Release();

        voxelTex.height = Size;
        voxelTex.width = Size;
        voxelTex.volumeDepth = Size;

        voxelTex.Create();

        voxelBuffer = new ComputeBuffer(Size3D.x * Size3D.y * Size3D.z, sizeof(int));


        compute.SetTexture(kernel, "VoxelTex", voxelTex);
        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetVector("TranslateNoise", transform.position * NoiseScale);
        compute.SetFloat("Scale", NoiseScale);
        compute.SetFloat("Size", Size);
        compute.SetVector("Size3D", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.SetFloat("Threshold", 0.2f);
        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

        int[] vData = new int[Size3D.x * Size3D.y * Size3D.z];
        voxelBuffer.GetData(vData);
        voxelData = FlatTo3DArray(vData);

        //for (int y = 0; y < Size3D.y; y++)
        //{
        //    Debug.Log($"y:{y} = {voxelData[0, y, 0]}");
        //}

    }

    private void GenerateMeshCompute(ComputeShader compute)
    {
        int size3d = Size3D.x * Size3D.y * Size3D.z;
        vBuffer = new ComputeBuffer(24 * size3d, 3 * sizeof(float));
        nBuffer = new ComputeBuffer(24 * size3d, 3 * sizeof(float));
        cBuffer = new ComputeBuffer(24 * size3d, 4 * sizeof(float));
        tBuffer = new ComputeBuffer(24 * size3d, 2 * sizeof(float));

        int kernel = compute.FindKernel("ComputeMesh");

        compute.SetBuffer(kernel, "Vertices", vBuffer);
        compute.SetBuffer(kernel, "Normals", nBuffer);
        compute.SetBuffer(kernel, "Colors", cBuffer);
        compute.SetBuffer(kernel, "TexCoords", tBuffer);
        compute.SetBuffer(kernel, "Voxels", voxelBuffer);
        compute.SetInt("Size", Size);
        compute.SetFloat("Threshold", NoiseThreshold);
        compute.SetVector("Size3D", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.SetTexture(kernel, "VoxelTex", voxelTex);

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

        meshFilter = GetComponent<MeshFilter>();
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

        //Debug.Log($"vData length: {vData.Length}");

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

    private int[,,] TextureToArray(RenderTexture rt)
    {
        int[,,] result = new int[Size3D.x, Size3D.y, Size3D.z];
        Texture3D tex3D = new Texture3D(Size3D.x, Size3D.y, Size3D.z, TextureFormat.ARGB32, true);
        tex3D.CopyPixels(rt);
        Color[] pixels = tex3D.GetPixels(0);
        for(int x=0; x < Size3D.x; x++)
        {
            for (int y=0; y < Size3D.y; y++)
            {
                for (int z=0; z < Size3D.z; z++)
                {
                    result[x, y, z] = pixels[x + Size3D.x * y + Size3D.x * Size3D.y * z].r > NoiseThreshold ? 1 : 0;
                }
            }
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

    public Vector3 VoxelRaycast(Vector3 origin, Vector3 direction)
    {
        tempOrigin = origin;
        tempDirection = direction;
        tempCubes = new List<Vector4>();

        float stepDist = 0.5f;
        int stepCount = 20;

        bool hit = false;
        Vector3 hitPos = Vector3.zero;

        Vector3 stepPos = origin - transform.position;
        Debug.Log($"raycast origin: {stepPos}");

        for (int i = 0; i < stepCount; i++)
        {
            Vector3Int voxPos = new Vector3Int(Mathf.RoundToInt(stepPos.x), Mathf.RoundToInt(stepPos.y), Mathf.RoundToInt(stepPos.z));
            stepPos = stepPos + direction * stepDist;

            if (voxelData[voxPos.x, voxPos.y, voxPos.z] == 1)
            {
                //return new Vector3(voxPos.x, voxPos.y, voxPos.z) + transform.position;
                tempCubes.Add(new Vector4(voxPos.x, voxPos.y, voxPos.z, 1.0f) + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0.0f));

            }
            else
            {
                tempCubes.Add(new Vector4(voxPos.x, voxPos.y, voxPos.z, 0.0f) + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0.0f));
            }
            stepPos = stepPos + direction * stepDist;
        }
        return Vector3.zero;
    }

   
}
