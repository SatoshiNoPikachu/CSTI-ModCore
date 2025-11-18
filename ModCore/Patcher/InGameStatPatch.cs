using HarmonyLib;
using ModCore.Games;
using ModCore.Games.ExtraDataModule;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(InGameStat))]
internal static class InGameStatPatch
{
    [HarmonyPrefix, HarmonyPatch("Load")]
    public static void Load_Prefix(InGameStat __instance, StatSaveData _Data)
    {
        StatExtraDataStorage.OnStatLoad(__instance, _Data);
    }

    [HarmonyPostfix, HarmonyPatch("Save")]
    public static void Save_Postfix(InGameStat __instance, StatSaveData __result)
    {
        StatExtraDataStorage.OnStatSave(__instance, __result);
    }
}