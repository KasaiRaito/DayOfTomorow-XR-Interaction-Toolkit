using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class VictoryUI : MonoBehaviour
{
    const string UILayerName = "UI";

    [Header("References")]
    [SerializeField]
    GameSessionManager sessionManager;

    [SerializeField]
    ScoreManager scoreManager;

    [SerializeField]
    Camera uiCamera;

    [SerializeField]
    Canvas hudCanvas;

    [Header("Scores")]
    [SerializeField, Range(1, 10)]
    int displayedHighScores = 10;

    [Header("Display")]
    [SerializeField, Min(0.1f)]
    float planeDistance = 1f;

    [Header("Main Menu")]
    [SerializeField]
    string mainMenuSceneName;

    GameObject canvasObject;
    TMP_Text scoreText;
    TMP_Text bestScoresText;
    Button retryButton;

    void Awake()
    {
        if (sessionManager == null)
            sessionManager = GetComponent<GameSessionManager>();

        if (sessionManager == null)
            sessionManager = GameSessionManager.Instance;

        if (scoreManager == null)
            scoreManager = GetComponent<ScoreManager>();

        if (scoreManager == null)
            scoreManager = ScoreManager.Instance;

        if (uiCamera == null)
            uiCamera = Camera.main;

        if (uiCamera == null)
            uiCamera = FindAnyObjectByType<Camera>();

        if (hudCanvas == null)
        {
            GameObject hudObject = GameObject.Find("HUD-Canvas");

            if (hudObject != null)
                hudCanvas = hudObject.GetComponent<Canvas>();
        }

        ConfigureHudCanvas();
        BuildUI();
        Hide();
    }

    void OnEnable()
    {
        if (sessionManager == null)
            return;

        sessionManager.SessionStarted += Hide;
        sessionManager.SessionEnded += Show;
    }

    void OnDisable()
    {
        if (sessionManager == null)
            return;

        sessionManager.SessionStarted -= Hide;
        sessionManager.SessionEnded -= Show;
    }

    void OnValidate()
    {
        displayedHighScores = Mathf.Clamp(displayedHighScores, 1, 10);
        planeDistance = Mathf.Max(0.1f, planeDistance);
    }

    void BuildUI()
    {
        if (uiCamera == null)
        {
            Debug.LogWarning(
                "VictoryUI could not find a camera for its world-space canvas.",
                this);
            return;
        }

        canvasObject = new GameObject(
            "Victory UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(TrackedDeviceGraphicRaycaster));
        int uiLayer = LayerMask.NameToLayer(UILayerName);

        if (uiLayer < 0)
        {
            Debug.LogError(
                $"VictoryUI requires the '{UILayerName}' layer.",
                this);
            return;
        }

        canvasObject.layer = uiLayer;
        EnableDistanceUIInteraction(uiLayer);

        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = Mathf.Max(
            planeDistance,
            uiCamera.nearClipPlane + 0.01f);
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        Image background = CreateImage(
            "Background",
            canvasRect,
            Vector2.zero,
            new Vector2(700f, 560f),
            new Color(0.015f, 0.035f, 0.055f, 0.97f));
        background.raycastTarget = true;

        Outline outline = background.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.75f, 0.85f, 1f);
        outline.effectDistance = new Vector2(5f, -5f);

        CreateText(
            "Title",
            canvasRect,
            "VICTORY",
            new Vector2(0f, 235f),
            new Vector2(640f, 60f),
            46f,
            new Color(0.2f, 0.95f, 1f, 1f),
            TextAlignmentOptions.Center,
            FontStyles.Bold);

        scoreText = CreateText(
            "Score",
            canvasRect,
            "SCORE: 0",
            new Vector2(0f, 170f),
            new Vector2(640f, 50f),
            34f,
            Color.white,
            TextAlignmentOptions.Center,
            FontStyles.Bold);

        CreateText(
            "Best Scores Header",
            canvasRect,
            "BEST SCORES",
            new Vector2(0f, 112f),
            new Vector2(640f, 42f),
            27f,
            new Color(1f, 0.78f, 0.2f, 1f),
            TextAlignmentOptions.Center,
            FontStyles.Bold);

        bestScoresText = CreateText(
            "Best Scores",
            canvasRect,
            string.Empty,
            new Vector2(0f, -20f),
            new Vector2(580f, 210f),
            22f,
            Color.white,
            TextAlignmentOptions.Top,
            FontStyles.Normal);

        retryButton = CreateButton(
            "Retry Button",
            canvasRect,
            "RETRY",
            new Vector2(-140f, -235f),
            Retry);

        CreateButton(
            "Main Menu Button",
            canvasRect,
            "MAIN MENU",
            new Vector2(140f, -235f),
            OpenMainMenu);
    }

    void ConfigureHudCanvas()
    {
        if (hudCanvas == null || uiCamera == null)
            return;

        int uiLayer = LayerMask.NameToLayer(UILayerName);

        if (uiLayer < 0)
            return;

        SetLayerRecursively(hudCanvas.gameObject, uiLayer);

        hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        hudCanvas.worldCamera = uiCamera;
        hudCanvas.planeDistance = Mathf.Max(
            planeDistance,
            uiCamera.nearClipPlane + 0.01f);
        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder = 90;

        CanvasScaler scaler = hudCanvas.GetComponent<CanvasScaler>();

        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;

        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    void EnableDistanceUIInteraction(int uiLayer)
    {
        NearFarInteractor[] nearFarInteractors =
            FindObjectsByType<NearFarInteractor>(
                FindObjectsInactive.Include);

        foreach (NearFarInteractor interactor in nearFarInteractors)
            interactor.enableUIInteraction = true;

        int uiLayerMask = 1 << uiLayer;
        XRRayInteractor[] rayInteractors =
            FindObjectsByType<XRRayInteractor>(
                FindObjectsInactive.Include);

        foreach (XRRayInteractor interactor in rayInteractors)
        {
            interactor.enableUIInteraction = true;
            interactor.raycastMask |= uiLayerMask;
        }
    }

    Image CreateImage(
        string objectName,
        RectTransform parent,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.layer = canvasObject.layer;

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    TMP_Text CreateText(
        string objectName,
        RectTransform parent,
        string content,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = canvasObject.layer;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = fontStyle;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    Button CreateButton(
        string objectName,
        RectTransform parent,
        string label,
        Vector2 position,
        UnityAction callback)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.layer = canvasObject.layer;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(240f, 70f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.3f, 0.38f, 1f);
        colors.highlightedColor = new Color(0.12f, 0.55f, 0.65f, 1f);
        colors.pressedColor = new Color(0.05f, 0.2f, 0.26f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        button.colors = colors;
        button.onClick.AddListener(callback);

        CreateText(
            "Label",
            rect,
            label,
            Vector2.zero,
            rect.sizeDelta,
            26f,
            Color.white,
            TextAlignmentOptions.Center,
            FontStyles.Bold);

        return button;
    }

    void Show()
    {
        if (canvasObject == null || sessionManager == null)
            return;

        scoreText.text = $"SCORE: {sessionManager.CurrentScore}";
        bestScoresText.text = BuildBestScoresText();
        canvasObject.SetActive(true);

        if (EventSystem.current != null && retryButton != null)
            retryButton.Select();
    }

    void Hide()
    {
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    string BuildBestScoresText()
    {
        if (scoreManager == null || scoreManager.TopScores.Count == 0)
            return "NO SCORES YET";

        StringBuilder text = new StringBuilder();
        int scoreCount = Mathf.Min(
            displayedHighScores,
            scoreManager.TopScores.Count);

        for (int i = 0; i < scoreCount; i++)
        {
            if (i > 0)
                text.AppendLine();

            text.Append(i + 1);
            text.Append(".  ");
            text.Append(scoreManager.TopScores[i]);
        }

        return text.ToString();
    }

    void Retry()
    {
        if (sessionManager != null)
            sessionManager.StartSession();
    }

    void OpenMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning(
                "VictoryUI Main Menu scene is not configured yet.",
                this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogWarning(
                $"Main Menu scene '{mainMenuSceneName}' is not in Build Settings.",
                this);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
