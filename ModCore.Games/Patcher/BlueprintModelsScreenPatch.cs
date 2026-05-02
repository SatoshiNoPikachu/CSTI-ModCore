using HarmonyLib;
using ModCore.Games.Blueprints;

namespace ModCore.Games.Patcher;

[HarmonyPatch(typeof(BlueprintModelsScreen))]
internal static class BlueprintModelsScreenPatch
{
    [HarmonyPostfix, HarmonyPatch("Awake")]
    public static void Awake_Postfix(BlueprintModelsScreen __instance)
    {
        BlueprintTabCtrl.OnBlueprintModelsScreenAwake(__instance);
    }

    [HarmonyPostfix, HarmonyPatch("Show")]
    public static void Show_Postfix()
    {
        BlueprintTabCtrl.Instance?.OnShow();
    }
}