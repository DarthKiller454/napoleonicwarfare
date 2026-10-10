using Alliance.Client.Extensions.AgentsCount.Views;
using Alliance.Client.Extensions.ExNativeUI.Artillery;
using Alliance.Client.Extensions.Spyglass.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.BattleRoyale
{
    [ViewCreatorModule]
    public class BRMissionView
    {
        [ViewMethod("BattleRoyale")]
        public static MissionView[] OpenBRMission(Mission mission)
        {
            List<MissionView> missionViews = new List<MissionView>
            {
                new AgentsCountView(),

                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
                MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
                //MultiplayerViewCreator.CreateMissionMultiplayerFFAView(),
                MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("BattleRoyale"),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, true),
                MultiplayerViewCreator.CreatePollProgressUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionBoundaryWallView(),
                new CannonCrosshairMissionView(),
                new SpyglassView()
            };
            return missionViews.ToArray();
        }
    }
}
