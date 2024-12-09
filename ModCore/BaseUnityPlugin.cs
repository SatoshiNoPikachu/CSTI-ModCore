using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace ModCore;

/// <summary>
/// 插件泛型基类
/// </summary>
/// <typeparam name="T">派生自本类的类型</typeparam>
public abstract class BaseUnityPlugin<T> : BaseUnityPlugin where T : BaseUnityPlugin<T>
{
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
    public static string PluginPath => Path.GetDirectoryName(Instance.Info.Location);

    /// <summary>
    /// 初始化插件
    /// </summary>
    protected virtual void Awake()
    {
        Instance = (T)this;
        Log = Logger;
    }
}