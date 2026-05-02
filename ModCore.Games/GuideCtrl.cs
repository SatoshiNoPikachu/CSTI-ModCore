using ModCore.Services;

namespace ModCore.Games;

public static class GuideCtrl
{
    internal static void OnLoadComplete()
    {
        var guideManager = GuideManager.Instance;

        foreach (var mod in ModService.Mods)
        {
            var dict = mod.GetData<GuideEntry>();
            if (dict is not null) guideManager.AllEntries.AddRange(dict.Values);
        }

        guideManager.GenerateAllPages();
    }
}