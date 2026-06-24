using System;

namespace ModCore.Data;

public static class DataMap
{
    private static readonly Dictionary<CardTag, HashSet<CardData>> CardTagMap = [];

    private static readonly Dictionary<CardTypes, HashSet<CardData>> CardTypeMap = [];

    extension(CardTag tag)
    {
        public IReadOnlyCollection<CardData> Cards =>
            CardTagMap.TryGetValue(tag, out var set) ? set : Array.Empty<CardData>();
    }

    extension(CardTypes type)
    {
        public IReadOnlyCollection<CardData> Cards =>
            CardTypeMap.TryGetValue(type, out var set) ? set : Array.Empty<CardData>();
    }

    internal static void Mapping()
    {
        CardTagMap.Clear();
        CardTypeMap.Clear();

        var cards = Database.GetData<CardData>()?.Values;
        if (cards is null) return;

        foreach (var card in cards)
        {
            MapCardTags(card);
            MapCardTypes(card);
        }
    }

    [Obsolete("该扩展方法将弃用，请使用扩展属性Cards", true)]
    public static IEnumerable<CardData> GetCards(this CardTag tag)
    {
        return CardTagMap.TryGetValue(tag, out var cards) ? cards : Array.Empty<CardData>();
    }

    [Obsolete("该扩展方法将弃用，请使用扩展属性Cards", true)]
    public static IEnumerable<CardData> GetCards(this CardTypes type)
    {
        return CardTypeMap.TryGetValue(type, out var cards) ? cards : Array.Empty<CardData>();
    }

    private static void MapCardTags(CardData card)
    {
        if (card.CardTags is null) return;

        foreach (var tag in card.CardTags)
        {
            if (tag is null) continue;
            
            if (CardTagMap.TryGetValue(tag, out var set)) set.Add(card);
            else CardTagMap[tag] = [card];
        }
    }

    private static void MapCardTypes(CardData card)
    {
        if (CardTypeMap.TryGetValue(card.CardType, out var set)) set.Add(card);
        else CardTypeMap[card.CardType] = [card];
    }
}