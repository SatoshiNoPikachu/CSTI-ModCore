using HarmonyLib;
using ModCore.Games.ExtraDataModule;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(InGameStat))]
internal static class InGameStatPatch
{
    [HarmonyPrefix, HarmonyPatch("Load")]
    public static void Load_Prefix(InGameStat __instance, StatSaveData _Data)
    {
        StatExtraData.OnStatLoad(__instance, _Data);
    }

    [HarmonyPostfix, HarmonyPatch("Save")]
    public static void Save_Postfix(InGameStat __instance, StatSaveData __result)
    {
        StatExtraData.OnStatSave(__instance, __result);
    }
}