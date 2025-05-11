using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.AI;

public class EnvironmentController : MonoBehaviour
{
    public Transform EnvironmentCenter;
    public List<Transform> agentTransforms;
    public List<Transform> targetTransforms;
    private PathfinderAgent[] agents;
    private HashSet<GameObject> visitedTargets = new HashSet<GameObject>();
    private SimpleMultiAgentGroup m_AgentGroup;

    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 5000;
    private int m_ResetTimer;

    private void Start()
    {
        agents = new PathfinderAgent[agentTransforms.Count];
        for (int i = 0; i < agentTransforms.Count; i++)
        {
            agents[i] = agentTransforms[i].GetComponent<PathfinderAgent>();
        }

        m_AgentGroup = new SimpleMultiAgentGroup();
        foreach (var agent in agents)
        {
            m_AgentGroup.RegisterAgent(agent);
        }

        foreach (var target in targetTransforms)
        {
            target.gameObject.SetActive(true);
        }

        ResetScene();
    }

    private void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }


    public Vector3 GetRandomPositionOnNavi()
    {
        float rangeX = 15f;
        float rangeZ = 25f;

        while (true)
        {
            Vector3 randomDir = EnvironmentCenter.position + new Vector3(Random.Range(-rangeX, rangeX), 0.5f, Random.Range(-rangeZ, rangeZ));

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                return randomDir;
            }
        }
    }

    void ResetScene()
    {
        m_ResetTimer = 0;
        visitedTargets.Clear();

        foreach (var transform in agentTransforms)
        {
            transform.position = GetRandomPositionOnNavi();
            m_AgentGroup.RegisterAgent(transform.GetComponent<PathfinderAgent>());
        }

        foreach (var target in targetTransforms)
        {
            target.position = GetRandomPositionOnNavi();
            target.gameObject.SetActive(true);
        }
    }

    public void TouchWall()
    {
        m_AgentGroup.AddGroupReward(-0.04f);
    }

    public void TouchTarget(Collider other)
    {
        if (visitedTargets.Contains(other.gameObject))
            return;

        m_AgentGroup.AddGroupReward(70f);
        visitedTargets.Add(other.gameObject);
        other.gameObject.SetActive(false);

        if(visitedTargets.Count == targetTransforms.Count)
        {
            m_AgentGroup.AddGroupReward(700f);
            m_AgentGroup.EndGroupEpisode();
            ResetScene();
        }
    }

    public void TouchAgent()
    {
        m_AgentGroup.AddGroupReward(-10f);
    }
}