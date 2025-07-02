using HarmonyLib;
using ModCore.UI;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(UniqueIDScriptable))]
internal static class UniqueIDScriptablePatch
{
    [HarmonyPrefix, HarmonyPatch("ClearDict"), HarmonyPriority(1000)]
    public static void ClearDict_Prefix()
    {
        LoadingScreen.Loading();
    }
}