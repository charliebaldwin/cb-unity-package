using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ChunkRaycast : MonoBehaviour
{
    public LayerMask mask;

    private Vector3 debugRayStart;
    private Vector3 debugRayEnd;
    private Vector3 hitVoxPos;
    private bool didHitVox = false;
    private List<Vector3> colliderEnterPoints = new List<Vector3>();
    private List<Vector3> colliderExitPoints = new List<Vector3>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(debugRayStart, debugRayEnd);

        Gizmos.color = Color.blue;
        foreach (Vector3 p in colliderEnterPoints)
        {
            Gizmos.DrawSphere(p, 0.25f);
        }

        Gizmos.color = Color.red;
        foreach (Vector3 p in colliderExitPoints)
        {
            Gizmos.DrawSphere(p, 0.25f);
        }


        if (didHitVox)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(debugRayEnd, hitVoxPos);
            Gizmos.DrawSphere(hitVoxPos, 0.5f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        DoRaycast2();

        


    }

    private void DoRaycast1()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit[] hits;
            hits = Physics.RaycastAll(new Ray(transform.position, transform.forward), 999f, mask);
            foreach (RaycastHit hit in hits)
            {
                hit.collider.gameObject.GetComponent<VoxelChunk>().VoxelRaycast(hit.point, transform.forward);
            }
        }
    }

    private void DoRaycast2()
    {

        if (Input.GetMouseButtonDown(0))
        {
            colliderEnterPoints = new List<Vector3>();
            colliderExitPoints = new List<Vector3>();

            bool complete = false;
            int i = 0;
            Vector3 rayOrigin = transform.position;

            while (!complete && i < 10)
            {
                i++;
                RaycastHit hit;
                VoxelHitData voxelHitData;

                if (Physics.Raycast(new Ray(rayOrigin, transform.forward), out hit, 999f, mask))
                {
                    debugRayStart = transform.position;
                    debugRayEnd = hit.point;
                    colliderEnterPoints.Add(hit.point);
                    

                    VoxelChunk hitChunk = hit.collider.gameObject.GetComponent<VoxelChunk>();
                    voxelHitData = hitChunk.VoxelRaycast(hit.point, transform.forward);
                    rayOrigin = voxelHitData.hitPos;

                    if (voxelHitData.didHit)
                    {
                        hitChunk.BreakBlock(voxelHitData.localVoxelPos);
                        Debug.Log($"hit block on attempt {i}");
                        didHitVox = true;
                        hitVoxPos = voxelHitData.hitPos;
                        debugRayEnd = voxelHitData.hitPos;

                        complete = true;
                        break;
                    } else
                    {
                        colliderExitPoints.Add(voxelHitData.hitPos);
                        debugRayEnd = voxelHitData.hitPos;

                    }
                }
                else
                {
                    Debug.Log($"raycast missed after {i} attempts");
                    didHitVox = false;
                    complete = true;
                }
            }
        }
    }
}
