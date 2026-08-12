using HarmonyLib;
using ModCore.Games.EquipEx;

namespace ModCore.Games.Patcher;

[HarmonyPatch(typeof(CharacterScreen))]
internal static class CharacterScreenPatch
{
    [HarmonyPostfix, HarmonyPatch("Init")]
    public static void Init_Postfix(CharacterScreen __instance)
    {
        EquipTab.AddTags(__instance);
        CharacterScreenEx.Create(__instance);
    }

    [HarmonyPrefix, HarmonyPatch("ChangeTab")]
    public static bool ChangeTab_Prefix(int _NewTab)
    {
        if (!CharacterScreenEx.Instance) return true;
        CharacterScreenEx.Instance.ChangeTab(_NewTab);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch("Open")]
    public static bool Open_Prefix()
    {
        if (!CharacterScreenEx.Instance) return true;
        CharacterScreenEx.Instance.Open();
        return false;
    }
}