using System;
using ModCore.Games.ExtraDataModule.Parsers;

namespace ModCore.Games.ExtraDataModule;

/// <summary>
/// 数据代理基类。
/// </summary>
/// <param name="data">代理的额外数据对象。</param>
public abstract class DataProxy(ExtraData data)
{
    /// <summary>
    /// 数据类型。
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    /// 是否有效。
    /// </summary>
    public bool IsValid => data.IsValid;

    /// <summary>
    /// 创建代理。
    /// </summary>
    /// <param name="data">代理的额外数据对象。</param>
    /// <param name="parser">数据解析器。</param>
    /// <returns>数据代理。</returns>
    internal static DataProxy<T>? Create<T>(ExtraData data, IParser<T> parser)
    {
        return parser.TryParse(data.Data, out var v) ? new DataProxy<T>(data, v, parser) : null;
    }
}

/// <summary>
/// 泛型数据代理。
/// </summary>
/// <param name="data">代理的额外数据对象。</param>
/// <param name="value">数据解析值。</param>
/// <param name="parser">数据解析器。</param>
/// <typeparam name="T">目标数据类型。</typeparam>
public class DataProxy<T>(ExtraData data, T value, IParser<T> parser) : DataProxy(data)
{
    /// <summary>
    /// 数据类型。
    /// </summary>
    public override Type Type => typeof(T);

    /// <summary>
    /// 数据值。
    /// </summary>
    public T Value { get; set; } = value;

    /// <summary>
    /// 将数据值转换成字符串。
    /// </summary>
    /// <returns>转换结果。</returns>
    public override string ToString() => parser.ToString(Value);
}