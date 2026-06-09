using System;
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [Serializable]
    public class TargetOption
    {
        public Target targetTemplate;

        [Min(0)]
        public int scoreValue = 10;

        public Color color = Color.red;
    }

    public static TargetSpawner Instance { get; private set; }

    [Header("Targets")]
    [SerializeField]
    List<TargetOption> targetOptions = new List<TargetOption>();

    [SerializeField, Min(1)]
    int activeTargetCount = 3;

    [Header("Spawn Area")]
    [SerializeField]
    Transform spawnCenter;

    [SerializeField]
    Vector3 spawnAreaCenter = new Vector3(-15f, 1f, 0f);

    [SerializeField]
    Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);

    [SerializeField, Min(0f)]
    float minimumTargetSpacing = 2f;

    [SerializeField, Min(1)]
    int spawnPositionAttempts = 20;

    readonly HashSet<Target> spawnedTargets = new HashSet<Target>();

    bool sessionActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DisableSceneTemplates();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnValidate()
    {
        activeTargetCount = Mathf.Max(1, activeTargetCount);
        spawnAreaSize = new Vector3(
            Mathf.Abs(spawnAreaSize.x),
            Mathf.Abs(spawnAreaSize.y),
            Mathf.Abs(spawnAreaSize.z));
        minimumTargetSpacing = Mathf.Max(0f, minimumTargetSpacing);
        spawnPositionAttempts = Mathf.Max(1, spawnPositionAttempts);
    }

    public void BeginSession()
    {
        sessionActive = true;
        ClearSpawnedTargets();
        FillTargetCount();
    }

    public void EndSession()
    {
        sessionActive = false;
        ClearSpawnedTargets();
    }

    public void HandleTargetDestroyed(Target target)
    {
        spawnedTargets.Remove(target);

        if (sessionActive)
            SpawnRandomTarget();
    }

    public Target SpawnRandomTarget()
    {
        TargetOption option = GetRandomValidOption();
        if (option == null)
            return null;

        Vector3 spawnPosition = GetRandomSpawnPosition();
        Target target = Instantiate(
            option.targetTemplate,
            spawnPosition,
            Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));

        target.Initialize(this, option.scoreValue, option.color);
        target.gameObject.SetActive(true);
        spawnedTargets.Add(target);
        return target;
    }

    void FillTargetCount()
    {
        RemoveMissingTargets();

        while (spawnedTargets.Count < activeTargetCount)
        {
            if (SpawnRandomTarget() == null)
                break;
        }
    }

    TargetOption GetRandomValidOption()
    {
        List<TargetOption> validOptions = new List<TargetOption>();

        foreach (TargetOption option in targetOptions)
        {
            if (option != null && option.targetTemplate != null)
                validOptions.Add(option);
        }

        if (validOptions.Count == 0)
        {
            Debug.LogWarning("TargetSpawner has no target options configured.", this);
            return null;
        }

        return validOptions[UnityEngine.Random.Range(0, validOptions.Count)];
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = spawnCenter != null
            ? spawnCenter.position
            : spawnAreaCenter;
        Vector3 candidate = center;
        float minimumSpacingSqr = minimumTargetSpacing * minimumTargetSpacing;

        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
                UnityEngine.Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f),
                UnityEngine.Random.Range(-spawnAreaSize.z * 0.5f, spawnAreaSize.z * 0.5f));

            candidate = center + offset;

            bool isFarEnough = true;
            foreach (Target spawnedTarget in spawnedTargets)
            {
                if (spawnedTarget != null &&
                    (spawnedTarget.transform.position - candidate).sqrMagnitude <
                    minimumSpacingSqr)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough)
                return candidate;
        }

        return candidate;
    }

    void DisableSceneTemplates()
    {
        foreach (TargetOption option in targetOptions)
        {
            if (option == null || option.targetTemplate == null)
                continue;

            GameObject templateObject = option.targetTemplate.gameObject;
            if (templateObject.scene.IsValid())
                templateObject.SetActive(false);
        }
    }

    void ClearSpawnedTargets()
    {
        foreach (Target target in spawnedTargets)
        {
            if (target != null)
            {
                target.gameObject.SetActive(false);
                Destroy(target.gameObject);
            }
        }

        spawnedTargets.Clear();
    }

    void RemoveMissingTargets()
    {
        spawnedTargets.RemoveWhere(target => target == null);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = spawnCenter != null
            ? spawnCenter.position
            : spawnAreaCenter;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, spawnAreaSize);
    }
}
