using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class MapDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    RawImage mapImage;

    [SerializeField]
    Transform tank;

    [SerializeField]
    TargetSpawner targetSpawner;

    [Header("Map")]
    [SerializeField]
    bool useSpawnerRadius = true;

    [SerializeField, Min(1f)]
    float worldRadius = 50f;

    [SerializeField, Min(1f)]
    float mapPadding = 1.1f;

    [SerializeField, Min(64)]
    int textureHeight = 256;

    [SerializeField, Min(1f)]
    float refreshRate = 15f;

    [SerializeField, Min(1f)]
    float gridSpacing = 10f;

    [Header("Markers")]
    [SerializeField, Min(1)]
    int targetMarkerRadius = 4;

    [SerializeField, Min(1)]
    int tankMarkerRadius = 6;

    [SerializeField, Min(1)]
    int tankHeadingLength = 16;

    [Header("Colors")]
    [SerializeField]
    Color backgroundColor = new Color(0.015f, 0.03f, 0.04f, 1f);

    [SerializeField]
    Color gridColor = new Color(0.08f, 0.25f, 0.3f, 1f);

    [SerializeField]
    Color originColor = new Color(0.25f, 0.75f, 0.85f, 1f);

    [SerializeField]
    Color minimumRangeColor = new Color(0.8f, 0.35f, 0.1f, 1f);

    [SerializeField]
    Color maximumRangeColor = new Color(0.9f, 0.75f, 0.15f, 1f);

    [SerializeField]
    Color tankColor = new Color(0.2f, 1f, 0.25f, 1f);

    [SerializeField]
    Color targetColor = new Color(1f, 0.15f, 0.1f, 1f);

    Texture2D mapTexture;
    Color32[] pixels;
    int textureWidth;
    float nextRefreshTime;

    void Awake()
    {
        if (mapImage == null)
            mapImage = GetComponent<RawImage>();

        if (tank == null)
        {
            Tank tankComponent = GetComponentInParent<Tank>();

            if (tankComponent == null)
                tankComponent = FindAnyObjectByType<Tank>();

            if (tankComponent != null)
                tank = tankComponent.transform;
        }

        if (targetSpawner == null)
            targetSpawner = TargetSpawner.Instance;

        if (targetSpawner == null)
            targetSpawner = FindAnyObjectByType<TargetSpawner>();

        CreateMapTexture();
        RenderMap();
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + 1f / refreshRate;
        RenderMap();
    }

    void OnDestroy()
    {
        if (mapImage != null && mapImage.texture == mapTexture)
            mapImage.texture = null;

        if (mapTexture != null)
            Destroy(mapTexture);
    }

    void OnValidate()
    {
        worldRadius = Mathf.Max(1f, worldRadius);
        mapPadding = Mathf.Max(1f, mapPadding);
        textureHeight = Mathf.Max(64, textureHeight);
        refreshRate = Mathf.Max(1f, refreshRate);
        gridSpacing = Mathf.Max(1f, gridSpacing);
        targetMarkerRadius = Mathf.Max(1, targetMarkerRadius);
        tankMarkerRadius = Mathf.Max(1, tankMarkerRadius);
        tankHeadingLength = Mathf.Max(1, tankHeadingLength);
    }

    void CreateMapTexture()
    {
        if (mapImage == null)
            return;

        float rectHeight = mapImage.rectTransform.rect.height;
        float aspect = rectHeight > Mathf.Epsilon
            ? mapImage.rectTransform.rect.width / rectHeight
            : 1f;

        textureWidth = Mathf.Max(
            64,
            Mathf.RoundToInt(textureHeight * Mathf.Max(0.1f, aspect)));
        pixels = new Color32[textureWidth * textureHeight];
        mapTexture = new Texture2D(
            textureWidth,
            textureHeight,
            TextureFormat.RGBA32,
            false)
        {
            name = "Runtime Tactical Map",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };

        mapImage.texture = mapTexture;
        mapImage.raycastTarget = false;
    }

    void RenderMap()
    {
        if (mapTexture == null || pixels == null)
            return;

        Color32 background = backgroundColor;

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = background;

        float mapRadius = GetMapRadius();
        float horizontalRadius =
            mapRadius * textureWidth / (float)textureHeight;

        DrawGrid(mapRadius, horizontalRadius);

        if (targetSpawner != null)
        {
            DrawWorldCircle(
                targetSpawner.MinimumSpawnDistance,
                minimumRangeColor,
                mapRadius,
                horizontalRadius);
            DrawWorldCircle(
                targetSpawner.SpawnRadius,
                maximumRangeColor,
                mapRadius,
                horizontalRadius);
        }

        DrawOrigin(mapRadius, horizontalRadius);
        DrawTargets(mapRadius, horizontalRadius);
        DrawTank(mapRadius, horizontalRadius);
        DrawBorder(originColor);

        mapTexture.SetPixels32(pixels);
        mapTexture.Apply(false);
    }

    float GetMapRadius()
    {
        if (useSpawnerRadius &&
            targetSpawner != null &&
            targetSpawner.SpawnRadius > 0f)
        {
            return targetSpawner.SpawnRadius * mapPadding;
        }

        return worldRadius;
    }

    void DrawGrid(float mapRadius, float horizontalRadius)
    {
        float firstX =
            Mathf.Ceil(-horizontalRadius / gridSpacing) * gridSpacing;

        for (float x = firstX;
             x <= horizontalRadius;
             x += gridSpacing)
        {
            if (Mathf.Abs(x) < 0.01f)
                continue;

            DrawWorldLine(
                new Vector3(x, 0f, -mapRadius),
                new Vector3(x, 0f, mapRadius),
                gridColor,
                mapRadius,
                horizontalRadius);
        }

        float firstZ = Mathf.Ceil(-mapRadius / gridSpacing) * gridSpacing;

        for (float z = firstZ; z <= mapRadius; z += gridSpacing)
        {
            if (Mathf.Abs(z) < 0.01f)
                continue;

            DrawWorldLine(
                new Vector3(-horizontalRadius, 0f, z),
                new Vector3(horizontalRadius, 0f, z),
                gridColor,
                mapRadius,
                horizontalRadius);
        }
    }

    void DrawOrigin(float mapRadius, float horizontalRadius)
    {
        float armLength = gridSpacing * 0.35f;

        DrawWorldLine(
            new Vector3(-armLength, 0f, 0f),
            new Vector3(armLength, 0f, 0f),
            originColor,
            mapRadius,
            horizontalRadius);
        DrawWorldLine(
            new Vector3(0f, 0f, -armLength),
            new Vector3(0f, 0f, armLength),
            originColor,
            mapRadius,
            horizontalRadius);
    }

    void DrawTargets(float mapRadius, float horizontalRadius)
    {
        foreach (Target target in Target.ActiveTargets)
        {
            if (target == null || !target.isActiveAndEnabled)
                continue;

            if (TryWorldToPixel(
                    target.transform.position,
                    mapRadius,
                    horizontalRadius,
                    out Vector2Int position))
            {
                DrawFilledCircle(
                    position.x,
                    position.y,
                    targetMarkerRadius,
                    targetColor);
            }
        }
    }

    void DrawTank(float mapRadius, float horizontalRadius)
    {
        if (tank == null ||
            !TryWorldToPixel(
                tank.position,
                mapRadius,
                horizontalRadius,
                out Vector2Int position))
        {
            return;
        }

        DrawFilledCircle(
            position.x,
            position.y,
            tankMarkerRadius,
            tankColor);

        Vector2 heading = new Vector2(tank.forward.x, tank.forward.z);
        if (heading.sqrMagnitude <= Mathf.Epsilon)
            return;

        heading.Normalize();
        int headingX =
            Mathf.RoundToInt(position.x + heading.x * tankHeadingLength);
        int headingY =
            Mathf.RoundToInt(position.y + heading.y * tankHeadingLength);

        DrawLine(
            position.x,
            position.y,
            headingX,
            headingY,
            tankColor);
        DrawFilledCircle(headingX, headingY, 2, tankColor);
    }

    void DrawWorldCircle(
        float radius,
        Color32 color,
        float mapRadius,
        float horizontalRadius)
    {
        if (radius <= 0f)
            return;

        const int segmentCount = 128;
        Vector3 previousPoint = new Vector3(radius, 0f, 0f);

        for (int segment = 1; segment <= segmentCount; segment++)
        {
            float angle = segment * Mathf.PI * 2f / segmentCount;
            Vector3 nextPoint = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            DrawWorldLine(
                previousPoint,
                nextPoint,
                color,
                mapRadius,
                horizontalRadius);
            previousPoint = nextPoint;
        }
    }

    void DrawWorldLine(
        Vector3 start,
        Vector3 end,
        Color32 color,
        float mapRadius,
        float horizontalRadius)
    {
        if (!TryWorldToPixel(
                start,
                mapRadius,
                horizontalRadius,
                out Vector2Int startPixel) ||
            !TryWorldToPixel(
                end,
                mapRadius,
                horizontalRadius,
                out Vector2Int endPixel))
        {
            return;
        }

        DrawLine(
            startPixel.x,
            startPixel.y,
            endPixel.x,
            endPixel.y,
            color);
    }

    bool TryWorldToPixel(
        Vector3 worldPosition,
        float mapRadius,
        float horizontalRadius,
        out Vector2Int pixel)
    {
        float normalizedX =
            (worldPosition.x + horizontalRadius) /
            (horizontalRadius * 2f);
        float normalizedY =
            (worldPosition.z + mapRadius) / (mapRadius * 2f);

        pixel = new Vector2Int(
            Mathf.RoundToInt(normalizedX * (textureWidth - 1)),
            Mathf.RoundToInt(normalizedY * (textureHeight - 1)));

        return normalizedX >= 0f &&
               normalizedX <= 1f &&
               normalizedY >= 0f &&
               normalizedY <= 1f;
    }

    void DrawFilledCircle(int centerX, int centerY, int radius, Color32 color)
    {
        int radiusSquared = radius * radius;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radiusSquared)
                    SetPixel(centerX + x, centerY + y, color);
            }
        }
    }

    void DrawLine(
        int startX,
        int startY,
        int endX,
        int endY,
        Color32 color)
    {
        int deltaX = Mathf.Abs(endX - startX);
        int stepX = startX < endX ? 1 : -1;
        int deltaY = -Mathf.Abs(endY - startY);
        int stepY = startY < endY ? 1 : -1;
        int error = deltaX + deltaY;

        while (true)
        {
            SetPixel(startX, startY, color);

            if (startX == endX && startY == endY)
                break;

            int doubledError = error * 2;

            if (doubledError >= deltaY)
            {
                error += deltaY;
                startX += stepX;
            }

            if (doubledError <= deltaX)
            {
                error += deltaX;
                startY += stepY;
            }
        }
    }

    void DrawBorder(Color32 color)
    {
        DrawLine(0, 0, textureWidth - 1, 0, color);
        DrawLine(
            textureWidth - 1,
            0,
            textureWidth - 1,
            textureHeight - 1,
            color);
        DrawLine(
            textureWidth - 1,
            textureHeight - 1,
            0,
            textureHeight - 1,
            color);
        DrawLine(0, textureHeight - 1, 0, 0, color);
    }

    void SetPixel(int x, int y, Color32 color)
    {
        if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight)
            return;

        pixels[y * textureWidth + x] = color;
    }
}
