using System;
using System.Linq;

namespace ModCore.Data;

/// <summary>
/// 模组数据
/// </summary>
/// <param name="ns">命名空间，不允许包含':'和'@'</param>
/// <param name="rootPath">模组根目录</param>
public class ModData(string ns, string rootPath)
{
    /// <summary>
    /// 命名空间
    /// </summary>
    public string Namespace { get; } = HasNamespace(ns) ? throw new ArgumentException(nameof(ns)) : ns;

    /// <summary>
    /// 模组根目录
    /// </summary>
    public string RootPath { get; } = rootPath ?? throw new ArgumentNullException(nameof(rootPath));

    /// <summary>
    /// 获取命名空间
    /// </summary>
    /// <param name="key">数据键，支持分隔符':'和'@'</param>
    /// <returns>如果数据键存在命名空间，则返回命名空间，否则返回null</returns>
    public static bool HasNamespace(string key)
    {
        return key.Any(c => c is ':' or '@');
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    /// <param name="key">不带命名空间的键</param>
    /// <typeparam name="T">数据类型</typeparam>
    /// <returns>如果键存在，则返回数据对象，否则返回null</returns>
    public T? GetData<T>(string key)
    {
        return Database.GetData<T>($"{Namespace}:{key}");
    }
}