using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BattlePlayerStatsHalloween : BattlePlayerStatsBase
{
    public int Score { get; set; }

    public BattlePlayerStatsHalloween()
    {
        base.GameType = "Halloween";
    }
}