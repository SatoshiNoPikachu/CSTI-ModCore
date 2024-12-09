using System;

namespace ModCore.Data;

/// <summary>
/// 数据信息特性，加载器会自动注册带有该特性的类型
/// </summary>
/// <param name="name">别名</param>
[AttributeUsage(AttributeTargets.Class)]
public class DataInfoAttribute(string name = "") : Attribute
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; } = name;
}