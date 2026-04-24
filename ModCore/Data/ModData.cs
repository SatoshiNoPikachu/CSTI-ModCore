using System;
using System.Linq;

namespace ModCore.Data;

/// <summary>
/// 模组数据。
/// </summary>
/// <param name="ns">命名空间，不允许包含':'和'@'以及'|'。</param>
/// <param name="rootPath">模组根目录。</param>
public class ModData(string ns, string rootPath)
{
    /// <summary>
    /// 命名空间。
    /// </summary>
    public string Namespace { get; } = IsValidNamespace(ns) ? ns : throw new ArgumentException(nameof(ns));

    /// <summary>
    /// 模组根目录。
    /// </summary>
    public string RootPath { get; } = rootPath ?? throw new ArgumentNullException(nameof(rootPath));

    /// <summary>
    /// 数据对象表。
    /// </summary>
    internal Dictionary<Type, IDictionary> AllData = [];

    /// <summary>
    /// 判断数据键中是否包含命名空间。
    /// </summary>
    /// <param name="key">数据键，支持分隔符':'和'@'。</param>
    /// <returns>是否包含命名空间。</returns>
    public static bool HasNamespace(string key)
    {
        return key.Any(c => c is ':' or '@');
    }

    /// <summary>
    /// 是否是有效的命名空间。
    /// </summary>
    /// <param name="ns">命名空间，不允许包含':'和'@'以及'|'。</param>
    /// <returns>命名空间是否有效。</returns>
    public static bool IsValidNamespace(string ns)
    {
        return !ns.Any(c => c is ':' or '@' or '|');
    }

    /// <summary>
    /// 获取数据。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <returns>数据字典。</returns>
    public Dictionary<string, T>? GetData<T>()
    {
        return AllData.GetValueOrDefault(typeof(T)) as Dictionary<string, T>;
    }

    /// <summary>
    /// 获取数据。
    /// </summary>
    /// <param name="key">不带命名空间的键。</param>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <returns>如果数据存在，则返回数据对象，否则返回类型默认值。</returns>
    public T? GetData<T>(string key)
    {
        var dict = GetData<T>();
        return dict is null ? default : dict.GetValueOrDefault(key);
    }
}