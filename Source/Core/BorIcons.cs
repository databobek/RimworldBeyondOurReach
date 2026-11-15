using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Gizmo and menu icons from Textures/BOR/UI/Icons. Loaded on first use, so no static constructor is needed.
public static class BorIcons
{
    const string Folder = "BOR/UI/Icons/";

    static readonly Dictionary<string, Texture2D> cache = new();

    public static Texture2D Get(string name)
    {
        if (!cache.TryGetValue(name, out Texture2D? tex))
        {
            cache[name] = tex = ContentFinder<Texture2D>.Get(Folder + name);
        }
        return tex;
    }

    public static Texture2D BodysuitArmor => Get("Bodysuit_Armor");
    public static Texture2D BodysuitCamo => Get("Bodysuit_Camo");
    public static Texture2D BodysuitWork => Get("Bodysuit_Work");
    public static Texture2D OrientisBlessing => Get("Orientis_Blessing");
    public static Texture2D OrientisMeditate => Get("Orientis_Meditate");

    // Pod gizmo icon per mode.
    public static Texture2D Pod(TenebralPodMode mode) => Get(mode switch
    {
        TenebralPodMode.Healing => "HealingPod_Medical",
        TenebralPodMode.Operating => "HealingPod_Operation",
        TenebralPodMode.Transformation => "HealingPod_Transformation",
        _ => "HealingPod_Stasis",
    });
}
