using System.Collections.Generic;
using UnityEngine;

namespace CllcExtended.Ui;

/// <summary>Shape drawn behind each modifier icon.</summary>
public enum IconBackgroundShape
{
    None,
    Square,
    Circle,
    Star,
}

/// <summary>
/// White 64x64 masks (tinted by the configured colour) for the icon backgrounds, with 4x4
/// supersampled edges. The star test is ported from CreatureManager by sighsorry (GPL-3.0).
/// </summary>
internal static class IconBackgrounds
{
    private const int Size = 64;
    private const int Samples = 4;

    // Share of the background the icon covers, so it stays inside the shape.
    internal static float IconScale(IconBackgroundShape shape) =>
        shape switch
        {
            IconBackgroundShape.Square => 0.8f,
            IconBackgroundShape.Circle => 0.75f,
            IconBackgroundShape.Star => 0.55f,
            _ => 1f,
        };

    private static readonly Dictionary<IconBackgroundShape, Sprite> Sprites = new();

    internal static Sprite For(IconBackgroundShape shape)
    {
        if (!Sprites.TryGetValue(shape, out var sprite) || sprite == null)
        {
            sprite = Create(shape);
            Sprites[shape] = sprite;
        }

        return sprite;
    }

    private static Sprite Create(IconBackgroundShape shape)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
        {
            name = "CllcxIconBackground" + shape,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color32[Size * Size];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var covered = 0;
                for (var sy = 0; sy < Samples; sy++)
                {
                    for (var sx = 0; sx < Samples; sx++)
                    {
                        var point = new Vector2(x + (sx + 0.5f) / Samples, y + (sy + 0.5f) / Samples);
                        if (Inside(shape, point))
                        {
                            covered++;
                        }
                    }
                }

                var alpha = (byte)(255 * covered / (Samples * Samples));
                pixels[y * Size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    private static bool Inside(IconBackgroundShape shape, Vector2 point)
    {
        var center = new Vector2(Size / 2f, Size / 2f);
        return shape switch
        {
            IconBackgroundShape.Square => true,
            IconBackgroundShape.Circle => (point - center).sqrMagnitude <= (Size / 2f) * (Size / 2f),
            IconBackgroundShape.Star => IsFivePointStarPixel(point, center, Size / 2f, Size * 0.22f),
            _ => false,
        };
    }

    private static bool IsFivePointStarPixel(Vector2 point, Vector2 center, float outerRadius, float innerRadius)
    {
        var inside = false;
        var previous = StarVertex(center, outerRadius, innerRadius, 9);
        for (var index = 0; index < 10; index++)
        {
            var current = StarVertex(center, outerRadius, innerRadius, index);
            if ((current.y > point.y) != (previous.y > point.y)
                && point.x < (previous.x - current.x) * (point.y - current.y) / (previous.y - current.y) + current.x)
            {
                inside = !inside;
            }

            previous = current;
        }

        return inside;
    }

    private static Vector2 StarVertex(Vector2 center, float outerRadius, float innerRadius, int index)
    {
        var radius = index % 2 == 0 ? outerRadius : innerRadius;
        var angle = Mathf.PI * 0.5f + index * Mathf.PI / 5f;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
