using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ModCore.Data;
using ModCore.Services;

namespace ModCore;

/// <summary>
/// 插件泛型基类
/// </summary>
/// <typeparam name="T">派生自本类的类型</typeparam>
public abstract class BaseUnityPlugin<T> : BaseUnityPlugin where T : BaseUnityPlugin<T>
{
    /// <summary>
    /// 模组核心GUID
    /// </summary>
    public const string ModCoreGuid = Plugin.PluginGuid;

    /// <summary>
    /// 模组核心版本
    /// </summary>
    public const string ModCoreVersion = Plugin.PluginVersion;

    /// <summary>
    /// 插件实例
    /// </summary>
    public static T Instance { get; private set; } = null!;

    /// <summary>
    /// 日志
    /// </summary>
    public static ManualLogSource Log { get; private set; } = null!;

    /// <summary>
    /// 插件所在目录路径
    /// </summary>
    public static string PluginPath => Path.GetDirectoryName(Instance.Info.Location)!;

    /// <summary>
    /// 模组命名空间
    /// </summary>
    public static string? ModNamespace { get; private set; }

    /// <summary>
    /// 模组数据
    /// </summary>
    public static ModData? ModData { get; internal set; }

    /// <summary>
    /// 初始化插件
    /// </summary>
    protected void Awake()
    {
        Instance = (T)this;
        Log = Logger;

        ModNamespace = typeof(T).GetCustomAttribute<ModNamespaceAttribute>()?.Namespace;
        if (ModNamespace is not null) ModData = ModService.GetMod(ModNamespace);

        OnAwake();
    }

    protected virtual void OnAwake()
    {
    }
}