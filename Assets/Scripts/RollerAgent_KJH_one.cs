using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RollerAgent_KJH_one : Agent
{
    Rigidbody rBody;

    public Transform Target;
    public string targetTag = "Target"; // Agent 별로 Target 또는 Target_2로 설정

    public float agentRunSpeed = 1.5f;
    public float agentRotationSpeed = 200f;

    float episodeCount = 0;
    float initialDistance;
    float previousProgress;

    float MAXmapHalfSizeX = 25f;
    float MAXmapHalfSizeZ = 25f;

    float mapHalfSizeX = 0f;
    float SizeZ = -5f;

    Vector3 lastTargetPosition;

    public override void Initialize()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        rBody.angularVelocity = Vector3.zero;
        rBody.velocity = Vector3.zero;

        // 에이전트 별 초기 위치 설정
        lastTargetPosition = GetInitialPositionByTag();
        transform.localPosition = lastTargetPosition;

        episodeCount++;
        SpawnObject();

        initialDistance = Vector3.Distance(transform.localPosition, Target.localPosition);
        previousProgress = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Target.localPosition);                          // 3
        sensor.AddObservation(transform.localPosition);                       // 3
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

        // 회전 고정
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
        if (other.CompareTag(targetTag))
        {
            SetReward(5f);
            EndEpisode();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            SetReward(5f);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.005f);
            Debug.Log("Wall collision!");
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
            mapHalfSizeX = Mathf.Min(7f + episodeCount * 0.001f, MAXmapHalfSizeX);
            SizeZ = Mathf.Min(SizeZ + (episodeCount * 0.0005f), MAXmapHalfSizeZ);

            float randomX = Random.Range(-mapHalfSizeX, mapHalfSizeX);
            float randomZ = Random.Range(-MAXmapHalfSizeZ, SizeZ);
            goalPosition = new Vector3(randomX, 0.3f, randomZ);

            Bounds goalBounds = new Bounds(goalPosition, new Vector3(3f, 3f, 3f));
            validPosition = true;

            foreach (GameObject wall in walls)
            {
                Collider wallCol = wall.GetComponent<Collider>();
                if (wallCol != null && wallCol.bounds.Intersects(goalBounds))
                {
                    validPosition = false;
                    break;
                }
            }
        }

        if (!validPosition)
            goalPosition = new Vector3(-5f, 0.3f, -10f);

        Target.transform.localPosition = goalPosition;
        lastTargetPosition = goalPosition;
    }

    private Vector3 GetInitialPositionByTag()
    {
        if (targetTag == "Target")
            return new Vector3(-15f, 0.3f, -20f); // Agent_1 초기 위치
        else if (targetTag == "Target_2")
            return new Vector3(15f, 0.3f, -20f);  // Agent_2 초기 위치
        else
            return new Vector3(0f, 0.3f, -20f);   // 기본값
    }
}
