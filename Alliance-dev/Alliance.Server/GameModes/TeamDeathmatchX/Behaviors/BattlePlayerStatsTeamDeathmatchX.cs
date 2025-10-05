using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BattlePlayerStatsTeamDeathmatchX : BattlePlayerStatsBase
{
    public int Score { get; set; }

    public BattlePlayerStatsTeamDeathmatchX()
    {
        base.GameType = "TeamDeathmatchX";
    }
}