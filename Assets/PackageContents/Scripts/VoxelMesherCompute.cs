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

    private Vector3 tempOrigin;
    private Vector3 tempDirection;

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

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(tempOrigin, 100f*    tempDirection);
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

    public void VoxelRaycast(Vector3 origin, Vector3 direction)
    {
        tempOrigin = origin;
        tempDirection = direction;

        Texture3D tex = new Texture3D(Size3D.x, Size3D.y, Size3D.z, UnityEngine.Experimental.Rendering.DefaultFormat.LDR, 0, voxelTex.GetNativeTexturePtr().ToInt32());

        for (int x = 0; x < Size3D.x; x++)
        {
            for(int y = 0; y < Size3D.y; y++)
            {
                for (int z = 0; z < Size3D.z; z++)
                {
                   // Debug.Log(tex.GetPixel(x, y, z));
                }
            }
        }
    }

   
}
