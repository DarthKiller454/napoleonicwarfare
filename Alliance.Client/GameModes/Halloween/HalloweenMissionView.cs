using Alliance.Client.Extensions.ExNativeUI.MainAgentEquipmentController.MissionViews;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Common.Extensions.PlayerSpawn.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.Halloween
{
    [ViewCreatorModule]
    public class HalloweenMissionView
    {
        [ViewMethod("Halloween")]
        public static MissionView[] OpenTeamDeathmatchMission(Mission mission)
        {
            List<MissionView> missionViews = new List<MissionView>
            {
                new FormationStatusView(),
                new AL_MainAgentEquipmentController(),

                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
				MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
				MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
                MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateOrderTroopPlacerView(null),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Halloween"),
                MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
				MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
				MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
				MultiplayerViewCreator.CreatePollProgressUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
                ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView()
			};
            return missionViews.ToArray();
        }
    }
}
