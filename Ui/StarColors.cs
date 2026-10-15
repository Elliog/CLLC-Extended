using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CllcExtended.Ui;

/// <summary>
/// Gives each star count above 2 its own body colour. The game only has variants for 1 and 2
/// stars (CLLC reuses the 2-star one above that); from 3 stars this sets the creature shader's
/// hue / saturation / value offsets, the same properties the game's variants use. Palette and
/// approach (all skinned meshes, one material copy per colour) from StarLevelSystem by MidnightsFX
/// (GPL-3.0, Colorization.cs and its default per-level colours). Runs on every client.
/// </summary>
[HarmonyPatch(typeof(LevelEffects), nameof(LevelEffects.SetupLevelVisualization))]
internal static class StarColors
{
    private const int FirstColoredStars = 3;

    // Hue, saturation, value offsets per star count (StarLevelSystem defaults for 3 to 10 stars).
    private static readonly Dictionary<int, (float Hue, float Saturation, float Value)> Palette = new()
    {
        [3] = (-0.04446793f, 0.05205864f, -0.191282f),
        [4] = (0.08774569f, -0.3959962f, 0.224104f),
        [5] = (-0.05712423f, 0.09905273f, -0.1369882f),
        [6] = (0.08566696f, -0.3398137f, -0.2270744f),
        [7] = (0.03924248f, -0.04047304f, -0.1966226f),
        [8] = (-0.3575756f, 0.09342755f, 0.3008582f),
        [9] = (0.3575756f, 0.09342755f, -0.1008582f),
        [10] = (0f, -0.25f, 0f),
    };

    private static readonly int Hue = Shader.PropertyToID("_Hue");
    private static readonly int Saturation = Shader.PropertyToID("_Saturation");
    private static readonly int Value = Shader.PropertyToID("_Value");

    // One coloured copy per (source material, star count), shared by every creature using it.
    private static readonly Dictionary<(int Source, int Stars), Material> Materials = new();

    // After CLLC, which extends the game's level setups to high levels.
    [HarmonyPostfix]
    [HarmonyAfter(Integration.CllcBridge.PluginGuid)]
    private static void Postfix(LevelEffects __instance, int level)
    {
        var stars = level - 1;
        if (stars < FirstColoredStars || CllcExtendedPlugin.Instance?.Settings.StarColors.Value != true)
        {
            return;
        }

        var color = Palette.TryGetValue(stars, out var exact) ? exact : Palette[Palette.Count + FirstColoredStars - 1];
        var character = __instance.GetComponentInParent<Character>();
        if (character == null)
        {
            return;
        }

        foreach (var renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var materials = renderer.sharedMaterials;
            if (materials.Length == 0 || materials[0] == null)
            {
                continue;
            }

            materials[0] = Colored(materials[0], stars, color);
            renderer.sharedMaterials = materials;
        }
    }

    private static Material Colored(Material source, int stars, (float Hue, float Saturation, float Value) color)
    {
        var key = (source.GetInstanceID(), stars);
        if (Materials.TryGetValue(key, out var colored) && colored != null)
        {
            return colored;
        }

        colored = new Material(source) { name = source.name + " (CLLCX " + stars + " stars)" };
        colored.SetFloat(Hue, color.Hue);
        colored.SetFloat(Saturation, color.Saturation);
        colored.SetFloat(Value, color.Value);
        Materials[key] = colored;
        // A creature recoloured again (level change) starts from this copy: map it to itself.
        Materials[(colored.GetInstanceID(), stars)] = colored;
        return colored;
    }
}
