using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Server.Patch.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;
using Alliance.Server.Extensions.AIBehavior.Behaviors;
using System;
using Alliance.Server.GameModes.Halloween.Behaviors;
using Alliance.Common.GameModes.ScoreboardData;


namespace Alliance.Server.GameModes.Halloween
{
    public class HalloweenGameMode : MissionBasedMultiplayerGameMode
    {
        public HalloweenGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("Halloween", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),
                    new FormationBehavior(),
                    new MissionMultiplayerHalloween(),
                    new MissionMultiplayerHalloweenClient(),
                    new MultiplayerTimerComponent(),
                    new SpawnComponent(new HalloweenSpawnFrame(), new HalloweenSpawningBehavior()),
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
                    new HalloweenBotController(),
                    new MultiplayerPreloadHelper(),
                    new HalloweenBotRandomizationBehavior(),
                    new ALGlobalAIBehavior()
                };
            }, true, true);
        }
    }
}
