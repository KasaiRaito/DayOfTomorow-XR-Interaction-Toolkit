using UnityEngine;

public class Alarm : MonoBehaviour
{
    static readonly int EmissionColorId =
        Shader.PropertyToID("_EmissionColor");

    [Header("References")]
    [SerializeField]
    Transform distanceOrigin;

    [SerializeField]
    Light alarmLight;

    [SerializeField]
    Material alarmMaterial;

    [SerializeField]
    AudioSource audioSource;

    [SerializeField]
    AudioClip beepClip;

    [Header("Detection Distance")]
    [SerializeField, Min(0f)]
    float minimumDistance = 2f;

    [SerializeField, Min(0f)]
    float maximumDistance = 20f;

    [Header("Alarm Timing")]
    [SerializeField, Min(0.01f)]
    float fastestInterval = 0.2f;

    [SerializeField, Min(0.01f)]
    float slowestInterval = 1.5f;

    [SerializeField, Range(0f, 1f)]
    float beepVolume = 0.5f;

    AudioClip generatedBeepClip;
    Material sourceAlarmMaterial;
    Material runtimeAlarmMaterial;
    Color alarmEmissionColor;
    float nextPulseTime;
    bool isPulsing;
    bool alarmVisualEnabled;

    void Awake()
    {
        if (distanceOrigin == null)
        {
            Tank tank = FindAnyObjectByType<Tank>();

            if (tank != null)
            {
                distanceOrigin = tank.transform;

                if (transform.parent == null)
                    transform.SetParent(tank.transform, false);
            }
            else
            {
                distanceOrigin = transform;
            }
        }

        if (alarmLight == null)
            alarmLight = GetComponentInChildren<Light>(true);

        PrepareAlarmMaterial();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        SetLight(false);
    }

    void Update()
    {
        float targetDistance = GetNearestTargetDistance();

        if (targetDistance > maximumDistance)
        {
            StopAlarm();
            return;
        }

        if (targetDistance <= minimumDistance)
        {
            SetLight(true);
            StopSound();
            isPulsing = false;
            return;
        }

        float distanceFactor = Mathf.InverseLerp(
            minimumDistance,
            maximumDistance,
            targetDistance);
        float pulseInterval = Mathf.Lerp(
            fastestInterval,
            slowestInterval,
            distanceFactor);
        AudioClip clip = GetBeepClip();
        float beepDuration = GetPlaybackDuration(clip);
        pulseInterval = Mathf.Max(pulseInterval, beepDuration);

        if (!isPulsing)
        {
            isPulsing = true;
            nextPulseTime = Time.time;
            SetLight(false);
        }

        if (Time.time >= nextPulseTime)
        {
            Pulse(clip);
            nextPulseTime = Time.time + pulseInterval;
        }
    }

    void OnDisable()
    {
        StopAlarm();
    }

    void OnDestroy()
    {
        if (generatedBeepClip != null)
            Destroy(generatedBeepClip);

        RestoreAndDestroyAlarmMaterial();
    }

    void OnValidate()
    {
        minimumDistance = Mathf.Max(0f, minimumDistance);
        maximumDistance = Mathf.Max(minimumDistance, maximumDistance);
        fastestInterval = Mathf.Max(0.01f, fastestInterval);
        slowestInterval = Mathf.Max(fastestInterval, slowestInterval);
    }

    float GetNearestTargetDistance()
    {
        if (distanceOrigin == null)
            return float.PositiveInfinity;

        float nearestSqrDistance = float.PositiveInfinity;

        foreach (Target target in Target.ActiveTargets)
        {
            if (target == null || !target.isActiveAndEnabled)
                continue;

            float sqrDistance =
                (target.transform.position - distanceOrigin.position).sqrMagnitude;

            if (sqrDistance < nearestSqrDistance)
                nearestSqrDistance = sqrDistance;
        }

        return Mathf.Sqrt(nearestSqrDistance);
    }

    void Pulse(AudioClip clip)
    {
        ToggleLight();

        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, beepVolume);
    }

    AudioClip GetBeepClip()
    {
        return beepClip != null ? beepClip : GetGeneratedBeepClip();
    }

    float GetPlaybackDuration(AudioClip clip)
    {
        if (clip == null)
            return 0.01f;

        float pitch = audioSource != null
            ? Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch))
            : 1f;

        return clip.length / pitch;
    }

    void StopAlarm()
    {
        SetLight(false);
        StopSound();
        isPulsing = false;
    }

    void StopSound()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    void SetLight(bool enabled)
    {
        alarmVisualEnabled = enabled;

        if (alarmLight != null)
            alarmLight.enabled = enabled;

        SetMaterialEmission(enabled);
    }

    void ToggleLight()
    {
        SetLight(!alarmVisualEnabled);
    }

    void PrepareAlarmMaterial()
    {
        if (alarmMaterial == null ||
            !alarmMaterial.HasProperty(EmissionColorId))
        {
            return;
        }

        sourceAlarmMaterial = alarmMaterial;
        alarmEmissionColor = alarmMaterial.GetColor(EmissionColorId);
        runtimeAlarmMaterial = new Material(alarmMaterial)
        {
            name = alarmMaterial.name + " (Alarm Runtime)"
        };
        alarmMaterial = runtimeAlarmMaterial;

        ReplaceMaterial(sourceAlarmMaterial, runtimeAlarmMaterial);
    }

    void SetMaterialEmission(bool enabled)
    {
        if (runtimeAlarmMaterial == null)
            return;

        if (enabled)
        {
            runtimeAlarmMaterial.EnableKeyword("_EMISSION");
            runtimeAlarmMaterial.SetColor(
                EmissionColorId,
                alarmEmissionColor);
        }
        else
        {
            runtimeAlarmMaterial.SetColor(EmissionColorId, Color.black);
            runtimeAlarmMaterial.DisableKeyword("_EMISSION");
        }
    }

    void ReplaceMaterial(Material oldMaterial, Material newMaterial)
    {
        foreach (Renderer childRenderer in
                 GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = childRenderer.sharedMaterials;
            bool materialReplaced = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != oldMaterial)
                    continue;

                materials[i] = newMaterial;
                materialReplaced = true;
            }

            if (materialReplaced)
                childRenderer.sharedMaterials = materials;
        }
    }

    void RestoreAndDestroyAlarmMaterial()
    {
        if (runtimeAlarmMaterial == null)
            return;

        ReplaceMaterial(runtimeAlarmMaterial, sourceAlarmMaterial);
        Destroy(runtimeAlarmMaterial);
        runtimeAlarmMaterial = null;
    }

    AudioClip GetGeneratedBeepClip()
    {
        if (generatedBeepClip != null)
            return generatedBeepClip;

        const int sampleRate = 44100;
        const float duration = 0.1f;
        const float frequency = 880f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = 1f - i / (float)sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope;
        }

        generatedBeepClip = AudioClip.Create(
            "Generated Alarm Beep",
            sampleCount,
            1,
            sampleRate,
            false);
        generatedBeepClip.SetData(samples, 0);
        return generatedBeepClip;
    }
}
