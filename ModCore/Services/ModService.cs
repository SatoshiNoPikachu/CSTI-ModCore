using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using LitJson;
using ModCore.Data;

namespace ModCore.Services;

/// <summary>
/// 模组服务
/// </summary>
public static class ModService
{
    private static readonly Dictionary<string, ModData> Mods = [];

    private static readonly string[] Targets = [BepInEx.Paths.PluginPath];

    /// <summary>
    /// 模组加载目录路径只读集合
    /// </summary>
    public static ReadOnlyCollection<string> ModLoadDirectories => new(Targets);

    /// <summary>
    /// 所有模组数据文件路径，可能包含未被成功加载的路径
    /// </summary>
    public static IEnumerable<string> ModMetaPaths => Targets.SelectMany(target =>
        Directory.EnumerateFiles(target, "ModMeta.json", SearchOption.AllDirectories));

    internal static void Init()
    {
        foreach (var path in ModMetaPaths)
        {
            LoadMod(path);
        }
    }

    private static void LoadMod(string path)
    {
        JsonData? meta;
        try
        {
            meta = JsonMapper.ToObject(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Load ModMeta failed: {e}");
            return;
        }

        if (meta is null) return;
        var ns = meta.ContainsKey("Namespace") && meta["Namespace"].IsString
            ? meta["Namespace"].ToString()
            : null!;
        if (string.IsNullOrWhiteSpace(ns)) return;

        if (Mods.ContainsKey(ns))
        {
            Plugin.Log.LogWarning($"Same namespace {ns} is skipped.");
            return;
        }

        try
        {
            Mods[ns] = new ModData(ns, Path.GetDirectoryName(path)!);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Creat ModData failed: {ex}");
        }
    }

    public static IEnumerable<ModData> GetMods()
    {
        return Mods.Values;
    }

    public static ModData? GetMod(string ns)
    {
        return Mods.GetValueOrDefault(ns);
    }
}