using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class MainMenu : MonoBehaviour
{
    const string UILayerName = "UI";

    [Header("Scenes")]
    [SerializeField]
    string gameSceneName = "Game";

    [Header("Credits")]
    [SerializeField]
    Sprite profilePicture;

    [SerializeField]
    string creatorName = "EDUARDO HUERTA";

    [SerializeField]
    string creditsUrl = "https://kasairaito.itch.io";

    [SerializeField]
    string creditsLinkLabel = "VISIT ITCH.IO";

    [Header("World Space Display")]
    [SerializeField, Min(0.0001f)]
    float worldScale = 0.002f;

    [SerializeField]
    Camera eventCamera;

    GameObject canvasObject;
    GameObject mainPanel;
    GameObject creditsPanel;
    Button playButton;
    Button backButton;

    void Awake()
    {
        if (eventCamera == null)
            eventCamera = Camera.main;

        EnsureEventSystem();
        BuildUI();
    }

    void Start()
    {
        EnableDistanceUIInteraction();
        SelectButton(playButton);
    }

    void OnValidate()
    {
        worldScale = Mathf.Max(0.0001f, worldScale);
    }

    void BuildUI()
    {
        int uiLayer = LayerMask.NameToLayer(UILayerName);

        if (uiLayer < 0)
        {
            Debug.LogError(
                $"MainMenu requires the '{UILayerName}' layer.",
                this);
            return;
        }

        canvasObject = new GameObject(
            "Main Menu Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(TrackedDeviceGraphicRaycaster));
        canvasObject.layer = uiLayer;

        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();
        canvasRect.SetParent(transform, false);
        canvasRect.localPosition = Vector3.zero;
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one * worldScale;
        canvasRect.sizeDelta = new Vector2(800f, 650f);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = eventCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        Image background = CreateImage(
            "Background",
            canvasRect,
            Vector2.zero,
            canvasRect.sizeDelta,
            new Color(0.015f, 0.035f, 0.055f, 0.97f));
        background.raycastTarget = true;

        Outline outline = background.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.75f, 0.85f, 1f);
        outline.effectDistance = new Vector2(5f, -5f);

        mainPanel = CreateContainer("Main Panel", canvasRect);
        creditsPanel = CreateContainer("Credits Panel", canvasRect);

        BuildMainPanel(mainPanel.GetComponent<RectTransform>());
        BuildCreditsPanel(creditsPanel.GetComponent<RectTransform>());

        creditsPanel.SetActive(false);
        SetLayerRecursively(canvasObject, uiLayer);
        EnableDistanceUIInteraction();
    }

    void BuildMainPanel(RectTransform parent)
    {
        CreateText(
            "Title",
            parent,
            "DAY OF TOMORROW",
            new Vector2(0f, 210f),
            new Vector2(720f, 80f),
            48f,
            new Color(0.2f, 0.95f, 1f, 1f),
            TextAlignmentOptions.Center,
            FontStyles.Bold);

        CreateText(
            "Subtitle",
            parent,
            "XR TANK DEFENSE",
            new Vector2(0f, 145f),
            new Vector2(720f, 45f),
            24f,
            new Color(1f, 0.78f, 0.2f, 1f),
            TextAlignmentOptions.Center,
            FontStyles.Normal);

        playButton = CreateButton(
            "Play Button",
            parent,
            "PLAY",
            new Vector2(0f, 55f),
            Play);

        CreateButton(
            "Credits Button",
            parent,
            "CREDITS",
            new Vector2(0f, -45f),
            ShowCredits);

        CreateButton(
            "Quit Button",
            parent,
            "QUIT",
            new Vector2(0f, -145f),
            Quit);
    }

    void BuildCreditsPanel(RectTransform parent)
    {
        CreateText(
            "Credits Title",
            parent,
            "CREDITS",
            new Vector2(0f, 235f),
            new Vector2(720f, 65f),
            42f,
            new Color(0.2f, 0.95f, 1f, 1f),
            TextAlignmentOptions.Center,
            FontStyles.Bold);

        Image picture = CreateImage(
            "Profile Picture",
            parent,
            Vector2.zero,
            new Vector2(210f, 210f),
            profilePicture != null
                ? Color.white
                : new Color(0.08f, 0.3f, 0.38f, 1f));
        picture.sprite = profilePicture;
        picture.preserveAspect = true;
        picture.raycastTarget = false;

        if (profilePicture == null)
        {
            CreateText(
                "Profile Picture Placeholder",
                picture.rectTransform,
                "PFP",
                Vector2.zero,
                picture.rectTransform.sizeDelta,
                38f,
                Color.white,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
        }

        CreateText(
            "Creator Name",
            parent,
            creatorName,
            new Vector2(0f, 135f),
            new Vector2(620f, 50f),
            28f,
            Color.white,
            TextAlignmentOptions.Center,
            FontStyles.Bold);

        Button linkButton = CreateButton(
            "Credits Link Button",
            parent,
            creditsLinkLabel,
            new Vector2(0f, -155f),
            OpenCreditsLink);
        linkButton.interactable =
            !string.IsNullOrWhiteSpace(creditsUrl);

        backButton = CreateButton(
            "Back Button",
            parent,
            "BACK",
            new Vector2(0f, -245f),
            HideCredits);
    }

    GameObject CreateContainer(
        string objectName,
        RectTransform parent)
    {
        GameObject container = new GameObject(
            objectName,
            typeof(RectTransform));

        RectTransform rect = container.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return container;
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

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(320f, 72f);

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

    void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(XRUIInputModule));
    }

    void EnableDistanceUIInteraction()
    {
        int uiLayer = LayerMask.NameToLayer(UILayerName);

        if (uiLayer < 0)
            return;

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

    static void SelectButton(Button button)
    {
        if (EventSystem.current != null && button != null)
            button.Select();
    }

    public void Play()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogWarning(
                "MainMenu Game scene is not configured.",
                this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogWarning(
                $"Game scene '{gameSceneName}' is not in Build Settings.",
                this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowCredits()
    {
        if (mainPanel == null || creditsPanel == null)
            return;

        mainPanel.SetActive(false);
        creditsPanel.SetActive(true);
        SelectButton(backButton);
    }

    public void HideCredits()
    {
        if (mainPanel == null || creditsPanel == null)
            return;

        creditsPanel.SetActive(false);
        mainPanel.SetActive(true);
        SelectButton(playButton);
    }

    public void OpenCreditsLink()
    {
        if (string.IsNullOrWhiteSpace(creditsUrl))
        {
            Debug.LogWarning(
                "MainMenu Credits URL is not configured.",
                this);
            return;
        }

        Application.OpenURL(creditsUrl);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
