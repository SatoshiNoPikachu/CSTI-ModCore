namespace ModCore.Games.ExtraDataModule.Parsers;

public abstract class ParserBase<T> : IParser<T>
{
    public abstract T Parse(string value);

    public abstract string ToString(T value);

    public virtual bool TryParse(string value, out T result)
    {
        try
        {
            result = Parse(value);
            return true;
        }
        catch
        {
            result = default!;
            return false;
        }
    }
}