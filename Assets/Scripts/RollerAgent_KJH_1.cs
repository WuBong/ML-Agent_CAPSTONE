using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RollerAgent_KJH_1 : Agent
{
    Rigidbody rBody;
    RollerSetting m_RollerSetting;

    public Transform Target;
    public Transform StartPoint;

    float episodeCount = 0;
    float initialDistance;
    float previousProgress;

    float MAXmapHalfSizeX = 25f;
    float MAXmapHalfSizeZ = 25f;

    float mapHalfSizeX = 0f;
    float SizeZ = -5f;

    public override void Initialize()
    {
        rBody = GetComponent<Rigidbody>();
        m_RollerSetting = FindObjectOfType<RollerSetting>();
    }

    public override void OnEpisodeBegin()
    {
        rBody.angularVelocity = Vector3.zero;
        rBody.velocity = Vector3.zero;
        transform.localPosition = new Vector3(0, 0.3f, -20);

        episodeCount++;
        SpawnObject();

        initialDistance = Vector3.Distance(transform.localPosition, Target.localPosition);
        previousProgress = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Target.localPosition);             // 3
        sensor.AddObservation(transform.localPosition);          // 3
        Vector3 toTarget = (Target.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(toTarget);                         // 3
        // 총 9개
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // 시간당 패널티
        AddReward(-1.5f / MaxStep);

        // 움직이면 미세 보상
        if (actionBuffers.DiscreteActions[0] != 0)
            AddReward(0.002f);

        MoveAgent(actionBuffers.DiscreteActions);

        // 거리 기반 shaping reward
        float currentDistance = Vector3.Distance(transform.localPosition, Target.localPosition);
        float currentProgress = 1 - (currentDistance / initialDistance);
        float deltaProgress = currentProgress - previousProgress;

        if (deltaProgress > 0.001f)
        {
            AddReward(deltaProgress * 0.3f);  // 보상 크기 조정
        }
        else if (deltaProgress < 0.0005f)
        {
            AddReward(-0.001f);  // 진전 없음에 대한 미세 패널티
        }

        previousProgress = currentProgress;

        // 낙하 처리
        if (transform.localPosition.y < -1f)
        {
            AddReward(-1f);
            EndEpisode();
        }
        // ✅ x축, z축 회전값 고정
        Quaternion rot = transform.rotation;
        transform.rotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];
        switch (action)
        {
            case 1: dirToGo = transform.forward; break;
            case 2: dirToGo = -transform.forward; break;
            case 3: rotateDir = transform.up; break;
            case 4: rotateDir = -transform.up; break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * m_RollerSetting.agentRotationSpeed);
        rBody.AddForce(dirToGo * m_RollerSetting.agentRunSpeed, ForceMode.VelocityChange);
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

    void OnCollisionEnter(Collision collision)
    {
        string tag = collision.gameObject.tag;

        if (tag == "Target")
        {
            SetReward(5f);
            EndEpisode();
        }
        else if (tag == "Wall")
        {
            AddReward(-0.005f);
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
        {
            goalPosition = new Vector3(-5f, 0.3f, -10f);
        }

        Target.transform.localPosition = goalPosition;
    }
}
