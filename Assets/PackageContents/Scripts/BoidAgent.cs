using UnityEngine;
using VInspector;

public class BoidAgent : MonoBehaviour
{
    public BoidAgentData data;
    public bool drawDebug;

    [Foldout("Boid Settings")]
    public float speed = 1f;
    public float steerForce = 0.5f;
    public float dataLerp = 0.05f;

    public float viewRadius = 5f;
    public float avoidRadius = 1f;

    public float alignWeight = 2f;
    public float cohesionWeight = 2f;
    public float avoidWeight = 2f;
    public float goalWeight = 2f;
    public float containerWeight = 1f;

    public Transform goalTransform;

    public Collider container;
    [EndFoldout]

    private Rigidbody rb;

    private Vector3 currentFlockCenter;
    private Vector3 currentFlockDirection;
    private Vector3 currentAvoidDirection;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        MoveBoid();
    }

    public BoidAgentData InitData()
    {
        data = new BoidAgentData();
        data.position = transform.position;
        data.direction = transform.forward;
        data.viewRadius = viewRadius;
        data.avoidRadius = avoidRadius;

        currentFlockCenter = transform.position;
        currentFlockDirection = transform.forward;
        currentAvoidDirection = transform.up;


        return data;
    }

    public BoidAgentData GetRefreshedData()
    {
        data.position = transform.position;
        data.direction = transform.forward;

        return data;
    }

    [OnValueChanged("viewRadius", "avoidRadius")]
    public void RefreshSettings()
    {
        data.viewRadius = viewRadius;
        data.avoidRadius = avoidRadius;
    }

    public void UpdateData(BoidAgentData newData)
    {
        data = newData;
    }


    public void MoveBoid()
    {
        //currentFlockCenter = Vector3.Lerp(currentFlockCenter, data.flockCenter, dataLerp);
        //currentFlockDirection = Vector3.Slerp(currentFlockDirection, data.flockCenter, dataLerp);
        //currentAvoidDirection = Vector3.Slerp(currentAvoidDirection, data.flockCenter, dataLerp);

        Vector3 acceleration = transform.forward;

        if (goalTransform != null)
        {
            Vector3 towardsGoal = (goalTransform.position - transform.position).normalized;
            acceleration += towardsGoal * goalWeight;
        }
        if (data.numFlockmates > 0)
        {
            Vector3 towardsFlockCenter = (data.flockCenter - transform.position).normalized;

            acceleration += data.avoidDirection * avoidWeight;
            acceleration += data.flockDirection * alignWeight;
            acceleration += towardsFlockCenter * cohesionWeight;
        }
        if (container != null)
        {
            Vector3 center = container.bounds.center;
            Vector3 checkDir = center - transform.position;
            Ray checkRay = new Ray(transform.position, checkDir);
            bool didHitContainer = Physics.Raycast(checkRay, checkDir.magnitude);

            if (didHitContainer)
            {
                Vector3 returnDir = container.ClosestPoint(transform.position) - transform.position;

                acceleration += returnDir * containerWeight;
            }
        }

        //rb.rotation = Quaternion.RotateTowards(rb.rotation, Quaternion.FromToRotation(Vector3.forward, acceleration.normalized), steerForce);
        //rb.AddForce(transform.forward * speed * Time.deltaTime);
        
        transform.forward = Vector3.Slerp(transform.forward, acceleration.normalized, steerForce);
        transform.position += transform.forward * speed * Time.deltaTime;
        
    }

    public void OnDrawGizmos()
    {
        if (drawDebug)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + data.flockDirection.normalized);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + data.avoidDirection.normalized);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + (goalTransform.position - transform.position).normalized);
        }
    }
}
