using BepInEx;
using HarmonyLib;
using ModCore.Services;

namespace ModCore;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[ModNamespace(PluginName)]
internal class Plugin : BaseUnityPlugin<Plugin>
{
    public const string PluginGuid = "Pikachu.CSTI.ModCore";
    public const string PluginName = "ModCore";
    public const string PluginVersion = "2.0.2";

    private static readonly Harmony Harmony = new(PluginGuid);

    protected override void OnAwake()
    {
        Harmony.PatchAll();

        ModService.Init();
        LocalizationService.Init();
    }
}