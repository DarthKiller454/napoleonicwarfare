using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.BattleX
{
    [ViewCreatorModule]
    public class TeamDeathmatchMissionView
    {
        [ViewMethod("BattleX")]
        public static MissionView[] OpenBattleMission(Mission mission)
        {
            List<MissionView> missionViews = new List<MissionView>
            {
                new FormationStatusView(),
                new EquipmentSelectionView(),
                new AgentStatusView(),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
                new MissionBoundaryWallView(),
                new SpectatorCameraView(),

                MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
                MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
                MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Battle"),
                MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
                ViewCreator.CreateOrderTroopPlacerView(mission),
                ViewCreator.CreateOptionsUIHandler(),
                MultiplayerViewCreator.CreatePollProgressUIHandler(),
            };
            return missionViews.ToArray();
        }
    }
}
