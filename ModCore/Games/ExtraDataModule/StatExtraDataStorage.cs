using System;

namespace ModCore.Games.ExtraDataModule;

public static class StatExtraDataStorage
{
    public static void OnStatLoad(InGameStat stat, StatSaveData statSave)
    {
        var storage = ExtraDataStorage.GetStorage(stat);
        storage?.Reset();

        statSave.StaleActions?.RemoveAll(save =>
        {
            if (!ExtraDataStorage.TryExtractData(save.ModifierSource, out var data)) return false;

            storage ??= ExtraDataStorage.GetOrCreateStorage(stat);

            if (!storage.TryAdd(data.Key, data.Value))
            {
                Plugin.Log.LogWarning($"Duplicate key {data.Key} in {nameof(StatExtraDataStorage)}.");
            }

            return true;
        });
    }

    public static void OnStatSave(InGameStat stat, StatSaveData statSave)
    {
        var storage = ExtraDataStorage.GetStorage(stat);
        if (storage is null) return;

        var count = storage.Count;
        if (count == 0) return;

        statSave.StaleActions ??= [];

        var saves = statSave.StaleActions;
        saves.Capacity = Math.Max(saves.Capacity, saves.Count + count);

        foreach (var raw in storage.GetRaws())
        {
            saves.Add(new StalenessData(raw, 0));
        }
    }
}