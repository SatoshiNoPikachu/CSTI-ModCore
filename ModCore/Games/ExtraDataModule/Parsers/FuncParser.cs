using System;

namespace ModCore.Games.ExtraDataModule.Parsers;

public delegate bool TryParseFunc<T>(string a, out T b);

public class FuncParser<T>(Func<string, T> parse, Func<T, string> toString, TryParseFunc<T>? tryParse = null)
    : ParserBase<T>
{
    public override T Parse(string value) => parse(value);

    public override string ToString(T value) => toString(value);

    public override bool TryParse(string value, out T result)
    {
        return tryParse is null ? base.TryParse(value, out result) : tryParse(value, out result);
    }
}