using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class ProceduralBackground : MaskableGraphic
{
    private const int WaveSegments = 96;
    private const float WaveAmplitude = 10f;
    private const float WaveFrequency = 0.015f;
    private const float WaveSpeed = 0.8f;
    private const float WaterlineNormalized = 0.40f;

    private static readonly Color32 SkyTop =
    new(0x7C, 0x8C, 0x88, 0xFF);

    private static readonly Color32 SkyBottom =
        new(0x64, 0x77, 0x70, 0xFF);

    private static readonly Color32 VillageBody =
        new(0x2A, 0x2E, 0x2B, 0xFF);

    private static readonly Color32 VillageDark =
        new(0x1A, 0x1E, 0x1B, 0xFF);

    private static readonly Color32 WaterTop =
        new(0x5A, 0x61, 0x4F, 0xFF);

    private static readonly Color32 WaterBottom =
        new(0x3A, 0x49, 0x44, 0xFF);

    private static readonly Color32 DebrisColor =
        new(0x4B, 0x36, 0x25, 0xFF);

    private static readonly Color32 VignetteClear =
        new(0, 0, 0, 0);

    private static readonly Color32 VignetteEdge =
        new(0, 0, 0, 0x28);

    private static readonly Color32 HorizonMistBottom =
    new(0xE7, 0xE1, 0xCD, 0x44);

    private static readonly Color32 HorizonMistTop =
        new(0xE7, 0xE1, 0xCD, 0x00);

    private static readonly Color32 WaterSheenBottom =
    new(0xE4, 0xDC, 0xC4, 0x00);

    private static readonly Color32 WaterSheenTop =
        new(0xE4, 0xDC, 0xC4, 0x20);

    private readonly House[] houses = new House[7];
    private readonly Palm[] palms = new Palm[4];
    private readonly float[] polePositions = { 0.15f, 0.50f, 0.84f };
    private readonly Debris[] debris = new Debris[3];

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        InitializeLayout();
        SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        InitializeLayout();
        SetVerticesDirty();
    }

    private void Update()
    {
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        DrawSky(vertexHelper, rect);
        DrawHorizonMist(vertexHelper, rect);
        DrawVillage(vertexHelper, rect);
        DrawWater(vertexHelper, rect);
        DrawWaterSheen(vertexHelper, rect);
        DrawDebris(vertexHelper, rect);
        DrawVignette(vertexHelper, rect);
    }

    private void InitializeLayout()
    {
        var random = new System.Random(213);
        for (int i = 0; i < houses.Length; i++)
        {
            float t = (i + 0.5f) / houses.Length;
            houses[i] = new House
            {
                x = Mathf.Clamp01(t + Range(random, -0.045f, 0.045f)),
                width = Range(random, 90f, 120f),
                height = Range(random, 60f, 80f),
                roofHeight = Range(random, 35f, 45f),
                lean = Range(random, 1f, 3f) * (i % 2 == 0 ? -1f : 1f)
            };
        }

        float[] palmPositions = { 0.08f, 0.37f, 0.64f, 0.93f };
        for (int i = 0; i < palms.Length; i++)
        {
            palms[i] = new Palm
            {
                x = Mathf.Clamp01(palmPositions[i] + Range(random, -0.025f, 0.025f)),
                lean = Range(random, 5f, 15f) * (i % 2 == 0 ? -1f : 1f),
                height = Range(random, 100f, 140f),
                frondLength = Range(random, 50f, 70f)
            };
        }

        for (int i = 0; i < debris.Length; i++)
        {
            debris[i] = new Debris
            {
                start = Range(random, 0f, 1f),
                depth = Range(random, 0.08f, 0.29f),
                speed = Range(random, 0.018f, 0.035f),
                rotation = Range(random, -8f, 8f),
                scale = Range(random, 0.8f, 1.2f)
            };
        }
    }

    private static void DrawSky(VertexHelper vh, Rect rect)
    {
        float y = rect.yMin + rect.height * WaterlineNormalized;
        AddQuad(vh, new(rect.xMin, y), new(rect.xMax, y), new(rect.xMax, rect.yMax),
            new(rect.xMin, rect.yMax), SkyBottom, SkyBottom, SkyTop, SkyTop);
    }

    private static void DrawHorizonMist(VertexHelper vh, Rect rect)
    {
        float waterline = rect.yMin + rect.height * WaterlineNormalized;
        float y0 = waterline - 10f;
        float y1 = waterline + rect.height * 0.14f;

        AddQuad(vh,
            new(rect.xMin, y0),
            new(rect.xMax, y0),
            new(rect.xMax, y1),
            new(rect.xMin, y1),
            HorizonMistBottom, HorizonMistBottom,
            HorizonMistTop, HorizonMistTop);
    }

    private void DrawVillage(VertexHelper vh, Rect rect)
    {
        float canvasHeight = rect.height;
        float waterLineY = canvasHeight * WaterlineNormalized;
        float waterline = rect.yMin + waterLineY;
        foreach (House house in houses)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, house.x);
            float bottom = waterline - 20f;
            Vector2 bodyCenter = new(x, bottom + house.height * 0.5f);
            AddRect(vh, bodyCenter, new Vector2(house.width, house.height), house.lean, VillageBody);

            float roofBase = bottom + house.height;
            float roofWidth = house.width + 40f;
            Vector2 roofLeft = RotateAround(new Vector2(x - roofWidth * 0.5f, roofBase), bodyCenter, house.lean);
            Vector2 roofRight = RotateAround(new Vector2(x + roofWidth * 0.5f, roofBase), bodyCenter, house.lean);
            Vector2 roofPeak = RotateAround(new Vector2(x, roofBase + house.roofHeight), bodyCenter, house.lean);
            AddTriangle(vh, roofLeft, roofRight, roofPeak, VillageDark);
        }

        foreach (Palm palm in palms)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, palm.x);
            Vector2 trunkCenter = new(x, waterline - 18f + palm.height * 0.5f);
            AddTaperedRect(vh, trunkCenter, 8f, 5f, palm.height, palm.lean, VillageBody);
            Vector2 crown = trunkCenter + Rotate(new Vector2(0f, palm.height * 0.5f), palm.lean);
            float[] frondAngles = { -40f, 0f, 35f, 75f, 120f };
            foreach (float angle in frondAngles)
            {
                float rotation = angle + palm.lean;
                Vector2 offset = Rotate(new Vector2(palm.frondLength * 0.28f, 0f), rotation);
                AddEllipse(vh, crown + offset, new Vector2(palm.frondLength, 8f),
                    rotation, VillageDark, 12);
            }
        }

        float[] poleHeights = { 168f, 180f, 164f };
        Vector2[] wirePoints = new Vector2[polePositions.Length];
        for (int i = 0; i < polePositions.Length; i++)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, polePositions[i]);
            float poleBottom = waterline - 22f;
            float height = poleHeights[i];
            float crossbarY = poleBottom + height * 0.85f;
            AddRect(vh, new(x, poleBottom + height * 0.5f), new(5f, height), 0f, VillageBody);
            AddRect(vh, new(x, crossbarY), new(40f, 4f), 0f, VillageBody);
            wirePoints[i] = new Vector2(x, crossbarY);
        }

        for (int i = 0; i < polePositions.Length - 1; i++)
        {
            AddSaggingWire(vh, wirePoints[i], wirePoints[i + 1], 15f, 1.5f, VillageBody);
        }
    }

    private static void DrawWater(VertexHelper vh, Rect rect)
    {
        float canvasHeight = rect.height;
        float waterLineY = canvasHeight * WaterlineNormalized;
        float baseY = rect.yMin + waterLineY;
        for (int i = 0; i < WaveSegments; i++)
        {
            float t0 = i / (float)WaveSegments;
            float t1 = (i + 1) / (float)WaveSegments;
            float x0 = Mathf.Lerp(rect.xMin, rect.xMax, t0);
            float x1 = Mathf.Lerp(rect.xMin, rect.xMax, t1);
            float y0 = baseY + WaveAmplitude * Mathf.Sin(x0 * WaveFrequency + Time.time * WaveSpeed);
            float y1 = baseY + WaveAmplitude * Mathf.Sin(x1 * WaveFrequency + Time.time * WaveSpeed);
            AddQuad(vh, new(x0, rect.yMin), new(x1, rect.yMin), new(x1, y1), new(x0, y0),
                WaterBottom, WaterBottom, WaterTop, WaterTop);
        }
    }

    private static void DrawWaterSheen(VertexHelper vh, Rect rect)
    {
        float y0 = rect.yMin + rect.height * 0.22f;
        float y1 = rect.yMin + rect.height * 0.34f;

        AddQuad(vh,
            new(rect.xMin, y0),
            new(rect.xMax, y0),
            new(rect.xMax, y1),
            new(rect.xMin, y1),
            WaterSheenBottom, WaterSheenBottom,
            WaterSheenTop, WaterSheenTop);
    }

    private void DrawDebris(VertexHelper vh, Rect rect)
    {
        float waterline = rect.yMin + rect.height * WaterlineNormalized;
        for (int i = 0; i < debris.Length; i++)
        {
            Debris item = debris[i];
            float x = Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Repeat(item.start - Time.time * item.speed, 1.1f) - 0.05f);
            float y = waterline - rect.height * item.depth + 2f * Mathf.Sin(Time.time * 0.9f + i * 1.7f);
            AddRect(vh, new(x, y), new Vector2(20f, 8f) * item.scale, item.rotation, DebrisColor);
        }
    }

    private static void DrawVignette(VertexHelper vh, Rect rect)
    {
        const int segments = 40;
        Vector2 center = rect.center;
        float innerRadius = Mathf.Min(rect.width, rect.height) * 0.34f;
        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.PI * 2f * i / segments;
            float a1 = Mathf.PI * 2f * (i + 1) / segments;
            Vector2 d0 = new(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector2 d1 = new(Mathf.Cos(a1), Mathf.Sin(a1));
            Vector2 inner0 = center + d0 * innerRadius;
            Vector2 inner1 = center + d1 * innerRadius;
            Vector2 outer0 = center + d0 * RayToRectEdge(rect, center, d0);
            Vector2 outer1 = center + d1 * RayToRectEdge(rect, center, d1);
            AddQuad(vh, inner0, inner1, outer1, outer0,
                VignetteClear, VignetteClear, VignetteEdge, VignetteEdge);
        }
    }

    private static float RayToRectEdge(Rect rect, Vector2 origin, Vector2 direction)
    {
        float tx = direction.x > 0f ? (rect.xMax - origin.x) / direction.x
            : direction.x < 0f ? (rect.xMin - origin.x) / direction.x : float.MaxValue;
        float ty = direction.y > 0f ? (rect.yMax - origin.y) / direction.y
            : direction.y < 0f ? (rect.yMin - origin.y) / direction.y : float.MaxValue;
        return Mathf.Min(Mathf.Abs(tx), Mathf.Abs(ty)) + 2f;
    }

    private static void AddRect(VertexHelper vh, Vector2 center, Vector2 size, float angle, Color32 color)
    {
        Vector2 half = size * 0.5f;
        AddQuad(vh, center + Rotate(new(-half.x, -half.y), angle),
            center + Rotate(new(half.x, -half.y), angle),
            center + Rotate(new(half.x, half.y), angle),
            center + Rotate(new(-half.x, half.y), angle), color, color, color, color);
    }

    private static void AddTaperedRect(VertexHelper vh, Vector2 center, float baseWidth,
        float topWidth, float height, float angle, Color32 color)
    {
        Vector2 bottomLeft = center + Rotate(new Vector2(-baseWidth * 0.5f, -height * 0.5f), angle);
        Vector2 bottomRight = center + Rotate(new Vector2(baseWidth * 0.5f, -height * 0.5f), angle);
        Vector2 topRight = center + Rotate(new Vector2(topWidth * 0.5f, height * 0.5f), angle);
        Vector2 topLeft = center + Rotate(new Vector2(-topWidth * 0.5f, height * 0.5f), angle);
        AddQuad(vh, bottomLeft, bottomRight, topRight, topLeft, color, color, color, color);
    }

    private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
    {
        int index = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero);
        vh.AddVert(b, color, Vector2.right);
        vh.AddVert(c, color, Vector2.up);
        vh.AddTriangle(index, index + 1, index + 2);
    }

    private static void AddTrapezoid(VertexHelper vh, float x, float bottom, float bottomWidth,
        float topWidth, float height, Color32 color)
    {
        AddQuad(vh, new(x - bottomWidth * 0.5f, bottom), new(x + bottomWidth * 0.5f, bottom),
            new(x + topWidth * 0.5f, bottom + height), new(x - topWidth * 0.5f, bottom + height),
            color, color, color, color);
    }

    private static void AddEllipse(VertexHelper vh, Vector2 center, Vector2 size, float angle,
        Color32 color, int segments)
    {
        int centerIndex = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float radians = Mathf.PI * 2f * i / segments;
            Vector2 point = new(Mathf.Cos(radians) * size.x * 0.5f, Mathf.Sin(radians) * size.y * 0.5f);
            vh.AddVert(center + Rotate(point, angle), color, Vector2.zero);
            if (i > 0)
            {
                vh.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
            }
        }
    }

    private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color32 color)
    {
        Vector2 direction = (b - a).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * thickness * 0.5f;
        AddQuad(vh, a - normal, b - normal, b + normal, a + normal, color, color, color, color);
    }

    private static void AddSaggingWire(VertexHelper vh, Vector2 start, Vector2 end,
        float sag, float thickness, Color32 color)
    {
        const int segments = 18;
        Vector2 control = (start + end) * 0.5f + Vector2.down * sag * 2f;
        Vector2 previous = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float inverse = 1f - t;
            Vector2 current = inverse * inverse * start + 2f * inverse * t * control + t * t * end;
            AddLine(vh, previous, current, thickness, color);
            previous = current;
        }
    }

    private static void AddQuad(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
        Color32 c0, Color32 c1, Color32 c2, Color32 c3)
    {
        int index = vh.currentVertCount;
        vh.AddVert(p0, c0, Vector2.zero);
        vh.AddVert(p1, c1, Vector2.right);
        vh.AddVert(p2, c2, Vector2.one);
        vh.AddVert(p3, c3, Vector2.up);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    private static Vector2 Rotate(Vector2 value, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    private static Vector2 RotateAround(Vector2 point, Vector2 pivot, float degrees)
    {
        return pivot + Rotate(point - pivot, degrees);
    }

    private static float Range(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private struct House
    {
        public float x;
        public float width;
        public float height;
        public float roofHeight;
        public float lean;
    }

    private struct Palm
    {
        public float x;
        public float lean;
        public float height;
        public float frondLength;
    }
    private struct Debris { public float start; public float depth; public float speed; public float rotation; public float scale; }
}
