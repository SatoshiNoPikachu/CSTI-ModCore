using System;
using HarmonyLib;
using ModCore.Data;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(GameLoad))]
internal static class GameLoadPatch
{
    [HarmonyPostfix, HarmonyPatch("LoadGameFilesData")]
    public static void LoadGameFilesData_Postfix()
    {
        try
        {
            Loader.LoadAllData();
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
        }
    }
}