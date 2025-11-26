using BepInEx;

namespace ModCore;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
internal class Plugin : BaseUnityPlugin<Plugin>
{
    public const string PluginGuid = "Pikachu.CSTI.ModCore";
    public const string PluginName = "ModCore";
    public const string PluginVersion = "0.0.0.0";

    private void Awake()
    {
        Logger.LogWarning("""
                          无效的ModCore版本，该版本仅用于提供BepInEx对依赖插件的识别，但不会运行。请安装有效的ModCore版本！
                          This is an invalid ModCore version. It is intended solely for enabling BepInEx to recognize dependent plugins and will not run. Please install a valid ModCore version!
                          """);
    }
}