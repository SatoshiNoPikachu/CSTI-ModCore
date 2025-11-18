using ModCore.Games.ExtraDataModule;

namespace ModCore.Games;

public static class Util
{
    public static GameManager Gm { get; internal set; } = null!;

    extension(GameStat stat)
    {
        public float InGameValue => Gm.StatsDict[stat].CurrentValue(Gm.NotInBase);
    }

    extension(InGameCardBase card)
    {
        public ExtraDataStorage Storage => ExtraDataStorage.GetOrCreateStorage(card);

        public float GetDurabilityValue(DurabilitiesTypes type) => type switch
        {
            DurabilitiesTypes.Spoilage => card.CurrentSpoilage,
            DurabilitiesTypes.Usage => card.CurrentUsageDurability,
            DurabilitiesTypes.Fuel => card.CurrentFuel,
            DurabilitiesTypes.Progress => card.CurrentProgress,
            DurabilitiesTypes.Special1 => card.CurrentSpecial1,
            DurabilitiesTypes.Special2 => card.CurrentSpecial2,
            DurabilitiesTypes.Special3 => card.CurrentSpecial3,
            DurabilitiesTypes.Special4 => card.CurrentSpecial4,
            DurabilitiesTypes.Liquid => card.CurrentLiquidQuantity,
            _ => -1
        };
    }
}