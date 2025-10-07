using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Server.Patch.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;
using Alliance.Common.GameModes.ScoreboardData;


namespace Alliance.Server.GameModes.DeathmatchX
{
    public class DeathmatchGameMode : MissionBasedMultiplayerGameMode
    {
        public DeathmatchGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("DeathmatchX", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return new MissionBehavior[]
                {
                        new AllianceLobbyComponent(),
                        new FormationBehavior(),
                        new MissionMultiplayerFFAX(),
                        new MissionMultiplayerFFAClient(),
                        new MultiplayerTimerComponent(),
                        new SpawnComponent(new FFASpawnFrameBehavior(), new WarmupSpawningBehavior()),
                        new MissionLobbyEquipmentNetworkComponent(),
                        new MultiplayerTeamSelectComponent(),
                        new MissionHardBorderPlacer(),
                        new MissionBoundaryPlacer(),
                        new MissionBoundaryCrossingHandler(),
                        new MultiplayerPollComponent(),
                        new MultiplayerAdminComponent(),
                        new MultiplayerGameNotificationsComponent(),
                        new MissionOptionsComponent(),
                        new MissionScoreboardComponent(new FFAXScoreboardData()),
                        new MissionAgentPanicHandler(),
                        new AgentHumanAILogic(),
                        new EquipmentControllerLeaveLogic(),
                        new MultiplayerPreloadHelper()
                };
            }, true, true);
        }
    }
}
