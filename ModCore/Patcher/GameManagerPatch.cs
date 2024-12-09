using HarmonyLib;
using ModCore.UI;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(GameManager))]
internal static class GameManagerPatch
{
    [HarmonyPrefix, HarmonyPatch("Awake"), HarmonyPriority(Priority.First)]
    public static void Awake_Prefix_First()
    {
        CommonPrefab.MakePrefabsOnGame();
    }
}