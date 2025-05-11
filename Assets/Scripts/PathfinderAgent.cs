using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PathfinderAgent : Agent
{
    public List<Transform> targetTransforms;
    public float rotationSpeed = 3f;
    public float maxRotationSpeed = 3f;
    public float moveSpeed = 3f;
    public float maxSpeed = 3f;
    private Rigidbody rb;
    private EnvironmentController env;

    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        env = GetComponentInParent<EnvironmentController>();
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);

        foreach (var targetTransform in env.targetTransforms)
            sensor.AddObservation(targetTransform.localPosition - transform.localPosition);

        foreach (var agentTransform in env.agentTransforms)
        {
            if (agentTransform.gameObject == this) 
                continue;
            
            sensor.AddObservation(agentTransform.localPosition - transform.localPosition);
        }

        //if(transform.position.y < -0.1f)
        //{
        //    env.ResetEnvironment();
        //}            
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 이동 (Continuous or Discrete)
        float h = actions.ContinuousActions[0];
        float v = actions.ContinuousActions[1];

        rb.AddForce(v * transform.forward * moveSpeed, ForceMode.Force);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, h * rotationSpeed * Time.deltaTime, 0f));
                
        if(rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Target"))
        {
            env.TouchTarget(other);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("agent"))
        {
            env.TouchAgent();
        }
    }

    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            env.TouchWall();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        float h = Input.GetAxis("Horizontal"); // A/D 또는 좌/우 화살표
        float v = Input.GetAxis("Vertical");   // W/S 또는 상/하 화살표
        continuousActions[0] = h;
        continuousActions[1] = v;
    }
}