using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VInspector;
using static Perlin;

public class VoxelMesherCompute : MonoBehaviour
{
    public int Size = 3;
    public Vector3Int Size3D = new Vector3Int(16,32,16);
    ComputeBuffer cBuffer;
    ComputeBuffer vBuffer;
    ComputeBuffer nBuffer;
    ComputeBuffer tBuffer;

    public RenderTexture voxelTex;
    public RenderTexture testTex;

    public Vector3 NoiseTranslate = Vector3.zero;
    public float NoiseScale = 0.1f;
    public float NoiseThreshold = 0.5f;

    public ComputeShader Compute;


    private MeshFilter meshFilter;
    private Mesh mesh;

    private Vector3 lastPos;
    private float lastScale;
    private float lastThresh;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        voxelTex = new RenderTexture(voxelTex);

        lastPos = transform.position;
        lastScale = NoiseScale;
        lastThresh = NoiseThreshold;
    }


    private void Start()
    {
        VoxelNoise(Compute);
        GenerateMeshCompute(Compute);

    }

    private void Update()
    {
        Size3D.y = Mathf.Clamp(Size3D.y, 4, 32);

        if (transform.position != lastPos || NoiseScale != lastScale || lastThresh != NoiseThreshold)
        {
            lastPos = transform.position;
            lastScale = NoiseScale;
            lastThresh = NoiseThreshold;

            VoxelNoise(Compute);
            GenerateMeshCompute(Compute);
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


        compute.SetTexture(kernel, "Voxels", voxelTex);
        compute.SetVector("TranslateNoise", transform.position * NoiseScale);
        compute.SetFloat("Scale", NoiseScale);
        compute.SetFloat("Size", Size);
        compute.SetVector("Size3D", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.SetFloat("Threshold", 0.2f);
        compute.Dispatch(kernel, Size3D.x, 1, Size3D.z);

    }

    private void GenerateMeshCompute(ComputeShader compute)
    {
        int size3d = Size * Size * Size;
        vBuffer = new ComputeBuffer(24 * size3d, 3 * sizeof(float));
        nBuffer = new ComputeBuffer(24 * size3d, 3 * sizeof(float));
        cBuffer = new ComputeBuffer(24 * size3d, 4 * sizeof(float));
        tBuffer = new ComputeBuffer(24 * size3d, 2 * sizeof(float));

        int kernel = compute.FindKernel("ComputeMesh");

        compute.SetBuffer(kernel, "Vertices", vBuffer);
        compute.SetBuffer(kernel, "Normals", nBuffer);
        compute.SetBuffer(kernel, "Colors", cBuffer);
        compute.SetBuffer(kernel, "TexCoords", tBuffer);
        compute.SetInt("Size", Size);
        compute.SetFloat("Threshold", NoiseThreshold);
        compute.SetVector("Size3D", new Vector4(Size3D.x, Size3D.y, Size3D.z, 0.0f));
        compute.SetTexture(kernel, "Voxels", voxelTex);

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

    private Vector3[] GenerateCubeVertices(Vector3 center, float size)
    {
        float half = size / 2f;
        Vector3[] data = new Vector3[8];
        data[0] = center + new Vector3(-half, -half, -half);
        data[1] = center + new Vector3(-half, -half,  half);
        data[2] = center + new Vector3( half, -half,  half);
        data[3] = center + new Vector3( half, -half, -half);
        data[4] = center + new Vector3(-half,  half, -half);
        data[5] = center + new Vector3(-half,  half,  half);
        data[6] = center + new Vector3( half,  half,  half);
        data[7] = center + new Vector3( half,  half, -half);
        return data;
    }
    private Vector3[] GenerateCubeVerticesSplit(Vector3 center, float size, Connections adj)
    {
        float half = size / 2f;
        int numFaces = adj.SumFaces();
        Vector3[] data = new Vector3[numFaces * 4];
        int i = 0;

        // -X
        if (adj.nx == 1)
        {
            data[i+0] = center + new Vector3(-half, -half, half);
            data[i+1] = center + new Vector3(-half, half, half);
            data[i+2] = center + new Vector3(-half, half, -half);
            data[i+3] = center + new Vector3(-half, -half, -half);
            i+=4;
        }

        // +X
        if (adj.px == 1)
        {
            data[i+0] = center + new Vector3(half, -half, -half);
            data[i+1] = center + new Vector3(half, half, -half);
            data[i+2] = center + new Vector3(half, half, half);
            data[i+3] = center + new Vector3(half, -half, half);
            i += 4;
        }

        // -Y   
        if (adj.ny == 1)
        {
           data[i+0] = center + new Vector3(half, -half, half);
           data[i+1] = center + new Vector3(-half, -half, half);
           data[i+2] = center + new Vector3(-half, -half, -half);
           data[i+3] = center + new Vector3(half, -half, -half);
           i += 4;
        }

        // +Y
        if (adj.py == 1)
        {
            data[i+0] = center + new Vector3(-half, half, -half);
            data[i+1] = center + new Vector3(-half, half, half);
            data[i+2] = center + new Vector3(half, half, half);
            data[i+3] = center + new Vector3(half, half, -half);
            i += 4;
        }

        // -Z
        if (adj.nz == 1)
        {
            data[i+0] = center + new Vector3(-half, half, -half);
            data[i+1] = center + new Vector3(half, half, -half);
            data[i+2] = center + new Vector3(half, -half, -half);
            data[i+3] = center + new Vector3(-half, -half, -half);
            i += 4;
        }

        // +Z
        if (adj.pz == 1)
        {
            data[i+0] = center + new Vector3(half, -half, half);
            data[i+1] = center + new Vector3(half, half, half);
            data[i+2] = center + new Vector3(-half, half, half);
            data[i+3] = center + new Vector3(-half, -half, half);
            i += 4;
        }

        return data;
    }
    private Vector3[] GenerateCubeNormalsSplit(Connections adj)
    {
        int totalFaces = adj.SumFaces();
        Vector3[] normals = new Vector3[4 * totalFaces];
        int i = 0;
        if (adj.nx == 1)
        {
            normals[i + 0] = Vector3.left;
            normals[i + 1] = Vector3.left;
            normals[i + 2] = Vector3.left;
            normals[i + 3] = Vector3.left;
            i += 4;
        }
        if (adj.px == 1)
        {
            normals[i + 0] = Vector3.right;
            normals[i + 1] = Vector3.right;
            normals[i + 2] = Vector3.right;
            normals[i + 3] = Vector3.right;
            i += 4;
        }
        if (adj.ny == 1)
        {
            normals[i + 0] = Vector3.down;
            normals[i + 1] = Vector3.down;
            normals[i + 2] = Vector3.down;
            normals[i + 3] = Vector3.down;
            i += 4;
        }
        if (adj.py == 1)
        {
            normals[i + 0] = Vector3.up;
            normals[i + 1] = Vector3.up;
            normals[i + 2] = Vector3.up;
            normals[i + 3] = Vector3.up;
            i += 4;
        }
        if (adj.nz == 1)
        {
            normals[i + 0] = Vector3.back;
            normals[i + 1] = Vector3.back;
            normals[i + 2] = Vector3.back;
            normals[i + 3] = Vector3.back;
            i += 4;
        }
        if (adj.pz == 1)
        {
            normals[i + 0] = Vector3.forward;
            normals[i + 1] = Vector3.forward;
            normals[i + 2] = Vector3.forward;
            normals[i + 3] = Vector3.forward;
            i += 4;
        }

        return normals;

        //return new Vector3[24] {
        //    Vector3.left, Vector3.left, Vector3.left, Vector3.left,
        //    Vector3.right, Vector3.right, Vector3.right, Vector3.right,
        //    Vector3.down, Vector3.down, Vector3.down, Vector3.down,
        //    Vector3.up, Vector3.up, Vector3.up, Vector3.up,
        //    Vector3.back, Vector3.back, Vector3.back, Vector3.back,
        //    Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward
        //};
    }

    private int[] GenerateCubeIndices(int first)
    {

        int[] indices = new int[36]
        {
            0, 2, 1, 0, 3, 2,
            1, 2, 6, 1, 6, 5,
            2, 3, 7, 2, 7, 6,
            3, 0, 4, 3, 4, 7,
            0, 1, 5, 0, 5, 4,
            4, 5, 6, 4, 6, 7
        };

        for (int i = 0; i < 36; i++)
        {
            indices[i] = first + indices[i];
        }
        return indices;
    }
    private int[] GenerateCubeIndicesSplit(int first, Connections adj)
    {
        int totalFaces = adj.SumFaces();
        int[] indices = new int[6 * totalFaces];
        
        for(int f=0; f<totalFaces; f++)
        {
            indices[6 * f + 0] = first + 4 * f + 0;
            indices[6 * f + 1] = first + 4 * f + 1;
            indices[6 * f + 2] = first + 4 * f + 2;
            indices[6 * f + 3] = first + 4 * f + 0;
            indices[6 * f + 4] = first + 4 * f + 2;
            indices[6 * f + 5] = first + 4 * f + 3;
        }


        //for (int i = 0; i < 36; i++)
        //{
        //    indices[i] = first + indices[i];
        //}

        //int[] indices = new int[36]
        //{
        //    0, 1, 2, 0, 2, 3,
        //    4, 5, 6, 4, 6, 7,
        //    8, 9, 10, 8, 10, 11,
        //    12, 13, 14, 12, 14, 15,
        //    16, 17, 18, 16, 18, 19,
        //    20, 21, 22, 20, 22, 23
        //};

        return indices;
    }

    private Color[] VecArrayToColor(Vector3[] vectors)
    {
        Color[] colors = new Color[vectors.Length];
        for (int i = 0; i < vectors.Length; i++)
        {
            colors[i] = new Color(vectors[i].normalized.x, vectors[i].normalized.y, vectors[i].normalized.z, 1f);
        }
        return colors;
    }
}
