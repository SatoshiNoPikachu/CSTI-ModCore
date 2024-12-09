using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ModCore.Services;

public static class LocalizationService
{
    public static string CurrentLanguage { get; private set; }

    private static readonly List<(string, string)> Paths = [];

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

    public static void RegisterPath(string path, string keyPrefix = "")
    {
        if (!Directory.Exists(path)) return;
        Paths.Add((path, keyPrefix ?? ""));
    }

    private static Dictionary<string, List<string>> Parse(string text)
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