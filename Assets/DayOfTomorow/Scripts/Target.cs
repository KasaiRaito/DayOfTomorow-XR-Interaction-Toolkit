using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    static readonly HashSet<Target> activeTargets = new HashSet<Target>();

    public static IReadOnlyCollection<Target> ActiveTargets => activeTargets;

    void OnEnable()
    {
        activeTargets.Add(this);
    }

    void OnDisable()
    {
        activeTargets.Remove(this);
    }

    public void Hit()
    {
        gameObject.SetActive(false);
    }
}
