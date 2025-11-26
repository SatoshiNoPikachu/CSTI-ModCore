using BepInEx;

namespace ModCore;

/// <summary>
/// 插件泛型基类
/// </summary>
/// <typeparam name="T">派生自本类的类型</typeparam>
public abstract class BaseUnityPlugin<T> : BaseUnityPlugin where T : BaseUnityPlugin<T>;