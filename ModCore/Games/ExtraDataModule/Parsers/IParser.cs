namespace ModCore.Games.ExtraDataModule.Parsers;

public interface IParser<T>
{
    T Parse(string value);

    bool TryParse(string value, out T result);

    string ToString(T value);
}