using System;

namespace ModCore.Data;

/// <summary>
/// 数据信息
/// </summary>
/// <param name="type">类型</param>
/// <param name="name">名称</param>
public class DataInfo(Type type, string name = "") : IEquatable<DataInfo>
{
    /// <summary>
    /// 数据类型
    /// </summary>
    public Type Type { get; } = type;

    /// <summary>
    /// 数据名称
    /// </summary>
    public string Name { get; } = string.IsNullOrWhiteSpace(name) ? type.Name : name;

    public bool Equals(DataInfo other)
    {
        if (other is null) return false;

        return Type == other.Type;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as DataInfo);
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }
}