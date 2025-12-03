using HarmonyLib;
using ModCore.UI;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(AsyncLoading))]
internal static class AsyncLoadingPatch
{
    [HarmonyPostfix, HarmonyPatch("SetInitLoadingScreen")]
    public static void SetInitLoadingScreen_Postfix(InitLoadingState _State)
    {
        if (_State is InitLoadingState.Loading) LoadingScreen.Loading();
    }
    
    [HarmonyPrefix, HarmonyPatch("LoadScene")]
    public static bool LoadScene_Prefix()
    {
        return LoadingScreen.OnAsyncLoadingLoadScene();
    }
}