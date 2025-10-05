using Alliance.Client.Patch.Behaviors;
using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Captain.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;
using Alliance.Common.GameModes.ScoreboardData;

namespace Alliance.Client.GameModes.TeamDeathmatchX
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
					new AllianceAgentVisualSpawnComponent(),
					new MissionMultiplayerTeamDeathmatchClient(),
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
					new MissionScoreboardComponent(new BattleXScoreboardData()),
					MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
					new EquipmentControllerLeaveLogic(),
					new MissionRecentPlayersComponent(),
					new MultiplayerPreloadHelper()
				};
			}, true, true);
		}
	}
}
