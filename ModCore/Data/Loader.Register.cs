using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ModCore.Data;

public static partial class Loader
{
    /// <summary>
    /// 注册类型
    /// </summary>
    /// <param name="info">数据信息</param>
    /// <returns>是否注册成功</returns>
    public static bool RegisterType(DataInfo info)
    {
        if (IsLoaded) return false;
        if (!DataInfos.TryAdd(info.Name, info)) return false;

        Plugin.Log.LogInfo($"Registered type {info.Name} ({info.Type.FullName}) in loader.");
        return true;
    }

    /// <summary>
    /// 注册类型
    /// </summary>
    /// <param name="infos">数据信息</param>
    public static void RegisterTypes(IEnumerable<DataInfo> infos)
    {
        if (IsLoaded) return;

        foreach (var info in infos)
        {
            RegisterType(info);
        }
    }

    /// <summary>
    /// 初始化数据并自动注册类型
    /// </summary>
    private static void InitDatabaseAndAutoRegisterType()
    {
        foreach (var type in AccessTools.AllTypes())
        {
            if (type.IsInterface || type.IsGenericTypeDefinition) continue;
            if (type.IsAbstract) continue;

            var isSo = type.IsSubclassOf(typeof(ScriptableObject));
            if (isSo)
            {
                var objs = Resources.FindObjectsOfTypeAll(type);
                if (objs?.Length > 0)
                {
                    var dict = Database.GetData(type) ?? MakeDataDict(type);
                    if (dict is null) continue;

                    foreach (var obj in objs)
                    {
                        var so = (ScriptableObject)obj;
                        var name = so.name;
                        if (dict.Contains(name))
                        {
                            if (name != "") Plugin.Log.LogWarning($"{type.Name} has same name {name}.");
                            continue;
                        }

                        dict.Add(name, so);
                    }

                    Database.AddData(type, dict);
                }

                if (type.Module.Name is "Assembly-CSharp.dll") RegisterType(new DataInfo(type, false));
            }

            if (!type.IsSerializable && !isSo) continue;

            var attr = type.GetCustomAttribute<DataInfoAttribute>();
            if (attr is null) continue;

            RegisterType(new DataInfo(type, string.IsNullOrWhiteSpace(attr.Name) ? type.Name : attr.Name,
                attr.CanFallbackToRoot));
        }

        RegisterAudio();
    }

    private static void RegisterAudio()
    {
        var audios = Resources.FindObjectsOfTypeAll<AudioClip>();
        var dict = new Dictionary<string, AudioClip>();

        foreach (var audio in audios)
        {
            var n = audio.name;
            if (!dict.TryAdd(n, audio)) Plugin.Log.LogWarning($"AudioClip has same name {n}.");
        }

        Database.AddData(dict);
    }
}