using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Captain.Behaviors;
using Alliance.Server.GameModes.CaptainX.Behaviors;
using Alliance.Server.GameModes.PvC.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Multiplayer.Missions;

namespace Alliance.Server.GameModes.Groupfight
{
	public class GroupfightGameMode : MissionBasedMultiplayerGameMode
	{
		public GroupfightGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("Groupfight", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
		}

		private List<MissionBehavior> GetMissionBehaviors()
		{
			// Default behaviors
			List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new BattleScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new FormationBehavior(),
                new ALMissionMultiplayerFlagDomination(MultiplayerGameType.Battle),
                new ALMissionMultiplayerFlagDominationClient(),
                new MultiplayerBattleMissionAgentInteractionLogic(),
                new SpawnComponent(new PvCFlagDominationSpawnFrameBehavior(), new BattleXSpawningBehavior()),

				// Native battle behaviors
                new MultiplayerRoundController(),
				new MultiplayerWarmupComponent(),
				new MultiplayerTeamSelectComponent(),
				new AgentVictoryLogic()
			});
			return behaviors;
		}
	}
}
