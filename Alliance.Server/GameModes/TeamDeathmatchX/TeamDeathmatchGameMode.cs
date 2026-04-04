using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.ScoreboardData;
using Alliance.Server.GameModes.CaptainX.Behaviors;
using Alliance.Server.GameModes.PvC.Behaviors;
using Alliance.Server.Patch.Behaviors;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.Library;


namespace Alliance.Server.GameModes.TeamDeathmatchX
{
    public class TeamDeathmatchGameMode : MissionBasedMultiplayerGameMode
    {
        public TeamDeathmatchGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("TeamDeathmatchX", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
        }
        private List<MissionBehavior> GetMissionBehaviors()
        {
        // Default behaviors
        List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new BattleScoreboardData());
        behaviors.AppendList(new List<MissionBehavior>
        {
			// Custom behaviors
			new FormationBehavior(),
            new MissionMultiplayerTeamDeathmatch(),
            new MissionMultiplayerTeamDeathmatchClient(),
            new SpawnComponent(new TeamDeathmatchSpawnFrameBehavior(), new TeamDeathmatchXSpawningBehavior()),

			// Native battle behaviors
            new MultiplayerTeamSelectComponent(),
            new AgentVictoryLogic()
        });
        return behaviors;
        }
    }
}
