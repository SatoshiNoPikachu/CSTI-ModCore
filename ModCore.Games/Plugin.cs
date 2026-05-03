using BepInEx;
using HarmonyLib;
using ModCore.Data;
using ModCore.Games.Blueprints;

namespace ModCore.Games;

[BepInDependency(ModCoreGuid, ModCoreVersion)]
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[ModNamespace("ModCore")]
internal class Plugin : BaseUnityPlugin<Plugin>
{
    private const string PluginGuid = "Pikachu.CSTI.ModCore.Games";
    public const string PluginName = "ModCore.Games";
    public const string PluginVersion = "3.2.0";

    private static readonly Harmony Harmony = new(PluginGuid);

    protected override void OnAwake()
    {
        if (ModData is null) return;

        Harmony.PatchAll();

        Loader.LoadCompleteEvent += OnLoadComplete;
    }

    private static void OnLoadComplete()
    {
        GuideCtrl.OnLoadComplete();

        BlueprintTab.OnLoadComplete();
    }
}