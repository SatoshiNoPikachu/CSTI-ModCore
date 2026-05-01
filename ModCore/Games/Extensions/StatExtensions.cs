using ModCore.Games.ExtraDataModule;

namespace ModCore.Games.Extensions;

public static class StatExtensions
{
    extension(GameStat stat)
    {
        public InGameStat InGame => Game.Gm!.StatsDict[stat];

        public float InGameValue => Game.Gm!.StatsDict[stat].CurrentValue(Game.Gm.NotInBase);

        public ExtraDataStorage Storage => stat.InGame.Storage;
    }

    extension(InGameStat stat)
    {
        public ExtraDataStorage Storage => ExtraDataStorage.GetOrCreateStorage(stat);
    }
}