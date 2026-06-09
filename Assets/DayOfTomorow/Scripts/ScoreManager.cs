using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Serializable]
    class ScoreData
    {
        public List<int> scores = new List<int>();
    }

    const string ScoresKey = "DayOfTomorow.TopScores";
    const int MaximumSavedScores = 10;

    public static ScoreManager Instance { get; private set; }

    [SerializeField]
    TMP_Text highScoresDisplay;

    readonly List<int> topScores = new List<int>();

    public IReadOnlyList<int> TopScores => topScores;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        LoadScores();
        UpdateDisplay();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int SubmitScore(int score)
    {
        topScores.Add(Mathf.Max(0, score));
        topScores.Sort((left, right) => right.CompareTo(left));

        if (topScores.Count > MaximumSavedScores)
        {
            topScores.RemoveRange(
                MaximumSavedScores,
                topScores.Count - MaximumSavedScores);
        }

        SaveScores();
        UpdateDisplay();

        int position = topScores.IndexOf(score);
        return position >= 0 ? position + 1 : -1;
    }

    public void ClearScores()
    {
        topScores.Clear();
        PlayerPrefs.DeleteKey(ScoresKey);
        PlayerPrefs.Save();
        UpdateDisplay();
    }

    void LoadScores()
    {
        topScores.Clear();

        string json = PlayerPrefs.GetString(ScoresKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return;

        ScoreData data = JsonUtility.FromJson<ScoreData>(json);
        if (data?.scores == null)
            return;

        topScores.AddRange(data.scores);
        topScores.Sort((left, right) => right.CompareTo(left));

        if (topScores.Count > MaximumSavedScores)
        {
            topScores.RemoveRange(
                MaximumSavedScores,
                topScores.Count - MaximumSavedScores);
        }
    }

    void SaveScores()
    {
        ScoreData data = new ScoreData
        {
            scores = new List<int>(topScores)
        };

        PlayerPrefs.SetString(ScoresKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    void UpdateDisplay()
    {
        if (highScoresDisplay == null)
            return;

        StringBuilder text = new StringBuilder("TOP SCORES");

        for (int i = 0; i < topScores.Count; i++)
        {
            text.AppendLine();
            text.Append(i + 1);
            text.Append(". ");
            text.Append(topScores[i]);
        }

        highScoresDisplay.text = text.ToString();
    }
}
