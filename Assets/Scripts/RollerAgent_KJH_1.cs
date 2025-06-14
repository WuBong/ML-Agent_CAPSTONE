using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RollerAgent_KJH_1 : Agent
{
    Rigidbody rBody;
    
    public Transform Target;

    public float agentRunSpeed = 1.5f;
    public float agentRotationSpeed = 200f;

    float episodeCount = 0;
    float initialDistance;
    float previousProgress;

    float MAXmapHalfSizeX = 25f;
    float MAXmapHalfSizeZ = 25f;

    float mapHalfSizeX = 0f;
    float SizeZ = -5f;

    Vector3 lastTargetPosition = new Vector3(-15, 0.3f, -20); // 초기 위치

    public override void Initialize()
    {
        rBody = GetComponent<Rigidbody>();

        if (Target != null)
        {
            if (Target.name == "Target") // 또는 Target.tag == "Target_1"
            {
                lastTargetPosition = new Vector3(-15f, 0.3f, -20f); // Agent 1 시작 위치
            }
            else if (Target.name == "Target_2") // 또는 Target.tag == "Target_2"
            {
                lastTargetPosition = new Vector3(15f, 0.3f, -20f); // Agent 2 시작 위치
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no Target assigned!");
        }
    }

    public override void OnEpisodeBegin()
    {
        rBody.angularVelocity = Vector3.zero;
        rBody.velocity = Vector3.zero;

        transform.localPosition = lastTargetPosition;

        episodeCount++;
        SpawnObject();

        initialDistance = Vector3.Distance(transform.localPosition, Target.localPosition);
        previousProgress = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Target.localPosition);            // 3
        sensor.AddObservation(transform.localPosition);         // 3
        sensor.AddObservation((Target.localPosition - transform.localPosition).normalized); // 3
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-1.5f / MaxStep); // 시간 패널티

        if (actionBuffers.DiscreteActions[0] != 0)
            AddReward(0.002f); // 미세 보상

        MoveAgent(actionBuffers.DiscreteActions);

        float currentDistance = Vector3.Distance(transform.localPosition, Target.localPosition);
        float currentProgress = 1 - (currentDistance / initialDistance);
        float deltaProgress = currentProgress - previousProgress;

        if (deltaProgress > 0.001f)
            AddReward(deltaProgress * 0.3f);
        else if (deltaProgress < 0.0005f)
            AddReward(-0.001f);

        previousProgress = currentProgress;

        if (transform.localPosition.y < -1f)
        {
            AddReward(-1f);
            EndEpisode();
        }

        Quaternion rot = transform.rotation;
        transform.rotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        switch (act[0])
        {
            case 1: dirToGo = transform.forward; break;
            case 2: dirToGo = -transform.forward; break;
            case 3: rotateDir = transform.up; break;
            case 4: rotateDir = -transform.up; break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * agentRotationSpeed);
        rBody.AddForce(dirToGo * agentRunSpeed * 2f, ForceMode.VelocityChange);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 2;
        else if (Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 3;
        else if (Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 4;
        else discreteActionsOut[0] = 0;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == Target.gameObject)
        {
            SetReward(5f);
            EndEpisode();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        string tag = collision.gameObject.tag;


        if (tag == "Wall")
        {
            AddReward(-0.005f);
            Debug.Log("Wall collision!");
        }
        else if (tag == "Agent")
        {
            AddReward(-0.2f);
            Debug.Log("Agent Collision!");
        }
    }

    private void SpawnObject()
    {
        List<GameObject> walls = new List<GameObject>(GameObject.FindGameObjectsWithTag("Wall"));
        GameObject start = GameObject.FindGameObjectWithTag("Start");
        if (start != null) walls.Add(start);

        int maxTries = 10;
        bool validPosition = false;
        Vector3 goalPosition = Vector3.zero;

        for (int i = 0; i < maxTries && !validPosition; i++)
        {
            mapHalfSizeX = Mathf.Min(7f + episodeCount * 0.01f, MAXmapHalfSizeX); //episode에 따라 목적지 랜덤범위 증가
            SizeZ = Mathf.Min(SizeZ + (episodeCount * 0.01f), MAXmapHalfSizeZ);

            float randomX = Random.Range(-mapHalfSizeX, mapHalfSizeX);
            float randomZ = Random.Range(-MAXmapHalfSizeZ, SizeZ);
            goalPosition = new Vector3(randomX, 0.3f, randomZ);

            Bounds goalBounds = new Bounds(goalPosition, new Vector3(3f, 3f, 3f));
            validPosition = true;

            // 벽과 충돌 검사
            foreach (GameObject wall in walls)
            {
                Collider wallCol = wall.GetComponent<Collider>();
                if (wallCol != null && wallCol.bounds.Intersects(goalBounds))
                {
                    validPosition = false;
                    break;
                }
            }

            // 다른 Target과 거리 검사
            if (validPosition)
            {
                GameObject otherTarget = GameObject.FindWithTag("Target");
                if (Target.tag == "Target")
                    otherTarget = GameObject.FindWithTag("Target_2");
                else if (Target.tag == "Target_2")
                    otherTarget = GameObject.FindWithTag("Target");

                if (otherTarget != null)
                {
                    float dist = Vector3.Distance(goalPosition, otherTarget.transform.localPosition);
                    if (dist < 5f) // 최소 거리 조건
                    {
                        validPosition = false;
                    }
                }
            }
        }

        if (!validPosition)
            goalPosition = new Vector3(-5f, 0.3f, -10f); // fallback 위치

        Target.transform.localPosition = goalPosition;
        lastTargetPosition = goalPosition;
    }
}
