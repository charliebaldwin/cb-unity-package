using UnityEngine;

public class ChunkRaycast : MonoBehaviour
{
    public LayerMask mask;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
            bool complete = false;
            int i = 0;

            while (!complete && i < 5)
            {
                i++;
                RaycastHit hit;
                VoxelHitData voxelHitData;

                if (Physics.Raycast(new Ray(transform.position, transform.forward), out hit, 999f, mask))
                {
                    VoxelChunk hitChunk = hit.collider.gameObject.GetComponent<VoxelChunk>();
                    voxelHitData = hitChunk.VoxelRaycast(hit.point, transform.forward);

                    if (voxelHitData.didHit)
                        Debug.Log(voxelHitData.localVoxelPos);
                    
                    if (voxelHitData.didHit)
                    {
                        hitChunk.BreakBlock(voxelHitData.localVoxelPos);
                        complete = true;
                        break;
                    }
                }
                else
                {
                    complete = true;
                }
            }
        }
    }
}
