using System;
using System.Globalization;
using UnityEngine;

namespace ModCore.Games.ExtraDataModule.Parsers;

/// <summary>
/// 默认解析器。默认包含以下解析器：<br/>
/// <see cref="BoolParser"/><br/>
/// <see cref="IntParser"/><br/>
/// <see cref="FloatParser"/><br/>
/// <see cref="DoubleParser"/><br/>
/// <see cref="CharParser"/><br/>
/// <see cref="StringParser"/><br/>
/// <see cref="JsonUtilityParser{T}"/>
/// </summary>
public static class DefaultParsers
{
    private static readonly Dictionary<Type, object> Parsers = new()
    {
        [typeof(bool)] = new BoolParser(),
        [typeof(int)] = new IntParser(),
        [typeof(float)] = new FloatParser(),
        [typeof(double)] = new DoubleParser(),
        [typeof(char)] = new CharParser(),
        [typeof(string)] = new StringParser(),
    };

    /// <summary>
    /// 注册解析器。
    /// </summary>
    /// <param name="parser">数据解析器。</param>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <returns>是否注册成功。</returns>
    public static bool Register<T>(IParser<T> parser)
    {
        if (Parsers.ContainsKey(typeof(T))) return false;
        Parsers.Add(typeof(T), parser ?? throw new ArgumentNullException(nameof(parser)));
        return true;
    }

    /// <summary>
    /// 获取解析器。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <returns>若数据类型已注册</returns>
    public static IParser<T> GetParser<T>()
    {
        return Parsers.TryGetValue(typeof(T), out var parser) ? (IParser<T>)parser : new JsonUtilityParser<T>();
    }
}

/// <summary>
/// 布尔类型解析器。
/// </summary>
public class BoolParser : IParser<bool>
{
    public bool Parse(string value) => bool.Parse(value);

    public bool TryParse(string value, out bool result) => bool.TryParse(value, out result);

    public string ToString(bool value) => value.ToString();
}

/// <summary>
/// 整型解析器。
/// </summary>
public class IntParser : IParser<int>
{
    public int Parse(string value) => int.Parse(value);

    public bool TryParse(string value, out int result) => int.TryParse(value, out result);

    public string ToString(int value) => value.ToString();
}

/// <summary>
/// 单精度浮点型解析器。
/// </summary>
public class FloatParser : IParser<float>
{
    public float Parse(string value) => float.Parse(value);

    public bool TryParse(string value, out float result) => float.TryParse(value, out result);

    public string ToString(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
}

/// <summary>
/// 双精度浮点型解析器。
/// </summary>
public class DoubleParser : IParser<double>
{
    public double Parse(string value) => double.Parse(value);

    public bool TryParse(string value, out double result) => double.TryParse(value, out result);

    public string ToString(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}

/// <summary>
/// 字符解析器。
/// </summary>
public class CharParser : IParser<char>
{
    public char Parse(string value) => char.Parse(value);

    public bool TryParse(string value, out char result) => char.TryParse(value, out result);

    public string ToString(char value) => value.ToString();
}

/// <summary>
/// 字符串解析器。不建议使用此解析器，而是直接使用字符串。
/// </summary>
public class StringParser : IParser<string>
{
    public string Parse(string value) => value;

    public bool TryParse(string value, out string result)
    {
        result = value;
        return true;
    }

    public string ToString(string value) => value;
}

/// <summary>
/// JsonUtility解析器。
/// </summary>
/// <typeparam name="T"></typeparam>
public class JsonUtilityParser<T> : ParserBase<T>
{
    public override T Parse(string value) => JsonUtility.FromJson<T>(value);

    public override string ToString(T value) => JsonUtility.ToJson(value);
}