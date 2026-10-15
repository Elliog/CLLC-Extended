using System;
using System.Collections.Generic;
using UnityEngine;

namespace CllcExtended.Modifiers;

/// <summary>
/// 64x64 modifier icons decoded from <see cref="ModifierIconData"/>, ported from CreatureManager by
/// sighsorry (GPL-3.0, ModifierIconSource.cs): base64 runs of 2-bit tones (tone in the top 2 bits,
/// run length - 1 in the low 6), drawn with a 3-colour palette; tone 0 is transparent.
/// </summary>
internal static class ModifierIcons
{
    private const int Size = 64;

    private static readonly Dictionary<CreatureModifiers, Sprite> Sprites = new();

    /// <summary>The icon of a single modifier, created on first use.</summary>
    internal static Sprite For(CreatureModifiers modifier)
    {
        if (!Sprites.TryGetValue(modifier, out var sprite) || sprite == null)
        {
            var spec = ModifierIconData.Specs[modifier];
            sprite = Create("CllcxIcon" + modifier, spec.Tones, spec.Palette);
            Sprites[modifier] = sprite;
        }

        return sprite;
    }

    private static Sprite Create(string name, string packedTones, Color32[] palette)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var tones = Unpack(packedTones);
        var pixels = new Color32[tones.Length];
        for (var i = 0; i < tones.Length; i++)
        {
            pixels[i] = tones[i] == 0 ? new Color32(0, 0, 0, 0) : palette[tones[i] - 1];
        }

        // Pixel i is (x = i % Size, y = i / Size), the same order as Texture2D.SetPixels32.
        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    private static byte[] Unpack(string packedTones)
    {
        var tones = new byte[Size * Size];
        var next = 0;
        foreach (var run in Convert.FromBase64String(packedTones))
        {
            var tone = (byte)(run >> 6);
            var count = (run & 0x3F) + 1;
            if (next + count > tones.Length)
            {
                throw new InvalidOperationException("Invalid modifier icon data.");
            }

            for (var i = 0; i < count; i++)
            {
                tones[next++] = tone;
            }
        }

        if (next != tones.Length)
        {
            throw new InvalidOperationException("Incomplete modifier icon data.");
        }

        return tones;
    }
}
