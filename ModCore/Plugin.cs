using BepInEx;
using HarmonyLib;

namespace ModCore;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
internal class Plugin : BaseUnityPlugin<Plugin>
{
    public const string PluginGuid = "Pikachu.CSTI.ModCore";
    public const string PluginName = "ModCore";
    public const string PluginVersion = "1.1.0";

    private static readonly Harmony Harmony = new(PluginGuid);

    protected override void Awake()
    {
        base.Awake();
        Harmony.PatchAll();
        Logger.LogMessage($"Plugin {PluginName} is loaded!");
    }
}

public class ModCoreAfterAttribute() : HarmonyAfter(Plugin.PluginGuid);