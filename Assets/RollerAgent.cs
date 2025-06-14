using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RollerAgent : Agent
{
    [Header("Movement Settings")]
    [SerializeField] private float agentRunSpeed = 10f;
    [SerializeField] private float agentRotationSpeed = 180f;

    private Rigidbody rBody;

    [Header("Targets")]
    [Tooltip("씬에 배치한 모든 타겟(Transforms)을 드래그하여 넣으세요")]
    [SerializeField] private Transform[] targets;

    private Transform currentTarget; // 현재 타겟
    private Renderer agentRenderer; // 현재 타겟 색상으로 바꾸는 용도
    private int lastTargetIndex = -1; //바로 전 타겟 인덱스

    private int previousAction = 0;

    private const string LAYER_TARGET = "Target";
    private const string LAYER_NonTarget = "NonTarget";

    void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        if (rBody == null)
            Debug.LogError("Rigidbody가 없습니다.");

        // 에이전트 자신의 Renderer 가져오기
        agentRenderer = GetComponent<Renderer>();
        if (agentRenderer == null)
            Debug.LogError("RollerAgent에 Renderer 컴포넌트가 없습니다.");
    }

    public override void OnEpisodeBegin()
    {
        // 위치 리셋(떨어질 때만 리셋하거나 항상 리셋하고 싶으면 if 제거)
        if (transform.localPosition.y < 0)
        {
            rBody.angularVelocity = Vector3.zero;
            rBody.velocity = Vector3.zero;
            transform.localPosition = new Vector3(0, 0.2f, -13f);
        }

        // 1) 새 인덱스 뽑기
        int newIndex = Random.Range(0, targets.Length);
        if (newIndex == lastTargetIndex)
            newIndex = (newIndex + 1) % targets.Length;
        lastTargetIndex = newIndex;

        // 2) currentTarget 갱신
        currentTarget = targets[newIndex];

        // 3) 레이어 설정: currentTarget만 Target, 나머진 Default
        for (int i = 0; i < targets.Length; i++)
        {
            string layerName = (i == newIndex) ? LAYER_TARGET : LAYER_NonTarget;
            targets[i].gameObject.layer = LayerMask.NameToLayer(layerName);
        }

        // 4) (선택사항) 로그 및 머티리얼 동기화
        Debug.Log($"[EpisodeBegin] currentTarget: '{currentTarget.name}'");
        var rend = currentTarget.GetComponent<Renderer>();
        if (rend != null && agentRenderer != null)
            agentRenderer.material = rend.material;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and agent positions
        sensor.AddObservation(currentTarget.localPosition);
        sensor.AddObservation(transform.localPosition);


        // (B) 목표 방향 벡터 (normalized)
        Vector3 dirToTarget = (currentTarget.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(dirToTarget);

        // (C) 이전 행동 (정수 스칼라)
        sensor.AddObservation(previousAction);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        AddReward(-1f / MaxStep);

        previousAction = actionBuffers.DiscreteActions[0];

        MoveAgent(actionBuffers.DiscreteActions);

        float distanceToTarget = Vector3.Distance(transform.localPosition, currentTarget.localPosition);

        if (distanceToTarget < 2f)
        {
            Debug.Log($"Reached target '{currentTarget.name}'. Distance: {distanceToTarget}");
            SetReward(1.0f);
            EndEpisode();
        }
        else if (transform.localPosition.y < 0)
        {
            EndEpisode();
        }
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        if (rBody == null) return;

        Vector3 dirToGo = Vector3.zero;
        Vector3 rotateDir = Vector3.zero;
        int action = act[0];

        switch (action)
        {
            case 1:
                dirToGo = transform.forward;
                break;
            case 2:
                dirToGo = -transform.forward;
                break;
            case 3:
                rotateDir = transform.up;
                break;
            case 4:
                rotateDir = -transform.up;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * agentRotationSpeed);
        // 기존 rBody.AddForce(dirToGo * agentRunSpeed, ForceMode.VelocityChange);
        rBody.velocity = dirToGo * agentRunSpeed; // 관성 없음
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 2;
        else if (Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 3;
        else if (Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 4;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            Debug.Log($"벽에 부딪힘! 페널티: -0.005");
            SetReward(-0.01f);
        }
    }
}

