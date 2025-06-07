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
    float prevDist = 0f;

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

        episodeCount++;
        SpawnObject();
    }

    //관측후 python으로 전송, data 정규화하여 전송 -> 안정성 향상
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 dir = Target.localPosition - transform.localPosition;

        // 1) 타깃 방향 (X,Z만) → 2
        Vector2 flatDir = new Vector2(dir.x, dir.z).normalized;
        sensor.AddObservation(flatDir);

        // 2) 타깃까지 거리 (정규화) → 1
        sensor.AddObservation(dir.magnitude / 50f); // 맵 지름 50 기준

        // 3) 에이전트 속도 (정규화) → 3
        sensor.AddObservation(rBody.velocity / m_RollerSetting.agentRunSpeed);

    }


    //python으로 부터 action을 받아와 동작을 수행.
    public override void OnActionReceived(ActionBuffers actions)
    {

        // 1) 시간 패널티
        AddReward(-0.002f);

        // 2) 거리 기반 shaped reward
        float curDist = Vector3.Distance(transform.localPosition, Target.localPosition);
        float weightDist = 0.5f;
        AddReward((prevDist - curDist) * weightDist);

        // 3) 속도 방향 보상
        Vector3 dir = (Target.localPosition - transform.localPosition).normalized;
        float forwardSpeed = Vector3.Dot(rBody.velocity, dir);
        float weightVel = 0.05f;
        AddReward(Mathf.Clamp(forwardSpeed / m_RollerSetting.agentRunSpeed, -1f, 1f) * weightVel);

        prevDist = curDist;

        MoveAgent(actions.DiscreteActions);
    }


    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];
        switch (action)
        {
            case 0:
                dirToGo = transform.forward * 1f;
                break;
            case 1:
                dirToGo = transform.forward * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
            case 3:
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
            Debug.Log("Goal Hit");
            SetReward(2f);
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log("Wall Hit");
            SetReward(-0.1f);
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

            goalPosition = new Vector3(randomX, 1.5f, randomZ);

            Vector3 testPos = new Vector3(goalPosition.x, 1.5f, goalPosition.z);

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
            goalPosition = new Vector3(-5f, 1.5f, -10f);
        }

        Target.transform.localPosition = goalPosition;

    }

}