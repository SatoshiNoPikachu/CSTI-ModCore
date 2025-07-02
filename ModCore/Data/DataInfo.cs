using System;

namespace ModCore.Data;

/// <summary>
/// 数据信息
/// </summary>
/// <param name="type">类型</param>
/// <param name="name">名称</param>
public class DataInfo(Type type, string name = "")
{
    /// <summary>
    /// 数据类型
    /// </summary>
    public Type Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

    /// <summary>
    /// 数据名称
    /// </summary>
    public string Name { get; } = string.IsNullOrWhiteSpace(name) ? type.Name : name;

    /// <summary>
    /// 允许回退到模组根目录加载
    /// </summary>
    public bool CanFallbackToRoot { get; } = true;

    public DataInfo(Type type, bool canFallbackToRoot) : this(type)
    {
        CanFallbackToRoot = canFallbackToRoot;
    }

    public DataInfo(Type type, string name, bool canFallbackToRoot) : this(type, name)
    {
        CanFallbackToRoot = canFallbackToRoot;
    }

    public override bool Equals(object? obj)
    {
        return obj is DataInfo other && Type == other.Type;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }
}