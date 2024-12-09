using HarmonyLib;
using ModCore.Services;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(LocalizationManager))]
public static class LocalizationManagerPatch
{
    [HarmonyPostfix, HarmonyPatch("LoadLanguage")]
    public static void LoadLanguage_Postfix()
    {
        LocalizationService.LoadLanguage();
    }
}