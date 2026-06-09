using UnityEngine;

public class Alarm : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    Transform distanceOrigin;

    [SerializeField]
    Light alarmLight;

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
    float nextPulseTime;
    float lightOffTime;
    bool isPulsing;

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
        }

        if (Time.time >= nextPulseTime)
        {
            Pulse(clip, beepDuration);
            nextPulseTime = Time.time + pulseInterval;
        }

        if (alarmLight != null &&
            alarmLight.enabled &&
            Time.time >= lightOffTime)
        {
            alarmLight.enabled = false;
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

    void Pulse(AudioClip clip, float beepDuration)
    {
        SetLight(true);
        lightOffTime = Time.time + beepDuration;

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
        if (alarmLight != null)
            alarmLight.enabled = enabled;
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
