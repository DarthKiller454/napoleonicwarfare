using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Server.Patch.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;
using System;
using Alliance.Common.GameModes.ScoreboardData;


namespace Alliance.Server.GameModes.TeamDeathmatchX
{
    public class TeamDeathmatchGameMode : MissionBasedMultiplayerGameMode
    {
        public TeamDeathmatchGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("TeamDeathmatchX", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),
                    new FormationBehavior(),
                    new MissionMultiplayerTeamDeathmatch(),
                    new MissionMultiplayerTeamDeathmatchClient(),
                    new MultiplayerTimerComponent(),
                    new SpawnComponent(new TeamDeathmatchSpawnFrameBehavior(), new TeamDeathmatchXSpawningBehavior()),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MultiplayerTeamSelectComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerAdminComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new TDMXScoreboardData()),
                    new MissionAgentPanicHandler(),
                    new AgentHumanAILogic(),
                    new EquipmentControllerLeaveLogic(),
                    new MultiplayerPreloadHelper()
                };
            }, true, true);
        }
    }
}
