using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Siege.Behaviors;
using Alliance.Server.GameModes.SiegeX.Behaviors;
using Alliance.Server.Patch.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;
using Alliance.Common.GameModes.ScoreboardData;
using Alliance.Server.Core.Configuration.Behaviors;


namespace Alliance.Server.GameModes.SiegeX
{
    public class SiegeGameMode : MissionBasedMultiplayerGameMode
    {
        public SiegeGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("SiegeX", new MissionInitializerRecord(scene)
            {
                SceneUpgradeLevel = 3,
                SceneLevels = ""
            }, delegate (Mission missionController)
            {
                return new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),
                    new FormationBehavior(),
                    new MissionMultiplayerSiege(),
                    new MultiplayerWarmupComponent(),
                    new MissionMultiplayerSiegeClient(),
                    new MultiplayerTimerComponent(),
                    new SpawnComponent(new SiegeSpawnFrameBehavior(), new SiegeSpawningBehavior()),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MultiplayerTeamSelectComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerAdminComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new SiegeScoreboardData()),
                    new MissionAgentPanicHandler(),
                    new AgentHumanAILogic(),
                    new EquipmentControllerLeaveLogic(),
                    new MultiplayerPreloadHelper(),
                };
            }, true, true);
        }
    }
}
