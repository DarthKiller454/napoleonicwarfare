using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.TeamDeathmatchX
{
    [ViewCreatorModule]
    public class TeamDeathmatchMissionView
    {
        [ViewMethod("TeamDeathmatchX")]
        public static MissionView[] OpenTeamDeathmatchMission(Mission mission)
        {
            List<MissionView> missionViews = new List<MissionView>
            {
                new FormationStatusView(),
                new EquipmentSelectionView(),

				MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
				MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
				MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
				MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateOrderTroopPlacerView(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("TeamDeathmatch"),
                MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
				MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
				MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				MultiplayerViewCreator.CreatePollProgressUIHandler(),
				MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
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
