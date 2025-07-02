using System;
using System.Collections.Generic;

namespace ModCore.Data;

public static class DataMap
{
    private static readonly Dictionary<string, List<CardData>> CardTagMap = [];

    private static readonly Dictionary<CardTypes, List<CardData>> CardTypeMap = [];

    internal static void Mapping()
    {
        var cards = Database.GetData<CardData>()?.Values;
        if (cards is null) return;
        
        foreach (var card in cards)
        {
            MapCardTags(card);
            MapCardTypes(card);
        }
    }

    public static IEnumerable<CardData> GetCards(this CardTag tag)
    {
        return CardTagMap.TryGetValue(tag.name, out var cards) ? cards : Array.Empty<CardData>();
    }

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

            if (CardTagMap.TryGetValue(tag.name, out var list))
            {
                list.Add(card);
            }
            else
            {
                CardTagMap[tag.name] = [card];
            }
        }
    }

    private static void MapCardTypes(CardData card)
    {
        if (CardTypeMap.TryGetValue(card.CardType, out var list))
        {
            list.Add(card);
        }
        else
        {
            CardTypeMap[card.CardType] = [card];
        }
    }
}