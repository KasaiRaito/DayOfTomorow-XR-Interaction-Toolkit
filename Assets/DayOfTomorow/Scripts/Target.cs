using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    static readonly HashSet<Target> activeTargets = new HashSet<Target>();

    [SerializeField, Min(0)]
    int scoreValue = 10;

    [SerializeField]
    Renderer[] targetRenderers;

    public static IReadOnlyCollection<Target> ActiveTargets => activeTargets;
    public int ScoreValue => scoreValue;

    TargetSpawner spawner;
    bool hasBeenHit;

    void Awake()
    {
        CacheRenderers();
    }

    void OnEnable()
    {
        hasBeenHit = false;
        activeTargets.Add(this);
    }

    void OnDisable()
    {
        activeTargets.Remove(this);
    }

    public void Initialize(TargetSpawner owner, int value, Color color)
    {
        spawner = owner;
        scoreValue = Mathf.Max(0, value);
        ApplyColor(color);
    }

    public void Hit()
    {
        if (hasBeenHit)
            return;

        hasBeenHit = true;

        GameSessionManager sessionManager = GameSessionManager.Instance;
        if (sessionManager != null)
            sessionManager.AddScore(scoreValue);

        TargetSpawner owner = spawner;
        gameObject.SetActive(false);

        if (owner != null)
        {
            owner.HandleTargetDestroyed(this);
            Destroy(gameObject);
        }
    }

    void ApplyColor(Color color)
    {
        CacheRenderers();

        if (targetRenderers == null)
            return;

        MaterialPropertyBlock properties = new MaterialPropertyBlock();

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            targetRenderer.SetPropertyBlock(properties);
        }
    }

    void CacheRenderers()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);
    }
}
