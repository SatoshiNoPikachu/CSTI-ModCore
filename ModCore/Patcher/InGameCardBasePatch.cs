using HarmonyLib;
using ModCore.Games.ExtraDataModule;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(InGameCardBase))]
internal static class InGameCardBasePatch
{
    [HarmonyPrefix, HarmonyPatch("Init")]
    public static void Init_Prefix(InGameCardBase __instance, List<CollectionDropsSaveData>? _CollectionDrops)
    {
        CardExtraData.OnCardInit(__instance, _CollectionDrops);
    }

    [HarmonyPostfix, HarmonyPatch("Save")]
    public static void Save_Postfix(InGameCardBase __instance, CardSaveData __result)
    {
        CardExtraData.OnCardSave(__instance, __result);
    }

    [HarmonyPostfix, HarmonyPatch("SaveInventory")]
    public static void SaveInventory_Postfix(InGameCardBase __instance, InventoryCardSaveData __result)
    {
        CardExtraData.OnCardSave(__instance, __result);
    }

    [HarmonyPostfix, HarmonyPatch("ResetCard")]
    public static IEnumerator ResetCard_Postfix(IEnumerator __result, InGameCardBase __instance)
    {
        while (__result.MoveNext()) yield return __result.Current;

        CardExtraData.OnCardReset(__instance);
    }
}