using HarmonyLib;
using ModCore.UI;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(AsyncLoading))]
internal static class AsyncLoadingPatch
{
    [HarmonyPrefix, HarmonyPatch("LoadScene")]
    public static bool LoadScene_Prefix()
    {
        return LoadingScreen.OnAsyncLoadingLoadScene();
    }
}