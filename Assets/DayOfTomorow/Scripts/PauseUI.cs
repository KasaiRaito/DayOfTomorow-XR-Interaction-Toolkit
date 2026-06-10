using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class PauseUI : MonoBehaviour
{
    const string UILayerName = "UI";
    const string DefaultMainMenuSceneName = "MainMenu";

    [Header("References")]
    [SerializeField]
    GameSessionManager sessionManager;

    [SerializeField]
    Camera uiCamera;

    [Header("Display")]
    [SerializeField, Min(0.1f)]
    float planeDistance = 2f;

    [SerializeField, Range(0.5f, 1f)]
    float viewportScale = 0.8f;

    [Header("Main Menu")]
    [SerializeField]
    string mainMenuSceneName = DefaultMainMenuSceneName;

    InputAction pauseAction;
    GameObject canvasObject;
    Button continueButton;
    bool isPaused;

    void Awake()
    {
        if (sessionManager == null)
            sessionManager = GetComponent<GameSessionManager>();

        if (sessionManager == null)
            sessionManager = GameSessionManager.Instance;

        if (uiCamera == null)
            uiCamera = Camera.main;

        if (uiCamera == null)
            uiCamera = FindAnyObjectByType<Camera>();

        pauseAction = new InputAction(
            "Pause",
            InputActionType.Button,
            "<XRController>{LeftHand}/menuButton");
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.performed += OnPausePressed;

        BuildUI();
        SetPaused(false);
    }

    void OnEnable()
    {
        pauseAction?.Enable();

        if (sessionManager != null)
        {
            sessionManager.SessionStarted += ClosePauseMenu;
            sessionManager.SessionEnded += ClosePauseMenu;
        }
    }

    void OnDisable()
    {
        pauseAction?.Disable();

        if (sessionManager != null)
        {
            sessionManager.SessionStarted -= ClosePauseMenu;
            sessionManager.SessionEnded -= ClosePauseMenu;
        }

        RestoreTimeScale();
    }

    void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePressed;
            pauseAction.Dispose();
        }

        RestoreTimeScale();
    }

    void OnValidate()
    {
        planeDistance = Mathf.Max(0.1f, planeDistance);
        viewportScale = Mathf.Clamp(viewportScale, 0.5f, 1f);
    }

    void OnPausePressed(InputAction.CallbackContext context)
    {
        if (isPaused)
        {
            Continue();
            return;
        }

        if (sessionManager != null && !sessionManager.IsSessionActive)
            return;

        SetPaused(true);
    }

    void BuildUI()
    {
        if (uiCamera == null)
        {
            Debug.LogWarning(
                "PauseUI could not find a camera for its canvas.",
                this);
            return;
        }

        int uiLayer = LayerMask.NameToLayer(UILayerName);

        if (uiLayer < 0)
        {
            Debug.LogError(
                $"PauseUI requires the '{UILayerName}' layer.",
                this);
            return;
        }

        canvasObject = new GameObject(
            "Pause UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(TrackedDeviceGraphicRaycaster));
        canvasObject.layer = uiLayer;

        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = Mathf.Max(
            planeDistance,
            uiCamera.nearClipPlane + 0.01f);
        canvas.overrideSorting = true;
        canvas.sortingOrder = 110;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        GameObject contentObject = new GameObject(
            "Pause Content",
            typeof(RectTransform));
        contentObject.layer = canvasObject.layer;

        RectTransform contentRect =
            contentObject.GetComponent<RectTransform>();
        contentRect.SetParent(canvasRect, false);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.localScale = Vector3.one * viewportScale;

        Image background = CreateImage(
            "Background",
            contentRect,
            Vector2.zero,
            new Vector2(620f, 390f),
            new Color(0.015f, 0.035f, 0.055f, 0.97f));
        background.raycastTarget = true;

        Outline outline = background.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.75f, 0.85f, 1f);
        outline.effectDistance = new Vector2(5f, -5f);

        CreateText(
            "Title",
            contentRect,
            "PAUSED",
            new Vector2(0f, 120f),
            new Vector2(560f, 70f),
            46f,
            new Color(0.2f, 0.95f, 1f, 1f),
            TextAlignmentOptions.Center,
            FontStyles.Bold);

        continueButton = CreateButton(
            "Continue Button",
            contentRect,
            "CONTINUE",
            new Vector2(0f, 25f),
            Continue);

        CreateButton(
            "Main Menu Button",
            contentRect,
            "MAIN MENU",
            new Vector2(0f, -80f),
            OpenMainMenu);

        SetLayerRecursively(canvasObject, uiLayer);
        EnableDistanceUIInteraction(uiLayer);
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
        rect.sizeDelta = new Vector2(300f, 72f);

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
            27f,
            Color.white,
            TextAlignmentOptions.Center,
            FontStyles.Bold);

        return button;
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

    static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;

        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (canvasObject != null)
            canvasObject.SetActive(paused);

        if (paused &&
            EventSystem.current != null &&
            continueButton != null)
        {
            continueButton.Select();
        }
    }

    void ClosePauseMenu()
    {
        SetPaused(false);
    }

    public void Continue()
    {
        SetPaused(false);
    }

    public void OpenMainMenu()
    {
        string sceneName = string.IsNullOrWhiteSpace(mainMenuSceneName)
            ? DefaultMainMenuSceneName
            : mainMenuSceneName;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning(
                $"Main Menu scene '{sceneName}' is not in Build Settings.",
                this);
            return;
        }

        RestoreTimeScale();
        SceneManager.LoadScene(sceneName);
    }

    static void RestoreTimeScale()
    {
        Time.timeScale = 1f;
    }
}
