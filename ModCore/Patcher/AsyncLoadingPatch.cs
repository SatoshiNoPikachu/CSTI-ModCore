using HarmonyLib;
using ModCore.Data;

namespace ModCore.Patcher;

[HarmonyPatch(typeof(AsyncLoading))]
internal static class AsyncLoadingPatch
{
    [HarmonyPostfix, HarmonyPatch("SetInitLoadingScreen")]
    public static void SetInitLoadingScreen_Postfix(InitLoadingState _State)
    {
        if (_State is InitLoadingState.Loading) Loader.LoadingScreen.Loading();
    }
    
    [HarmonyPrefix, HarmonyPatch("LoadScene")]
    public static bool LoadScene_Prefix()
    {
        return Loader.LoadingScreen.OnAsyncLoadingLoadScene();
    }
}