using System;
using HarmonyLib;
using ModCore.Data;
using ModCore.Games;
using ModCore.UI;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(GameManager))]
internal static class GameManagerPatch
{
    [HarmonyPrefix, HarmonyPatch("Awake"), HarmonyPriority(Priority.First)]
    public static void Awake_Prefix_First(GameManager __instance)
    {
        if (!Loader.IsLoaded)
        {
            Loader.DestroyGameSceneObjects();
            throw new Exception();
        }

        Game.Create(__instance);

        CommonPrefab.MakePrefabsOnGame();
    }

    [HarmonyFinalizer, HarmonyPatch("Awake"), HarmonyPriority(Priority.Last)]
    public static Exception? Awake_Finalizer(Exception? __exception)
    {
        if (__exception is not null && Loader.IsLoaded)
        {
            Plugin.Log.LogError($"Error on GameManager.Awake: {__exception}");
        }

        return null;
    }
}