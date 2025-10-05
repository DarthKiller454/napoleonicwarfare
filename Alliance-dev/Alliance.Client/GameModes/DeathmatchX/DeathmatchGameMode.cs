using Alliance.Client.Patch.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;
using Alliance.Common.GameModes.ScoreboardData;


namespace Alliance.Client.GameModes.DeathmatchX
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
					new AllianceAgentVisualSpawnComponent(),

                    MissionLobbyComponent.CreateBehavior(),
                    new MissionMultiplayerFFAClient(),
                    new MultiplayerAchievementComponent(),
                    new MultiplayerTimerComponent(),
                    new MultiplayerMissionAgentVisualSpawnComponent(),
                    new ConsoleMatchStartEndHandler(),
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
                    MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
                    new EquipmentControllerLeaveLogic(),
                    new MissionRecentPlayersComponent(),
                    new MultiplayerPreloadHelper()
                };
			}, true, true);
		}
	}
}
