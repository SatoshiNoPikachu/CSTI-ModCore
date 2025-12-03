using System;
using UnityEngine;

namespace ModCore.Games.ExtraDataModule;

/// <summary>
/// 卡牌额外数据存储器。
/// </summary>
public static class CardExtraData
{
    internal static void OnCardInit(InGameCardBase card, List<CollectionDropsSaveData>? saves)
    {
        var storage = ExtraDataStorage.GetStorage(card);
        storage?.Reset();

        saves?.RemoveAll(save =>
        {
            if (!ExtraDataStorage.TryExtractData(save.CollectionName, out var data)) return false;

            storage ??= ExtraDataStorage.GetOrCreateStorage(card);

            if (!storage.TryAdd(data.Key, data.Value))
            {
                Plugin.Log.LogWarning($"Duplicate key {data.Key} in {nameof(CardExtraData)}.");
            }

            return true;
        });
    }

    internal static void OnCardSave(InGameCardBase card, CardSaveData save)
    {
        var storage = ExtraDataStorage.GetStorage(card);
        if (storage is null) return;

        var count = storage.Count;
        if (count == 0) return;

        var saves = save.CollectionUses;
        saves.Capacity = Math.Max(saves.Capacity, saves.Count + count);

        foreach (var raw in storage.GetRaws())
        {
            saves.Add(new CollectionDropsSaveData(raw, Vector2Int.zero));
        }
    }

    internal static void OnCardReset(InGameCardBase card)
    {
        ExtraDataStorage.GetStorage(card)?.Reset();
    }
}