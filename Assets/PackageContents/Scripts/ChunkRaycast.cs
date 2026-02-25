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
            RaycastHit hit;
            if (Physics.Raycast(new Ray(transform.position, transform.forward), out hit, 9999f, mask))
            {
                hit.collider.gameObject.GetComponent<VoxelChunk>().VoxelRaycast(hit.point, transform.forward);
            }
        }


    }
}
