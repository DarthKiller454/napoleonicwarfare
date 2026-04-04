using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.HUDExtension.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.Groupfight
{
	[ViewCreatorModule]
	public class GroupfightMissionView
	{
		[ViewMethod("Groupfight")]
		public static MissionView[] OpenBattleMission(Mission mission)
		{
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "Groupfight");
			views.AppendList(new List<MissionView>
			{
				// Custom views
				new FormationStatusView(),
				new HUDExtensionUIHandlerView(),
				new AgentStatusView(),

                MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
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
