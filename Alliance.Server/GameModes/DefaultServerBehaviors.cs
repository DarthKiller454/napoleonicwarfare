using Alliance.Common.Core.Configuration.Models;
using Alliance.Server.Extensions.SAE.Behaviors;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes
{
	public static class DefaultServerBehaviors
	{
		/// <summary>
		/// List of default behaviors for the server, included in every game mode.
		/// </summary>
		public static List<MissionBehavior> GetDefaultBehaviors(IScoreboardData scoreboardData)
		{
			List<MissionBehavior> defaultBehaviors = new List<MissionBehavior>()
			{
				// Default behaviors from native
				new MissionScoreboardComponent(scoreboardData),
				new MultiplayerTimerComponent(),
				new AgentHumanAILogic(),
                new MissionAgentPanicHandler(),
                new MissionLobbyEquipmentNetworkComponent(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(10f),
				new MultiplayerPollComponent(),
				new MultiplayerAdminComponent(),
				new MultiplayerGameNotificationsComponent(),
				new MissionOptionsComponent(),
				new EquipmentControllerLeaveLogic(),
				new MultiplayerPreloadHelper(),
			};

			if (Config.Instance.ActivateSAE) defaultBehaviors.Add(new SaeBehavior());

			return defaultBehaviors;
		}
	}
}
