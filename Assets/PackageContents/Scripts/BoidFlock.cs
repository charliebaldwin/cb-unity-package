using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VInspector;

public class BoidFlock : MonoBehaviour
{
    public ComputeShader compute;
    private const int threadGroupSize = 1024;

    public List<BoidAgent> agents;

    [Foldout("Spawn on Start")]
    public BoidAgent agentPrefab;
    public int spawnCount;
    public float spawnRadius;
    [EndFoldout]

    void Start()
    {
        SpawnBoids(spawnCount, transform.position, spawnRadius);
    }

    void Update()
    {
        if (agents != null && agents.Count > 0)
        {
            int numBoids = agents.Count;
            BoidAgentData[] boidData = GetAgentsData();

            ComputeBuffer boidBuffer = new ComputeBuffer(numBoids, BoidAgentData.Size);
            boidBuffer.SetData(boidData);
            compute.SetBuffer(0, "boids", boidBuffer);

            compute.SetInt("numBoids", numBoids);

            int threadGroups = Mathf.CeilToInt((float) numBoids / (float) threadGroupSize);
            compute.Dispatch(0, threadGroups, 1, 1);
            
            boidBuffer.GetData(boidData);
            UpdateAgentsData(boidData);

            boidBuffer.Release();
        }
    }

    private void SpawnBoids(int count, Vector3 center, float radius)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = center + Random.insideUnitSphere * radius;
            BoidAgent newAgent = Instantiate(agentPrefab, spawnPos, Random.rotation);
            newAgent.transform.parent = transform;
            newAgent.InitData();
            agents.Add(newAgent);
        }
    }
    private BoidAgentData[] GetAgentsData()
    {
        BoidAgentData[] agentsData = new BoidAgentData[agents.Count];
        for (int i = 0; i < agents.Count; i++)
        {
            agentsData[i] = agents[i].GetRefreshedData();
        }
        return agentsData;
    }

    private void UpdateAgentsData(BoidAgentData[] newData)
    {
        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].UpdateData(newData[i]);
        }
    }
}
