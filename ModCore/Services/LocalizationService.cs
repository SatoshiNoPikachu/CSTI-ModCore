using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ModCore.Services;

/// <summary>
/// 本地化服务
/// </summary>
public static class LocalizationService
{
    /// <summary>
    /// 本地化文本路径
    /// </summary>
    public const string LocalizationPath = "Resource";

    /// <summary>
    /// 当前语言
    /// </summary>
    public static string CurrentLanguage { get; private set; } = null!;

    /// <summary>
    /// 本地化目录路径
    /// </summary>
    private static readonly List<(string, string)> Paths = [];

    internal static void Init()
    {
        var mods = ModService.GetMods();
        foreach (var mod in mods)
        {
            RegisterPath(Path.Combine(mod.RootPath, LocalizationPath), $"{mod.Namespace}_");
        }
    }

    /// <summary>
    /// 加载语言
    /// </summary>
    internal static void LoadLanguage()
    {
        var manager = LocalizationManager.Instance;
        if (manager is null) return;

        var currentLanguage = LocalizationManager.CurrentLanguage;
        if (currentLanguage < 0 || currentLanguage >= manager.Languages.Length) return;

        var setting = manager.Languages[currentLanguage];
        var filePath = setting.FilePath;
        if (string.IsNullOrWhiteSpace(filePath)) filePath = "Localization/En.csv";
        CurrentLanguage = Path.GetFileNameWithoutExtension(filePath);

        foreach (var (pathDir, keyPrefix) in Paths)
        {
            var path = Path.Combine(pathDir, filePath);
            if (!File.Exists(path)) continue;

            var text = File.ReadAllText(path);
            var localizationDict = Parse(text);
            if (localizationDict is null) continue;

            var texts = LocalizationManager.CurrentTexts;
            var regex = new Regex(@"\\n");
            foreach (var (key, value) in localizationDict)
            {
                if (value.Count < 1) continue;
                texts[$"{keyPrefix}{key}"] = regex.Replace(value[0], "\n");
            }
        }
    }

    /// <summary>
    /// 注册本地化目录路径
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="keyPrefix">本地化键前缀</param>
    public static void RegisterPath(string path, string keyPrefix = "")
    {
        if (!Directory.Exists(path)) return;
        Paths.Add((path, keyPrefix ?? ""));
    }

    /// <summary>
    /// 解析本地化CSV文本
    /// </summary>
    /// <param name="text">CSV文本</param>
    /// <returns>本地化字典</returns>
    private static Dictionary<string, List<string>>? Parse(string text)
    {
        try
        {
            return CSVParser.LoadFromString(text);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
            return null;
        }
    }
}