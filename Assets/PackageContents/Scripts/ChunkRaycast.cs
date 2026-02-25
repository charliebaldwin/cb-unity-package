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
}
