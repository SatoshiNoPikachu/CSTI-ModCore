using HarmonyLib;

namespace ModCore.Games.Patcher;

[HarmonyPatch(typeof(GuideManager))]
internal static class GuideManagerPatch
{
    [HarmonyPrefix, HarmonyPatch("Start")]
    public static bool Start_Prefix()
    {
        return false;
    }
}