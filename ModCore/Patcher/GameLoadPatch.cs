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
        Loader.LoadAsync();
    }

    [HarmonyFinalizer]
    [HarmonyPatch("LoadOptions")]
    [HarmonyPatch("LoadMainGameData")]
    [HarmonyPatch("LoadGameFilesData")]
    [HarmonyPatch("ImportOldSaves")]
    public static void GameLoad_Finalizer(Exception? __exception)
    {
        if (__exception is null) return;
        Loader.LoadingScreen.OnError();
    }
}