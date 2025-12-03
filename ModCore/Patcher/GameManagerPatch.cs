using HarmonyLib;
using ModCore.Games;
using ModCore.UI;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(GameManager))]
internal static class GameManagerPatch
{
    [HarmonyPrefix, HarmonyPatch("Awake"), HarmonyPriority(Priority.First)]
    public static void Awake_Prefix_First(GameManager __instance)
    {
        Game.Create(__instance);
        
        CommonPrefab.MakePrefabsOnGame();
    }
}