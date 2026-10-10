using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.Artillery;
using Alliance.Client.Extensions.ExNativeUI.HUDExtension.Views;
using Alliance.Client.Extensions.ExNativeUI.MainAgentEquipmentController.MissionViews;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Client.Extensions.Spyglass.Views;
using Alliance.Common.Extensions.PlayerSpawn.Views;
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
                new AL_MainAgentEquipmentController(),
                new AgentStatusView(),
                new HUDExtensionUIHandlerView(),


                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
				MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
				MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
				MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("TeamDeathmatch"),
                MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                ViewCreator.CreateOrderTroopPlacerView(null),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
				MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
				MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				MultiplayerViewCreator.CreatePollProgressUIHandler(),
				MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
                new CannonCrosshairMissionView(),
                new SpyglassView(),
            };
            return missionViews.ToArray();
        }
    }
}
