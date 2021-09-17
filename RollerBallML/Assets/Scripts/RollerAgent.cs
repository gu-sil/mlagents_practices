using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RollerAgent : Agent
{
    [Header("Learning Factors")]
    [SerializeField] private float randomPositionDeltaX = 4f;
    [SerializeField] private float fixedPositionY = 0.5f;
    [SerializeField] private float randomPositionDeltaZ = 4f;
    [SerializeField] private float forcePerAction = 50.0f;
    [SerializeField] private float noMovePanalty = -0.001f;
    [SerializeField] private float resultColorRemainTime = 0.2f; 

    [Header("Objects")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material goodMaterial;
    [SerializeField] private Material badMaterial;

    private Transform transform;
    private Rigidbody rigidbody;

    public override void Initialize()
    {
        transform = GetComponent<Transform>();
        rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(
            Random.Range(-randomPositionDeltaX, randomPositionDeltaX),
            fixedPositionY,
            Random.Range(-randomPositionDeltaZ, randomPositionDeltaZ));

        targetTransform.localPosition = new Vector3(
            Random.Range(-randomPositionDeltaX, randomPositionDeltaX),
            fixedPositionY,
            Random.Range(-randomPositionDeltaZ, randomPositionDeltaZ));
        StartCoroutine(RevertPlatformDefaultMaterial());
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(targetTransform.localPosition); // 3 (x, y, z)
        sensor.AddObservation(transform.localPosition); // 3 (x, y, z)
        sensor.AddObservation(rigidbody.velocity.x); // 1 (x)
        sensor.AddObservation(rigidbody.velocity.z); // 1 (z)
    }

    // v[0] : up, down
    // v[1] : left, right
    public override void OnActionReceived(float[] vectorAction)
    {
        float v = Mathf.Clamp(vectorAction[0], -1.0f, 1.0f);
        float h = Mathf.Clamp(vectorAction[1], -1.0f, 1.0f);
        Vector3 direction = (Vector3.forward * v) + (Vector3.right * h);
        rigidbody.AddForce(direction.normalized * forcePerAction);

        SetReward(noMovePanalty);
    }

    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = Input.GetAxis("Vertical");
        actionsOut[1] = Input.GetAxis("Horizontal");
        Debug.Log($"[0]={actionsOut[0]} [1]={actionsOut[1]}");
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("DeadZone"))
        {
            platformRenderer.material = badMaterial;
            SetReward(-1.0f);
            EndEpisode();
        }

        if (collision.collider.CompareTag("Target"))
        {
            platformRenderer.material = goodMaterial;
            SetReward(+1.0f);
            EndEpisode();
        }
    }

    private IEnumerator RevertPlatformDefaultMaterial()
    {
        yield return new WaitForSeconds(resultColorRemainTime);
        platformRenderer.material = defaultMaterial;
    }
}
