using System;
using TMPro;
using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    public event Action SessionStarted;
    public event Action SessionEnded;

    [Header("Session")]
    [SerializeField, Min(1f)]
    float sessionDuration = 60f;

    [SerializeField]
    bool playOnStart = true;

    [Header("References")]
    [SerializeField]
    TargetSpawner targetSpawner;

    [SerializeField]
    ScoreManager scoreManager;

    [Header("Displays")]
    [SerializeField]
    TMP_Text scoreDisplay;

    [SerializeField]
    TMP_Text timerDisplay;

    public int CurrentScore { get; private set; }
    public float RemainingTime { get; private set; }
    public bool IsSessionActive { get; private set; }
    public int LastScorePosition { get; private set; } = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (targetSpawner == null)
            targetSpawner = GetComponent<TargetSpawner>();

        if (targetSpawner == null)
            targetSpawner = FindAnyObjectByType<TargetSpawner>();

        if (scoreManager == null)
            scoreManager = GetComponent<ScoreManager>();

        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        RemainingTime = sessionDuration;
        UpdateDisplays();
    }

    void Start()
    {
        if (playOnStart)
            StartSession();
    }

    void Update()
    {
        if (!IsSessionActive)
            return;

        RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
        UpdateDisplays();

        if (RemainingTime <= 0f)
            EndSession();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartSession()
    {
        CurrentScore = 0;
        RemainingTime = sessionDuration;
        LastScorePosition = -1;
        IsSessionActive = true;

        if (targetSpawner != null)
            targetSpawner.BeginSession();

        UpdateDisplays();
        SessionStarted?.Invoke();
    }

    public void EndSession()
    {
        if (!IsSessionActive)
            return;

        IsSessionActive = false;
        RemainingTime = 0f;

        if (targetSpawner != null)
            targetSpawner.EndSession();

        if (scoreManager != null)
            LastScorePosition = scoreManager.SubmitScore(CurrentScore);

        UpdateDisplays();
        SessionEnded?.Invoke();
    }

    public void AddScore(int points)
    {
        if (!IsSessionActive)
            return;

        CurrentScore += Mathf.Max(0, points);
        UpdateDisplays();
    }

    void UpdateDisplays()
    {
        if (scoreDisplay != null)
            scoreDisplay.text = $"Score: {CurrentScore}";

        if (timerDisplay != null)
        {
            int totalSeconds = Mathf.CeilToInt(RemainingTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerDisplay.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
