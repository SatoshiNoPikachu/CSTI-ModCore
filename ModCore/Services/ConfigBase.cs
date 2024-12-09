using BepInEx.Configuration;

namespace ModCore.Services;

/// <summary>
/// 配置泛型基类
/// </summary>
/// <typeparam name="TPlugin">派生自插件泛型基类的类型</typeparam>
public abstract class ConfigBase<TPlugin> where TPlugin : BaseUnityPlugin<TPlugin>
{
    /// <summary>
    /// 配置文件
    /// </summary>
    protected static ConfigFile Config => BaseUnityPlugin<TPlugin>.Instance.Config;

    /// <summary>
    /// 获取配置项
    /// </summary>
    /// <param name="section">节名称</param>
    /// <param name="key">键名称</param>
    /// <typeparam name="T">配置项值类型</typeparam>
    /// <returns>配置项</returns>
    public static ConfigEntry<T> Get<T>(string section, string key)
    {
        return Config.TryGetEntry<T>(section, key, out var config) ? config : null;
    }

    /// <summary>
    /// 获取布尔类型配置项值
    /// </summary>
    /// <param name="section">节名称</param>
    /// <param name="key">键名称</param>
    /// <returns>是否启用</returns>
    public static bool IsEnable(string section, string key)
    {
        var config = Get<bool>(section, key);
        return config is not null && config.Value;
    }

    /// <summary>
    /// 重新加载配置文件
    /// </summary>
    public static void Reload()
    {
        Config.Reload();
    }
}