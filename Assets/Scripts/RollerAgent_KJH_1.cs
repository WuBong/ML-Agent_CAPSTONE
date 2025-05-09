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

    private float previousDistanceToTarget = 0f;

    public Transform Target;
    public Transform StartPoint;
    float episodeCount = 0;
    float initialDistance;
    float previousProgress;

    float MAXmapHalfSizeX = 25f;
    float MAXmapHalfSizeZ = 25f;

    float mapHalfSizeX = 0f;
    float mapHalfSizeZ = 0f;
    float SizeZ = -5f;

    public override void Initialize()
    {
        rBody = GetComponent<Rigidbody>();
        m_RollerSetting = FindObjectOfType<RollerSetting>();
    }

    public override void OnEpisodeBegin()
    {
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;
        this.transform.localPosition = new Vector3(0, 0.3f, -20);

        initialDistance = Vector3.Distance(StartPoint.localPosition, Target.localPosition);
        previousProgress = 0f;

        episodeCount++;
        SpawnObject();

        /*Target.localPosition = new Vector3(Random.value * 15 - 7,
                                           0.3f,
                                           Random.value * 15 - 7);*/
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);
        Vector3 toTarget = (Target.localPosition - this.transform.localPosition).normalized;
        sensor.AddObservation(toTarget);

    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-1.5f / MaxStep);
        MoveAgent(actionBuffers.DiscreteActions);

        float currentDistance = Vector3.Distance(this.transform.localPosition, Target.localPosition);
        float currentProgress = 1 - (currentDistance / initialDistance);
        float deltaProgress = currentProgress - previousProgress;
        AddReward(deltaProgress * 0.3f);
        previousProgress = currentProgress;
    }


    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];
        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
        }
        transform.Rotate(rotateDir, Time.deltaTime * m_RollerSetting.agentRotationSpeed);
        rBody.AddForce(dirToGo * m_RollerSetting.agentRunSpeed, ForceMode.VelocityChange);
    }



    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }

    void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Target"))
        {
            SetReward(2f);
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            SetReward(-0.05f);
        }
    }

    private void SpawnObject()
    {
        List<GameObject> walls = new List<GameObject>(GameObject.FindGameObjectsWithTag("Wall"));
        GameObject start = GameObject.FindGameObjectWithTag("Start");
        if (start != null)
        {
            walls.Add(start);
        }

        //벽과 겹치지 않게 goal 위치 설정
        int maxTries = 10;
        bool validPosition = false;
        Vector3 goalPosition = Vector3.zero;

        for (int i = 0; i < maxTries && !validPosition; i++)
        {
            mapHalfSizeX = 7f + (episodeCount * 0.001f);
            mapHalfSizeZ = MAXmapHalfSizeX;
            SizeZ += (episodeCount * 0.001f);

            //x 조절
            if (mapHalfSizeX > MAXmapHalfSizeX)
                mapHalfSizeX = MAXmapHalfSizeX;

            //z 조절
            if (SizeZ > MAXmapHalfSizeZ)
                SizeZ = MAXmapHalfSizeZ;

            float randomX = Random.Range(-mapHalfSizeX, mapHalfSizeX);
            float randomZ = Random.Range(-mapHalfSizeZ, SizeZ);

            goalPosition = new Vector3(randomX, 1.8f, randomZ);

            Vector3 testPos = new Vector3(goalPosition.x, 1.8f, goalPosition.z);

            Bounds goalBounds = new Bounds(testPos, new Vector3(3f, 7f, 3f)); // Goal의 바운딩 박스

            validPosition = true; // 먼저 true로 설정 후 검사
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
            goalPosition = new Vector3(-5f, 1.8f, -10f);
        }

        Target.transform.localPosition = goalPosition;

    }

}