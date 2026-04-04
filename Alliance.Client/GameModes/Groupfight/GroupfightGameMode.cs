using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Captain.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Multiplayer.Missions;

namespace Alliance.Client.GameModes.Groupfight
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
			List<MissionBehavior> behaviors = DefaultClientBehaviors.GetDefaultBehaviors(new BattleScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new FormationBehavior(),
				new MissionMultiplayerGameModeFlagDominationClient(),

				// Native battle behaviors
				new MultiplayerBattleMissionAgentInteractionLogic(),
                new MultiplayerRoundComponent(),
				new MultiplayerWarmupComponent(),
                new MultiplayerTeamSelectComponent()
			});
			return behaviors;
		}
	}
}
