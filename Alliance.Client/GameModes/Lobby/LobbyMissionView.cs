using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.Artillery;
using Alliance.Client.Extensions.Spyglass.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.Lobby
{
	[ViewCreatorModule]
	public class LobbyMissionView
	{
		[ViewMethod("Lobby")]
		public static MissionView[] OpenLobbyMission(Mission mission)
		{
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "Lobby");
			views.AppendList(new List<MissionView>
			{
				// Custom views
				new AgentStatusView(),
                new CannonCrosshairMissionView(),
                new SpyglassView(),
				
				// Native views
				//MultiplayerViewCreator.CreateMissionMultiplayerFFAView(),
				MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler()
			});
			return views.ToArray();
		}
	}
}
