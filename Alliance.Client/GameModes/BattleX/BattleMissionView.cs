using Alliance.Client.Extensions.AgentsCount.Views;
using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.Artillery;
using Alliance.Client.Extensions.ExNativeUI.HUDExtension.Views;
using Alliance.Client.Extensions.ExNativeUI.MainAgentEquipmentController.MissionViews;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.BattleX
{
	[ViewCreatorModule]
	public class BattleMissionView
	{
		[ViewMethod("BattleX")]
		public static MissionView[] OpenBattleMission(Mission mission)
		{
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "Battle");
			views.AppendList(new List<MissionView>
			{
				// Custom views
				new FormationStatusView(),
                new AgentsCountView(),
                new AgentStatusView(),
                new AL_MainAgentEquipmentController(),
				new CannonCrosshairMissionView(),

				// Native battle views
				MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateOrderTroopPlacerView(null),
				MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
				MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
				MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                new SpectatorCameraView()
			});
			return views.ToArray();
		}
	}
}
