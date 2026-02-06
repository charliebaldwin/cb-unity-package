using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VInspector;
using static Perlin;

public class VoxelMesherCompute : MonoBehaviour
{
    public int Size = 3;
    public int[,,] voxels = new int[3, 3, 3]{
        { {1,1,1}, {0,1,0}, {0,0,0} },
        { {1,1,1}, {1,1,1}, {0,1,0} },
        { {1,1,1}, {0,1,0}, {0,0,0} }
    };

    public RenderTexture voxelTex;

    public Vector3 NoiseTranslate = Vector3.zero;
    public float NoiseScale = 0.1f;
    public float NoiseThreshold = 0.5f;

    //public int[,,] voxels = new int[3, 3, 3]{
    //    {
    //        {0,0,0}, {0,0,0}, {0,0,0}
    //    },
    //    {
    //        {0,0,0}, {0,1,0}, {0,0,0}
    //    },
    //    {
    //        {0,0,0}, {0,0,0}, {0,0,0}
    //    }
    //};


    private MeshFilter meshFilter;
    private Mesh mesh;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }


    private void Start()
    {

    }

    private void Update()
    {

    }

    [Button]
    private void GenerateMesh()
    {
        NoiseTranslate.x = transform.position.x / Size; // + 3f * Mathf.Sin(0.4f * Time.time);
        NoiseTranslate.z = transform.position.z / Size; // + 3f * Mathf.Cos(0.4f * Time.time);
        voxels = VoxelNoise();

        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;
        mesh.Clear();

        int currentIndex = 0;

        for (int x = 0; x < Size; x++) {
            for (int y = 0; y < Size; y++) {
                for (int z = 0; z < Size; z++) {
                    
                    if (voxels[x, y, z] == 1)
                    {
                        Connections adj = CheckConnections(x, y, z);

                        if (adj.SumFaces() > 0)
                        {
                            Vector3 p = new Vector3(x, y, z);

                            Vector3[] newVerts = GenerateCubeVerticesSplit(p, 1f, adj);
                            Vector3[] oldVerts = mesh.vertices;
                            Vector3[] combinedVerts = oldVerts.Concat(newVerts).ToArray();

                            Vector3[] newNorms = GenerateCubeNormalsSplit(adj);
                            Vector3[] oldNorms = mesh.normals;
                            Vector3[] combinedNorms = oldNorms.Concat(newNorms).ToArray();

                            int[] newTris = GenerateCubeIndicesSplit(currentIndex, adj);
                            int[] oldTris = mesh.triangles;
                            int[] combinedTris = oldTris.Concat(newTris).ToArray();

                            currentIndex += 4 * adj.SumFaces();

                            mesh.vertices = combinedVerts;
                            mesh.triangles = combinedTris;
                            mesh.normals = combinedNorms;
                            mesh.colors = VecArrayToColor(combinedNorms);

                           // Debug.Log($"voxel ({x},{y},{z}): {newVerts.Length}v, {newNorms.Length}n, {newTris.Length}t");
                        }
                    }

                }
            }
        }
    }



    private Connections CheckConnections(int x, int y, int z) {
        Connections adj = new Connections();

        // -X
        if (x > 0) {
            adj.nx = 1-voxels[x - 1, y, z];
        } else {
            adj.nx = 1;
        }
        // +X
        if (x < Size-1) {
            adj.px = 1 - voxels[x + 1, y, z];
        } else {
            adj.px = 1;
        }

        // -Y
        if (y > 0) {
            adj.ny = 1 - voxels[x, y - 1, z];
        } else {
            adj.ny = 1;
        }
        // +Y
        if (y < Size-1) {
            adj.py = 1 - voxels[x, y + 1, z];
        } else {
            adj.py = 1;
        }

       // -Z
        if (z > 0) {
            adj.nz = 1 - voxels[x, y, z - 1];
        } else {
            adj.nz = 1;
        }
        // +Z
        if (z < Size-1) {
            adj.pz = 1 - voxels[x, y, z + 1];
        } else {
            adj.pz = 1;
        }

        return adj;
    }

    private void VoxelNoise(ComputeShader compute)
    {
        voxelTex = new RenderTexture(Size, Size, Size, RenderTextureFormat.R8);
        voxelTex.dimension = TextureDimension.Tex3D;
        voxelTex.enableRandomWrite = true;

        int kernel = compute.FindKernel("GenerateGrid");

        compute.SetTexture(kernel, "Voxels", voxelTex);
        compute.Dispatch(kernel, Size / 8, Size / 8, Size / 8);

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
