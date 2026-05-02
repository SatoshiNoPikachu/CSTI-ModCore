using HarmonyLib;
using ModCore.Games.Blueprints;
using ModCore.Games.ScrollView;

namespace ModCore.Games.Patcher;

[HarmonyPatch(typeof(GameManager))]
internal static class GameManagerPatch
{
    [HarmonyPrefix, HarmonyPatch("Awake")]
    public static void Awake_Prefix()
    {
        BlueprintTab.AddMainTabs();
    }

    [HarmonyPostfix, HarmonyPatch("Awake")]
    public static void Awake_Postfix()
    {
        InspectionPopupScroll.Create();
    }
}